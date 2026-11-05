using System.Net;
using System.Text.Json;
using Xunit;

namespace OmniSocials.Tests;

/// <summary>
/// Request path, method and body of the ads calls, the optional typed models,
/// and the money-call rule: a call that can start ad spend is never sent twice
/// after a 5xx, a connection error or a timeout.
/// </summary>
public class AdsTests
{
    private const string BadGateway = "{\"error\":{\"code\":\"bad_gateway\",\"message\":\"upstream\"}}";
    private static readonly Dictionary<string, string> RetryNow = new() { ["Retry-After"] = "0" };

    private static OmniSocialsClient CreateClient(StubHttpMessageHandler handler, TimeSpan? timeout = null)
        => new(new OmniSocialsOptions
        {
            ApiKey = "omsk_test_key",
            BaseUrl = "https://api.test.local/v1",
            MaxRetries = 2,
            Timeout = timeout ?? TimeSpan.FromSeconds(5),
            HttpMessageHandler = handler,
        });

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private static string Path(StubHttpMessageHandler handler, int index)
        => handler.Requests[index].RequestUri!.OriginalString["https://api.test.local/v1".Length..];

    private static AdDraftParams Draft() => new()
    {
        Name = "Autumn sale",
        Objective = "traffic",
        Platforms = new object[] { "meta", new AdDraftPlatform { Platform = "linkedin", BudgetAmount = 20 } },
        Budget = new AdDraftBudget { Amount = 10, Currency = "EUR" },
        Schedule = new AdDraftSchedule { DurationDays = 7 },
        Targeting = new AdDraftTargeting { Countries = new[] { "NL" }, AutomaticAudience = true },
        Ads = new[]
        {
            new AdDraftAd
            {
                Media = new[] { new AdMediaFile { Url = "https://example.com/sale.jpg", Type = "image" } },
                PrimaryText = "30% off everything this week",
                Cta = "shop_now",
                Link = "https://example.com/sale",
                PlacementMedia = new AdPlacementMedia
                {
                    StoriesReels = new AdMediaFile { Url = "https://example.com/sale-9x16.jpg", Type = "image" },
                },
            },
        },
    };

    [Fact]
    public async Task Draft_create_update_launch_and_activate_send_the_right_requests()
    {
        var handler = new StubHttpMessageHandler();
        for (var i = 0; i < 4; i++) handler.Enqueue(HttpStatusCode.OK, "{\"data\":{}}");
        using var client = CreateClient(handler);

        await client.Ads.Drafts.CreateAsync(Draft());
        await client.Ads.Drafts.UpdateAsync("g-1", Draft());
        await client.Ads.Drafts.LaunchAsync("g-1", 10m);
        await client.Ads.ActivateAsync(7, 25m);

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("/ads/drafts", Path(handler, 0));
        var created = Parse(handler.RequestBodies[0]!);
        Assert.Equal("traffic", created.GetProperty("objective").GetString());
        Assert.Equal("meta", created.GetProperty("platforms")[0].GetString());
        Assert.Equal(20, created.GetProperty("platforms")[1].GetProperty("budget_amount").GetInt32());
        Assert.Equal(10, created.GetProperty("budget").GetProperty("amount").GetInt32());
        Assert.Equal(7, created.GetProperty("schedule").GetProperty("duration_days").GetInt32());
        var ad = created.GetProperty("ads")[0];
        Assert.Equal("shop_now", ad.GetProperty("cta").GetString());
        Assert.Equal("https://example.com/sale-9x16.jpg",
            ad.GetProperty("placement_media").GetProperty("stories_reels").GetProperty("url").GetString());
        Assert.False(ad.TryGetProperty("headline", out _)); // nulls are left out

        Assert.Equal(HttpMethod.Put, handler.Requests[1].Method);
        Assert.Equal("/ads/drafts/g-1", Path(handler, 1));

        Assert.Equal(HttpMethod.Post, handler.Requests[2].Method);
        Assert.Equal("/ads/drafts/g-1/launch", Path(handler, 2));
        Assert.Equal("{\"confirm_budget_amount\":10}", handler.RequestBodies[2]);

        Assert.Equal(HttpMethod.Post, handler.Requests[3].Method);
        Assert.Equal("/ads/campaigns/7/activate", Path(handler, 3));
        Assert.Equal("{\"confirm_budget_amount\":25}", handler.RequestBodies[3]);
    }

