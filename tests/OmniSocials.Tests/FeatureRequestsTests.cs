using System.Net;
using System.Text.Json;
using Xunit;

namespace OmniSocials.Tests;

/// <summary>Request paths, queries, bodies and typed models of the limits and feature request endpoints.</summary>
public class FeatureRequestsTests
{
    private const string LimitJson =
        "{\"id\":\"tiktok.delete\",\"kind\":\"platform\",\"kind_label\":\"The platform does not allow it\"," +
        "\"area\":\"posts\",\"platforms\":[\"tiktok\"],\"title\":\"Delete a TikTok video\"," +
        "\"says\":\"TikTok's API cannot delete a video.\",\"instead\":\"Delete the video in the TikTok app.\",\"requestable\":false}";

    private const string RequestJson =
        "{\"id\":412,\"title\":\"Post to a topic of a Telegram group\"," +
        "\"summary\":\"Post our daily update to the Announcements topic.\",\"status\":\"open\",\"team_answer\":null," +
        "\"team_replies\":[{\"message\":\"On the list.\",\"created_at\":\"2026-11-05T09:00:00.000Z\"}],\"votes_count\":2," +
        "\"limit_id\":\"telegram.forum_topics\",\"platform\":\"telegram\",\"area\":\"posts\",\"source\":\"api\"," +
        "\"created_at\":\"2026-11-04T10:00:00.000Z\",\"updated_at\":\"2026-11-05T09:00:00.000Z\",\"completed_at\":null}";

    private static OmniSocialsClient CreateClient(StubHttpMessageHandler handler)
        => new(new OmniSocialsOptions
        {
            ApiKey = "omsk_test_key",
            BaseUrl = "https://api.test.local/v1",
            MaxRetries = 0,
            HttpMessageHandler = handler,
        });

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();

    [Fact]
    public async Task Limits_list_sends_the_query_and_reads_the_entries()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK,
            "{\"data\":[" + LimitJson + "],\"kinds\":{\"platform\":\"The platform does not allow it\"}}");
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":[],\"kinds\":{}}");
        using var client = CreateClient(handler);

        var limits = await client.Limits.ListAsync(new LimitListParams { Platform = "tiktok", Area = "posts" });
        await client.Limits.ListAsync();

        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal("https://api.test.local/v1/limits?platform=tiktok&area=posts", handler.Requests[0].RequestUri!.ToString());
        Assert.Equal("https://api.test.local/v1/limits", handler.Requests[1].RequestUri!.ToString());

        var typed = limits!.Value.Deserialize<LimitListResponse>()!;
        Assert.Equal("tiktok.delete", typed.Data[0].Id);
        Assert.Equal("platform", typed.Data[0].Kind);
        Assert.False(typed.Data[0].Requestable);
        Assert.Equal("Delete the video in the TikTok app.", typed.Data[0].Instead);
        Assert.Equal("tiktok", typed.Data[0].Platforms[0]);
        Assert.Equal("The platform does not allow it", typed.Kinds["platform"]);
    }

    [Fact]
    public async Task Feature_requests_send_the_right_requests_and_keep_the_outcome()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Created, "{\"outcome\":\"voted\",\"data\":" + RequestJson + "}");
        handler.Enqueue(HttpStatusCode.OK, "{\"outcome\":\"already_requested\",\"data\":" + RequestJson + "}");
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":[" + RequestJson + "]}");
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":" + RequestJson + "}");
        using var client = CreateClient(handler);

        var voted = await client.FeatureRequests.CreateAsync(new FeatureRequestCreateParams
        {
            Summary = "Post our daily update to the Announcements topic.",
            Platform = "telegram",
            LimitId = "telegram.forum_topics",
        });
        var again = await client.FeatureRequests.CreateAsync(new FeatureRequestCreateParams { Summary = "Post to a Telegram topic, please." });
        var mine = await client.FeatureRequests.ListAsync();
        var one = await client.FeatureRequests.GetAsync(412);

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("https://api.test.local/v1/feature-requests", handler.Requests[0].RequestUri!.ToString());
        var body = Parse(handler.RequestBodies[0]!);
        Assert.Equal("Post our daily update to the Announcements topic.", body.GetProperty("summary").GetString());
        Assert.Equal("telegram", body.GetProperty("platform").GetString());
        Assert.Equal("telegram.forum_topics", body.GetProperty("limit_id").GetString());
        Assert.False(body.TryGetProperty("title", out _));
        Assert.False(body.TryGetProperty("area", out _));
        Assert.False(body.TryGetProperty("source", out _));

        Assert.Equal(HttpMethod.Get, handler.Requests[2].Method);
        Assert.Equal("https://api.test.local/v1/feature-requests", handler.Requests[2].RequestUri!.ToString());
        Assert.Equal("https://api.test.local/v1/feature-requests/412", handler.Requests[3].RequestUri!.ToString());

        var created = voted!.Value.Deserialize<FeatureRequestCreateResponse>()!;
        Assert.Equal("voted", created.Outcome);
        Assert.Equal(412, created.Data.Id);
        Assert.Equal(2, created.Data.VotesCount);
        Assert.Null(created.Data.TeamAnswer);
        Assert.Equal("On the list.", created.Data.TeamReplies[0].Message);
        Assert.Equal("already_requested", again!.Value.GetProperty("outcome").GetString());
        Assert.Equal("open", mine!.Value.Deserialize<FeatureRequestListResponse>()!.Data[0].Status);
        var request = one!.Value.Deserialize<FeatureRequestResponse>()!.Data;
        Assert.Equal("telegram.forum_topics", request.LimitId);
        Assert.Null(request.CompletedAt);
    }

    [Fact]
    public async Task Not_requestable_keeps_the_limit_in_the_body()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Conflict,
            "{\"error\":{\"code\":\"not_requestable\",\"message\":\"TikTok's API cannot delete a video. A request cannot change this, so nothing was sent.\",\"limit\":" + LimitJson + "}}");
        using var client = CreateClient(handler);

        var error = await Assert.ThrowsAnyAsync<ApiException>(
            () => client.FeatureRequests.CreateAsync(new FeatureRequestCreateParams
            {
                Summary = "Delete my TikTok videos from OmniSocials.",
                LimitId = "tiktok.delete",
            }));

        Assert.Equal(409, error.Status);
        Assert.Equal("not_requestable", error.Code);
        var limit = error.Body!.Value.GetProperty("error").GetProperty("limit").Deserialize<Limit>()!;
        Assert.Equal("Delete the video in the TikTok app.", limit.Instead);
        Assert.Single(handler.Requests);
    }
}
