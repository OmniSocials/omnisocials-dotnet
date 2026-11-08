using System.Net;
using System.Text.Json;
using Xunit;

namespace OmniSocials.Tests;

/// <summary>
/// X statistics on credits: accounts update, the post refresh (sent again only
/// after a 429), credits, the typed models and the webhook event names.
/// </summary>
public class XStatisticsTests
{
    private const string BadGateway = "{\"error\":{\"code\":\"refresh_failed\",\"message\":\"X did not answer\"}}";
    private static readonly Dictionary<string, string> RetryNow = new() { ["Retry-After"] = "0" };

    private const string XAccount =
        "{\"data\":{\"id\":\"1234_x\",\"platform\":\"x\",\"x_statistics\":{\"posts\":\"light\",\"followers\":\"daily\"," +
        "\"post_schedule\":null,\"source\":\"account\",\"billing\":\"credits\",\"state\":\"paused\"," +
        "\"paused_reason\":\"insufficient_credits\"}}}";

    private static OmniSocialsClient CreateClient(StubHttpMessageHandler handler, TimeSpan? timeout = null)
        => new(new OmniSocialsOptions
        {
            ApiKey = "omsk_test_key",
            BaseUrl = "https://api.test.local/v1",
            MaxRetries = 2,
            Timeout = timeout ?? TimeSpan.FromSeconds(5),
            HttpMessageHandler = handler,
        });

    private static string Path(StubHttpMessageHandler handler, int index)
        => handler.Requests[index].RequestUri!.OriginalString["https://api.test.local/v1".Length..];

    [Fact]
    public async Task Accounts_update_sends_only_the_given_keys_and_null()
    {
        var handler = new StubHttpMessageHandler();
        for (var i = 0; i < 4; i++) handler.Enqueue(HttpStatusCode.OK, XAccount);
        using var client = CreateClient(handler);

        var updated = await client.Accounts.UpdateAsync("1234_x", new AccountUpdateParams
        {
            XStatistics = new XStatisticsInput { Posts = "light", Followers = "daily" },
        });
        await client.Accounts.UpdateAsync("1234_x", new AccountUpdateParams
        {
            XStatistics = new XStatisticsInput { Followers = "off" },
        });
        await client.Accounts.UpdateAsync("1234_x", new AccountUpdateParams
        {
            XStatistics = new XStatisticsInput
            {
                Posts = "custom",
                PostSchedule = new XStatisticsPostSchedule { At = new[] { "1h", "1d" }, ThenEvery = "7d", Until = "90d" },
            },
        });
        await client.Accounts.UpdateAsync("1234_x", new AccountUpdateParams { XStatistics = null });

        Assert.Equal(HttpMethod.Patch, handler.Requests[0].Method);
        Assert.Equal("/accounts/1234_x", Path(handler, 0));
        Assert.Equal("{\"x_statistics\":{\"posts\":\"light\",\"followers\":\"daily\"}}", handler.RequestBodies[0]);
        Assert.Equal("{\"x_statistics\":{\"followers\":\"off\"}}", handler.RequestBodies[1]);
        Assert.Equal(
            "{\"x_statistics\":{\"posts\":\"custom\",\"post_schedule\":{\"at\":[\"1h\",\"1d\"],\"then_every\":\"7d\",\"until\":\"90d\"}}}",
            handler.RequestBodies[2]);
        Assert.Equal("{\"x_statistics\":null}", handler.RequestBodies[3]);

        var x = updated!.Value.GetProperty("data").GetProperty("x_statistics").Deserialize<AccountXStatistics>()!;
        Assert.Equal("light", x.Posts);
        Assert.Equal("paused", x.State);
        Assert.Equal("insufficient_credits", x.PausedReason);
        Assert.Null(x.PostSchedule);
    }