    [Fact]
    public async Task Other_calls_send_the_right_requests()
    {
        var handler = new StubHttpMessageHandler();
        for (var i = 0; i < 7; i++) handler.Enqueue(HttpStatusCode.OK, "{\"data\":{}}");
        using var client = CreateClient(handler);

        await client.Ads.Drafts.GetAsync("g 1/x");
        await client.Ads.Drafts.AttachAccountAsync("g-1", "meta", 12);
        await client.Ads.Drafts.LaunchAsync("g-1", new AdDraftLaunchParams
        {
            ConfirmBudgetAmounts = new Dictionary<string, decimal> { ["41"] = 10m, ["42"] = 20m },
        });
        await client.Ads.ActivateMultiAsync(new long[] { 7, 8 }, 5m);
        await client.Ads.UpdateAsync(7, new AdCampaignUpdateParams { ConfirmBudgetAmount = 30m });
        await client.Ads.ResumeAsync(7);
        await client.Ads.ListAsync(new AdCampaignListParams { Status = "active", Limit = 10 });

        Assert.Equal("/ads/drafts/g%201%2Fx", Path(handler, 0));
        Assert.Equal("{\"platform\":\"meta\",\"ad_account_id\":12}", handler.RequestBodies[1]);
        Assert.Equal(20, Parse(handler.RequestBodies[2]!).GetProperty("confirm_budget_amounts").GetProperty("42").GetInt32());
        Assert.False(Parse(handler.RequestBodies[2]!).TryGetProperty("confirm_budget_amount", out _));
        Assert.Equal("/ads/campaigns/activate-multi", Path(handler, 3));
        Assert.Equal("{\"campaign_ids\":[7,8],\"confirm_budget_amount\":5}", handler.RequestBodies[3]);
        Assert.Equal(HttpMethod.Patch, handler.Requests[4].Method);
        Assert.Equal("/ads/campaigns/7", Path(handler, 4));
        Assert.Equal("/ads/campaigns/7/resume", Path(handler, 5));
        Assert.Null(handler.RequestBodies[5]);
        Assert.Equal("/ads/campaigns?status=active&limit=10", Path(handler, 6));
    }

    [Fact]
    public async Task Launch_and_activate_are_sent_once_after_a_502()
    {
        var handler = new StubHttpMessageHandler();
        for (var i = 0; i < 5; i++) handler.Enqueue(HttpStatusCode.BadGateway, BadGateway, headers: RetryNow);
        using var client = CreateClient(handler);

        await Assert.ThrowsAsync<ServerException>(() => client.Ads.Drafts.LaunchAsync("g-1", 10m));
        Assert.Single(handler.Requests); // a launch must not be retried after a 502

        await Assert.ThrowsAsync<ServerException>(() => client.Ads.ActivateAsync(7, 25m));
        Assert.Equal(2, handler.Requests.Count); // nor an activation

        await Assert.ThrowsAsync<ServerException>(() => client.Ads.ActivateMultiAsync(new long[] { 7, 8 }, 25m));
        await Assert.ThrowsAsync<ServerException>(() => client.Ads.ResumeAsync(7));
        await Assert.ThrowsAsync<ServerException>(
            () => client.Ads.UpdateAsync(7, new AdCampaignUpdateParams { ConfirmBudgetAmount = 30m }));
        Assert.Equal(5, handler.Requests.Count); // activate multi, resume and update: once each
    }

    [Fact]
    public async Task A_launch_is_sent_once_after_a_connection_error_or_a_timeout()
    {
        var handler = new StubHttpMessageHandler();
        handler.EnqueueConnectionError();
        handler.EnqueueDelayed(TimeSpan.FromSeconds(30), HttpStatusCode.OK, "{\"data\":{}}");
        using var client = CreateClient(handler, timeout: TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAsync<ApiConnectionException>(() => client.Ads.Drafts.LaunchAsync("g-1", 10m));
        Assert.Single(handler.Requests);

        var ex = await Assert.ThrowsAsync<ApiConnectionException>(() => client.Ads.ActivateAsync(7, 25m));
        Assert.Contains("timed out", ex.Message);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task A_read_is_retried_after_a_502()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.BadGateway, BadGateway, headers: RetryNow);
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"create_networks\":[\"meta\"]}}");
        using var client = CreateClient(handler);

