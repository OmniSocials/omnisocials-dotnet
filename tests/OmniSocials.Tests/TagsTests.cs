using System.Net;
using System.Text.Json;
using Xunit;

namespace OmniSocials.Tests;

/// <summary>Request paths, bodies, queries and typed models of the post tag endpoints.</summary>
public class TagsTests
{
    private const string TagJson =
        "{\"id\":12,\"name\":\"Question\",\"color\":\"blue\",\"created_at\":\"2026-11-02T10:00:00.000Z\"}";

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
    public async Task Tag_crud_and_apply_send_the_right_requests()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":[" + TagJson + "]}");
        handler.Enqueue(HttpStatusCode.Created, "{\"data\":" + TagJson + "}");
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":" + TagJson + "}");
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"tag\":" + TagJson + ",\"posts_untagged\":3}}");
        handler.Enqueue(HttpStatusCode.OK,
            "{\"data\":{\"updated\":1,\"posts\":[{\"id\":\"1024\",\"tags\":[" + TagJson + "]}],\"not_found\":[\"999\"]}}");
        using var client = CreateClient(handler);

        var list = await client.Tags.ListAsync();
        var created = await client.Tags.CreateAsync(new TagCreateParams { Name = "Question", Color = "blue" });
        await client.Tags.UpdateAsync(12, new TagUpdateParams { Name = "Questions" });
        var deleted = await client.Tags.DeleteAsync("12");
        var applied = await client.Tags.ApplyAsync(new TagApplyParams
        {
            PostIds = new[] { "1024", "999" },
            Add = new List<object> { "Question", 7 },
        });

        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal("https://api.test.local/v1/tags", handler.Requests[0].RequestUri!.ToString());

        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        var createBody = Parse(handler.RequestBodies[1]!);
        Assert.Equal("Question", createBody.GetProperty("name").GetString());
        Assert.Equal("blue", createBody.GetProperty("color").GetString());
        Assert.False(createBody.TryGetProperty("group", out _));

        Assert.Equal(HttpMethod.Patch, handler.Requests[2].Method);
        Assert.Equal("https://api.test.local/v1/tags/12", handler.Requests[2].RequestUri!.ToString());
        var updateBody = Parse(handler.RequestBodies[2]!);
        Assert.Equal("Questions", updateBody.GetProperty("name").GetString());
        Assert.False(updateBody.TryGetProperty("color", out _));

        Assert.Equal(HttpMethod.Delete, handler.Requests[3].Method);
        Assert.Equal("https://api.test.local/v1/tags/12", handler.Requests[3].RequestUri!.ToString());

        Assert.Equal("https://api.test.local/v1/tags/apply", handler.Requests[4].RequestUri!.ToString());
        var applyBody = Parse(handler.RequestBodies[4]!);
        Assert.Equal("1024", applyBody.GetProperty("post_ids")[0].GetString());
        Assert.Equal("Question", applyBody.GetProperty("add")[0].GetString());
        Assert.Equal(7, applyBody.GetProperty("add")[1].GetInt32());
        Assert.False(applyBody.TryGetProperty("remove", out _));
        Assert.False(applyBody.TryGetProperty("set", out _));

        // Optional typed models.
        var tags = list!.Value.Deserialize<TagListResponse>()!.Data;
        Assert.Equal("Question", tags[0].Name);
        Assert.Equal(12, created!.Value.Deserialize<TagResponse>()!.Data.Id);
        Assert.Equal(3, deleted!.Value.Deserialize<TagDeleteResponse>()!.Data.PostsUntagged);
        var result = applied!.Value.Deserialize<TagApplyResponse>()!.Data;
        Assert.Equal(1, result.Updated);
        Assert.Equal("blue", result.Posts[0].Tags[0].Color);
        Assert.Equal("999", result.NotFound[0]);
    }

    [Fact]
    public async Task Tags_serialize_on_post_create_and_update()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"1\"}}");
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"1\"}}");
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"1\"}}");
        using var client = CreateClient(handler);

        await client.Posts.CreateAsync(new PostCreateParams
        {
            Content = "Would you post this?",
            Channels = new[] { "tiktok" },
            Tags = new List<object> { "Question", 12 },
        });
        await client.Posts.UpdateAsync("1", new PostUpdateParams { Tags = new List<object>() });
        await client.Posts.UpdateAsync("1", new PostUpdateParams { Content = "Edited" });

        var create = Parse(handler.RequestBodies[0]!).GetProperty("tags");
        Assert.Equal("Question", create[0].GetString());
        Assert.Equal(12, create[1].GetInt32());

        // An empty list clears the tags; leaving Tags null leaves them untouched.
        Assert.Equal(0, Parse(handler.RequestBodies[1]!).GetProperty("tags").GetArrayLength());
        Assert.False(Parse(handler.RequestBodies[2]!).TryGetProperty("tags", out _));
    }

    [Fact]
    public async Task Tag_filters_and_tag_analytics_send_the_query()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":[]}");
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{}}");
        handler.Enqueue(HttpStatusCode.OK,
            "{\"data\":{\"period\":{\"start_date\":\"2026-08-01T00:00:00.000Z\",\"end_date\":\"2026-10-31T23:59:59.999Z\"}," +
            "\"platform\":\"tiktok\",\"min_posts\":3,\"tags\":[{\"tag\":" + TagJson + ",\"posts\":9,\"measured\":8," +
            "\"median_impressions\":1520,\"engagement_rate\":5.4,\"vs_normal\":0.38,\"best_channel\":\"tiktok\",\"enough_posts\":true}]," +
            "\"untagged\":{\"posts\":4,\"measured\":4,\"median_impressions\":900,\"engagement_rate\":3.1,\"vs_normal\":-0.1," +
            "\"best_channel\":null,\"enough_posts\":false},\"normals\":{\"tiktok\":1100,\"x\":null}}}");
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{}}");
        using var client = CreateClient(handler);

        await client.Posts.ListAsync(new PostListParams { Tag = "Question,7" });
        await client.Analytics.OverviewAsync(new AnalyticsOverviewParams { Period = "30d", Tag = "Question" });
        var compare = await client.Analytics.TagsAsync(new TagAnalyticsParams
        {
            StartDate = "2026-08-01",
            EndDate = "2026-10-31",
            Platform = "tiktok",
            MinPosts = 3,
        });
        await client.Analytics.TagsAsync();

        Assert.Equal("https://api.test.local/v1/posts?tag=Question%2C7", handler.Requests[0].RequestUri!.ToString());
        Assert.Equal("https://api.test.local/v1/analytics/overview?period=30d&tag=Question", handler.Requests[1].RequestUri!.ToString());
        Assert.Equal(
            "https://api.test.local/v1/analytics/tags?start_date=2026-08-01&end_date=2026-10-31&platform=tiktok&min_posts=3",
            handler.Requests[2].RequestUri!.ToString());
        Assert.Equal("https://api.test.local/v1/analytics/tags", handler.Requests[3].RequestUri!.ToString());

        var data = compare!.Value.GetProperty("data");
        Assert.False(data.TryGetProperty("groups", out _));
        var typed = compare.Value.Deserialize<TagAnalyticsResponse>()!.Data;
        Assert.Equal(3, typed.MinPosts);
        Assert.Equal("Question", typed.Tags[0].Tag.Name);
        Assert.Equal(0.38, typed.Tags[0].VsNormal);
        Assert.True(typed.Tags[0].EnoughPosts);
        Assert.Null(typed.Untagged.BestChannel);
        Assert.False(typed.Untagged.EnoughPosts);
        Assert.Equal(1100, typed.Normals["tiktok"]);
        Assert.Null(typed.Normals["x"]);
    }

    [Fact]
    public async Task Tag_exists_keeps_the_existing_tag_in_the_body()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Conflict,
            "{\"error\":{\"code\":\"tag_exists\",\"message\":\"A tag with this name exists.\",\"tag\":" + TagJson + "}}");
        using var client = CreateClient(handler);

        var error = await Assert.ThrowsAnyAsync<ApiException>(
            () => client.Tags.CreateAsync(new TagCreateParams { Name = "Question" }));

        Assert.Equal(409, error.Status);
        Assert.Equal("tag_exists", error.Code);
        Assert.Equal(12, error.Body!.Value.GetProperty("error").GetProperty("tag").GetProperty("id").GetInt32());
    }

    [Fact]
    public void Trial_reel_model_reads_the_strategy()
    {
        var post = Parse("{\"id\":\"1\",\"tags\":[" + TagJson + "],\"trial_reel\":{\"strategy\":\"SS_PERFORMANCE\"}}");
        Assert.Equal("SS_PERFORMANCE", post.GetProperty("trial_reel").Deserialize<TrialReel>()!.Strategy);
        Assert.Equal("Question", post.GetProperty("tags").Deserialize<List<Tag>>()![0].Name);
    }
}