    [Fact]
    public async Task Refresh_post_and_credits_send_the_right_requests()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK,
            "{\"data\":{\"post_id\":9123,\"tweets\":[{\"id\":\"1859\",\"metrics\":{\"views\":3412},\"read_at\":\"2027-01-08T09:12:04Z\"}]," +
            "\"credits_used\":0.5,\"read_at\":\"2027-01-08T09:12:04Z\"}}");
        handler.Enqueue(HttpStatusCode.OK,
            "{\"data\":{\"balance\":412,\"auto_refill\":{\"enabled\":false},\"this_month\":{\"x_statistics\":31}," +
            "\"x_statistics\":{\"state\":\"active\",\"paused_at\":null,\"charging\":{\"active\":true,\"from\":\"2027-01-01T00:00:00.000Z\"}," +
            "\"prices\":{\"post_read\":0.5,\"media_read\":0.5,\"follower_read\":1},\"total_credits_month\":38," +
            "\"this_month\":{\"post_reads\":40,\"media_reads\":2,\"follower_reads\":8,\"refreshes\":1,\"credits\":31}," +
            "\"accounts\":[{\"account_id\":\"1234_x\",\"workspace_id\":1234,\"handle\":\"@omnisocials\",\"posts\":\"full\"," +
            "\"followers\":\"daily\",\"tweets_30d\":12,\"credits_month\":38}]}}}");
        using var client = CreateClient(handler);

        var fresh = await client.Analytics.RefreshPostAsync(9123);
        var credits = await client.Credits.GetAsync();

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("/analytics/posts/9123/refresh", Path(handler, 0));
        Assert.Null(handler.RequestBodies[0]);
        Assert.Equal(HttpMethod.Get, handler.Requests[1].Method);
        Assert.Equal("/credits", Path(handler, 1));

        var refresh = fresh!.Value.Deserialize<PostRefreshResponse>()!.Data;
        Assert.Equal(9123, refresh.PostId);
        Assert.Equal(0.5m, refresh.CreditsUsed);
        Assert.Equal("1859", refresh.Tweets[0].Id);
        Assert.Equal(3412, refresh.Tweets[0].Metrics["views"].GetInt32());

        var typed = credits!.Value.Deserialize<CreditsResponse>()!.Data;
        Assert.Equal(412m, typed.Balance);
        Assert.False(typed.AutoRefill.Enabled);
        Assert.Equal(31m, typed.ThisMonth["x_statistics"]);
        Assert.True(typed.XStatistics.Charging.Active);
        Assert.Equal(1m, typed.XStatistics.Prices.FollowerRead);
        Assert.Equal(1, typed.XStatistics.ThisMonth.Refreshes);
        Assert.Equal("1234_x", typed.XStatistics.Accounts[0].AccountId);
        Assert.Equal(12, typed.XStatistics.Accounts[0].Tweets30d);
    }

    [Fact]
    public async Task Refresh_post_is_sent_once_after_a_502_a_connection_error_or_a_timeout()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.BadGateway, BadGateway, headers: RetryNow);
        handler.EnqueueConnectionError();
        handler.EnqueueDelayed(TimeSpan.FromSeconds(30), HttpStatusCode.OK, "{\"data\":{}}");
        using var client = CreateClient(handler, TimeSpan.FromMilliseconds(200));

        var ex = await Assert.ThrowsAsync<ServerException>(() => client.Analytics.RefreshPostAsync("9123"));
        Assert.Equal("refresh_failed", ex.Code);
        Assert.Single(handler.Requests);

        await Assert.ThrowsAsync<ApiConnectionException>(() => client.Analytics.RefreshPostAsync("9123"));
        Assert.Equal(2, handler.Requests.Count);

        await Assert.ThrowsAsync<ApiConnectionException>(() => client.Analytics.RefreshPostAsync("9123"));
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task Refresh_post_is_retried_after_a_429_and_credits_after_a_502()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue((HttpStatusCode)429, "{\"error\":{\"code\":\"rate_limit_exceeded\"}}", headers: RetryNow);
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"post_id\":\"9123\",\"tweets\":[],\"credits_used\":0}}");
        handler.Enqueue(HttpStatusCode.BadGateway, BadGateway, headers: RetryNow);
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"balance\":1}}");
        using var client = CreateClient(handler);

        var fresh = await client.Analytics.RefreshPostAsync(9123);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(9123, fresh!.Value.Deserialize<PostRefreshResponse>()!.Data.PostId);

        await client.Credits.GetAsync();
        Assert.Equal(4, handler.Requests.Count);
    }

    [Fact]
    public void Webhook_event_names_and_data()
    {
        Assert.Equal(new[] { "x_statistics.paused", "x_statistics.resumed" }, XStatisticsWebhookEvents.All);
        var data = JsonSerializer.Deserialize<XStatisticsEventData>(
            "{\"workspace_id\":1234,\"account_id\":\"1234_x\",\"reason\":\"credits_added\"}")!;
        Assert.Equal(1234, data.WorkspaceId);
        Assert.Equal("1234_x", data.AccountId);
        Assert.Equal("credits_added", data.Reason);
    }
}