        var caps = await client.Ads.CapabilitiesAsync();

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("meta", caps!.Value.GetProperty("data").GetProperty("create_networks")[0].GetString());
    }

    [Fact]
    public async Task A_launch_is_retried_after_a_429()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.TooManyRequests,
            "{\"error\":{\"code\":\"rate_limit_exceeded\",\"message\":\"slow down\"}}",
            headers: RetryNow);
        handler.Enqueue(HttpStatusCode.OK,
            "{\"data\":{\"group\":{\"group_id\":\"g-1\",\"status\":\"launched\",\"campaigns\":[]}," +
            "\"results\":[{\"campaign_id\":\"41\",\"platform\":\"meta\",\"ok\":true}," +
            "{\"campaign_id\":42,\"platform\":\"linkedin\",\"ok\":false,\"code\":\"billing_missing\",\"error\":\"Add a payment method\"}]}}");
        using var client = CreateClient(handler);

        var launched = await client.Ads.Drafts.LaunchAsync("g-1", 10m);

        Assert.Equal(2, handler.Requests.Count); // a 429 means nothing started
        var typed = launched!.Value.Deserialize<AdDraftLaunchResponse>()!;
        Assert.Equal("launched", typed.Data.Group.Status);
        Assert.Equal(41, typed.Data.Results[0].CampaignId); // the API may send ids as strings
        Assert.False(typed.Data.Results[1].Ok);
        Assert.Equal("billing_missing", typed.Data.Results[1].Code);
    }

    [Fact]
    public void Typed_models_read_a_draft_and_a_campaign()
    {
        var draft = Parse(
            "{\"data\":{\"group_id\":\"g-1\",\"name\":\"Autumn sale\",\"objective\":\"traffic\",\"status\":\"draft\"," +
            "\"can_launch\":true,\"budget\":{\"type\":\"daily\",\"amount\":10,\"currency\":\"EUR\"}," +
            "\"schedule\":{\"duration_days\":null},\"targeting\":{\"countries\":[\"NL\"],\"automatic_audience\":true}," +
            "\"ads\":[{\"key\":\"a1\",\"media\":[{\"url\":\"https://example.com/a.jpg\",\"type\":\"image\"}]," +
            "\"primary_text\":\"Hi\",\"link\":\"https://example.com\"}]," +
            "\"campaigns\":[{\"id\":\"41\",\"platform\":\"meta\",\"status\":\"draft\",\"editable\":true," +
            "\"budget\":{\"type\":\"daily\",\"amount\":10,\"currency\":\"EUR\"}," +
            "\"ad_account\":{\"id\":\"12\",\"name\":\"Shop\",\"currency\":\"EUR\",\"billing_ok\":true,\"needs_reauth\":false}," +
            "\"errors\":null,\"readiness\":{\"ready\":false,\"issues\":[{\"field\":\"ads\",\"code\":\"media_missing\",\"message\":\"Add a picture\"}],\"ads\":[]}}]," +
            "\"review_url\":\"https://app.omnisocials.com/ads/create/g-1\"}}");

        var typed = draft.Deserialize<AdDraftResponse>()!.Data;

        Assert.Equal("g-1", typed.GroupId);
        Assert.True(typed.CanLaunch);
        Assert.Equal(10m, typed.Budget!.Amount);
        Assert.Null(typed.Schedule!.DurationDays);
        Assert.Equal("NL", typed.Targeting!.Countries.First());
        Assert.Equal("https://example.com/a.jpg", typed.Ads[0].Media.First().Url);
        Assert.Equal(41, typed.Campaigns[0].Id);
        Assert.Equal(12, typed.Campaigns[0].AdAccount!.Id);
        Assert.Equal("media_missing", typed.Campaigns[0].Readiness!.Issues[0].Code);

        var campaign = Parse(
            "{\"data\":{\"id\":7,\"name\":\"Boost\",\"platform\":\"meta\",\"campaign_type\":\"boost\",\"status\":\"active\"," +
            "\"budget\":{\"type\":\"daily\",\"amount\":\"5.50\",\"currency\":\"EUR\"}," +
            "\"metrics\":{\"spend\":12.4,\"impressions\":3100},\"external_campaign_id\":\"238\",\"group_id\":null," +
            "\"createdAt\":\"2026-10-20T09:00:00.000Z\"}}");

        var c = campaign.Deserialize<AdCampaignResponse>()!.Data;

        Assert.Equal(7, c.Id);
        Assert.Equal(5.50m, c.Budget!.Amount);
        Assert.Equal(3100, c.Metrics!["impressions"].GetInt32());
        Assert.Equal("2026-10-20T09:00:00.000Z", c.CreatedAt);

        var snakeCase = Parse("{\"id\":\"8\",\"platform\":\"x\",\"status\":\"draft\",\"created_at\":\"2026-10-21T10:00:00Z\"}");
        Assert.Equal("2026-10-21T10:00:00Z", snakeCase.Deserialize<AdCampaign>()!.CreatedAt);
        Assert.DoesNotContain("created_at", JsonSerializer.Serialize(snakeCase.Deserialize<AdCampaign>()));
    }

    [Fact]
    public async Task A_draft_without_an_end_sends_duration_days_null()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{}}");
        using var client = CreateClient(handler);

        await client.Ads.Drafts.CreateAsync(new AdDraftParams { Schedule = new AdDraftSchedule() });

        var schedule = Parse(handler.RequestBodies[0]!).GetProperty("schedule");
        Assert.Equal(JsonValueKind.Null, schedule.GetProperty("duration_days").ValueKind);
    }
}
