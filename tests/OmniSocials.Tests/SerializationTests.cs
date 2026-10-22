using System.Net;
using System.Text.Json;
using Xunit;

namespace OmniSocials.Tests;

public class SerializationTests
{
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
    public async Task Null_properties_are_omitted_from_the_body()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"1\"}}");
        using var client = CreateClient(handler);

        await client.Posts.CreateAsync(new PostCreateParams
        {
            Content = "Hello",
            Channels = new[] { "instagram" },
        });

        var body = Parse(handler.RequestBodies[0]!);
        Assert.Equal("Hello", body.GetProperty("content").GetString());
        Assert.Equal("instagram", body.GetProperty("channels")[0].GetString());
        Assert.False(body.TryGetProperty("scheduled_at", out _));
        Assert.False(body.TryGetProperty("media_ids", out _));
        Assert.False(body.TryGetProperty("x", out _));
    }

    [Fact]
    public async Task Draft_false_is_sent_on_update()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"1\",\"status\":\"scheduled\"}}");
        using var client = CreateClient(handler);

        // Scheduling a planned draft sends draft:false, so the false must
        // survive the null-omitting serializer.
        await client.Posts.UpdateAsync("1", new PostUpdateParams { Draft = false });

        var body = Parse(handler.RequestBodies[0]!);
        Assert.False(body.GetProperty("draft").GetBoolean());
    }

    [Fact]
    public async Task Discord_update_keeps_explicit_nulls()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"1\"}}");
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"1\"}}");
        using var client = CreateClient(handler);

        // A key set to null removes that Discord option.
        await client.Posts.UpdateAsync("1", new PostUpdateParams
        {
            Discord = new Dictionary<string, object?> { ["poll"] = null, ["silent"] = true },
        });
        var merge = Parse(handler.RequestBodies[0]!);
        Assert.Equal(JsonValueKind.Null, merge.GetProperty("discord").GetProperty("poll").ValueKind);
        Assert.True(merge.GetProperty("discord").GetProperty("silent").GetBoolean());

        // RemoveDiscord sends "discord": null, which removes every option.
        await client.Posts.UpdateAsync("1", new PostUpdateParams { RemoveDiscord = true });
        var remove = Parse(handler.RequestBodies[1]!);
        Assert.Equal(JsonValueKind.Null, remove.GetProperty("discord").ValueKind);
        Assert.False(remove.TryGetProperty("comment_dm_automation", out _));
        Assert.False(remove.TryGetProperty("RemoveDiscord", out _));
    }

    [Fact]
    public async Task Media_fit_is_sent_as_a_per_channel_map()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"1\",\"status\":\"scheduled\"},\"warnings\":[{\"code\":\"image_fit\",\"message\":\"Kept.\",\"platform\":\"pinterest\",\"action\":\"kept\"}]}");
        using var client = CreateClient(handler);

        var result = await client.Posts.UpdateAsync("1", new PostUpdateParams
        {
            MediaFit = new Dictionary<string, string> { ["instagram"] = "keep", ["pinterest"] = "crop" },
        });

        var body = Parse(handler.RequestBodies[0]!);
        Assert.Equal("keep", body.GetProperty("media_fit").GetProperty("instagram").GetString());
        Assert.Equal("crop", body.GetProperty("media_fit").GetProperty("pinterest").GetString());
        // The update response carries image_fit warnings like a create.
        Assert.Equal("image_fit", result!.Value.GetProperty("warnings")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Snake_case_property_names_are_used_on_the_wire()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"1\"}}");
        using var client = CreateClient(handler);

        await client.Posts.CreateAsync(new PostCreateParams
        {
            Content = "Hello",
            Channels = new[] { "instagram", "linkedin_page" },
            ScheduledAt = "2026-08-01T09:00:00Z",
            MediaUrls = new[] { "https://example.com/a.jpg" },
            LinkUrl = "https://example.com",
            LocationId = "12345",
            UserTags = new[] { new UserTag { Username = "someone", X = 0.5, Y = 0.5, ImageIndex = 0 } },
            LinkedinPage = new Dictionary<string, object?> { ["visibility"] = "PUBLIC" },
        });

        var body = Parse(handler.RequestBodies[0]!);
        Assert.Equal("2026-08-01T09:00:00Z", body.GetProperty("scheduled_at").GetString());
        Assert.Equal("https://example.com/a.jpg", body.GetProperty("media_urls")[0].GetString());
        Assert.Equal("https://example.com", body.GetProperty("link_url").GetString());
        Assert.Equal("12345", body.GetProperty("location_id").GetString());
        Assert.Equal("someone", body.GetProperty("user_tags")[0].GetProperty("username").GetString());
        Assert.Equal(0, body.GetProperty("user_tags")[0].GetProperty("image_index").GetInt32());
        Assert.Equal("PUBLIC", body.GetProperty("linkedin_page").GetProperty("visibility").GetString());
    }

    [Fact]
    public async Task Per_platform_content_and_thread_parts_serialize()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"1\"}}");
        using var client = CreateClient(handler);

        await client.Posts.CreateAsync(new PostCreateParams
        {
            Content = new Dictionary<string, string>
            {
                ["default"] = "Hello",
                ["x"] = "Hello X",
            },
            Channels = new[] { "x" },
            X = new Dictionary<string, object?>
            {
                ["reply_settings"] = "following",
                ["thread_parts"] = new[]
                {
                    new Dictionary<string, object?> { ["text"] = "part one" },
                    new Dictionary<string, object?> { ["text"] = "part two", ["media_urls"] = new[] { "https://example.com/a.jpg" } },
                },
            },
        });

        var body = Parse(handler.RequestBodies[0]!);
        Assert.Equal("Hello X", body.GetProperty("content").GetProperty("x").GetString());
        var x = body.GetProperty("x");
        Assert.Equal("following", x.GetProperty("reply_settings").GetString());
        Assert.Equal(2, x.GetProperty("thread_parts").GetArrayLength());
        Assert.Equal("part two", x.GetProperty("thread_parts")[1].GetProperty("text").GetString());
    }

    [Fact]
    public async Task Explicit_null_thread_parts_survives_serialization_on_update()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"1\"}}");
        using var client = CreateClient(handler);

        await client.Posts.UpdateAsync("1", new PostUpdateParams
        {
            X = new Dictionary<string, object?> { ["thread_parts"] = null },
            Threads = new Dictionary<string, object?> { ["thread_parts"] = null, ["location_id"] = null },
        });

        var body = Parse(handler.RequestBodies[0]!);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("x").GetProperty("thread_parts").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("threads").GetProperty("thread_parts").ValueKind);
        // Explicit null location_id must survive too: it clears the Threads location tag.
        Assert.Equal(JsonValueKind.Null, body.GetProperty("threads").GetProperty("location_id").ValueKind);
    }

    [Fact]
    public async Task Threads_thread_parts_serialize_on_create()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Created, "{\"data\":{\"id\":\"1\"}}");
        using var client = CreateClient(handler);

        await client.Posts.CreateAsync(new PostCreateParams
        {
            Content = "hello",
            Channels = new[] { "threads" },
            Threads = new Dictionary<string, object?>
            {
                ["thread_parts"] = new[]
                {
                    new Dictionary<string, object?> { ["text"] = "part one", ["media_urls"] = new[] { "https://example.com/a.jpg" } },
                    new Dictionary<string, object?> { ["text"] = "part two" },
                },
                ["location_id"] = "17843857450040591",
            },
        });

        var body = Parse(handler.RequestBodies[0]!);
        var threads = body.GetProperty("threads");
        Assert.Equal(2, threads.GetProperty("thread_parts").GetArrayLength());
        Assert.Equal("https://example.com/a.jpg", threads.GetProperty("thread_parts")[0].GetProperty("media_urls")[0].GetString());
        Assert.Equal("part two", threads.GetProperty("thread_parts")[1].GetProperty("text").GetString());
        Assert.Equal("17843857450040591", threads.GetProperty("location_id").GetString());
    }

    [Fact]
    public async Task Inbox_hide_posts_the_hide_flag()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"123\",\"hidden\":true}}");
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"123\",\"hidden\":false}}");
        using var client = CreateClient(handler);

        await client.Inbox.HideAsync("123"); // defaults to hide: true
        await client.Inbox.HideAsync("123", hide: false);

        Assert.Equal("https://api.test.local/v1/inbox/messages/123/hide",
            handler.Requests[0].RequestUri!.OriginalString);
        Assert.True(Parse(handler.RequestBodies[0]!).GetProperty("hide").GetBoolean());
        Assert.False(Parse(handler.RequestBodies[1]!).GetProperty("hide").GetBoolean());
    }

    [Fact]
    public async Task Inbox_next_builds_the_query_and_delete_message_uses_DELETE()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":null,\"remaining\":0}");
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"123\",\"conversation_id\":\"c1\",\"removed_reply_ids\":[]}}");
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"9\"},\"next\":null,\"remaining\":0}");
        using var client = CreateClient(handler);

        await client.Inbox.NextAsync(new InboxNextParams
        {
            Platform = "instagram",
            Type = "comment",
            Order = "newest",
            IncludeRead = true,
            Exclude = new[] { "c1", "c2" },
        });
        await client.Inbox.DeleteMessageAsync("123");
        await client.Inbox.ReplyAsync("c3", new InboxReplyParams { Text = "hi", IncludeNext = true });

        var nextUri = handler.Requests[0].RequestUri!;
        var nextQuery = Uri.UnescapeDataString(nextUri.Query);
        Assert.Equal("/v1/inbox/next", nextUri.AbsolutePath);
        Assert.Contains("platform=instagram", nextQuery);
        Assert.Contains("type=comment", nextQuery);
        Assert.Contains("order=newest", nextQuery);
        Assert.Contains("include_read=true", nextQuery);
        Assert.Contains("exclude=c1,c2", nextQuery);

        Assert.Equal(HttpMethod.Delete, handler.Requests[1].Method);
        Assert.Equal("https://api.test.local/v1/inbox/messages/123",
            handler.Requests[1].RequestUri!.OriginalString);

        Assert.True(Parse(handler.RequestBodies[2]!).GetProperty("include_next").GetBoolean());
    }

    [Fact]
    public async Task Locations_search_overload_builds_platform_and_coordinate_queries()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"locations\":[]}");
        handler.Enqueue(HttpStatusCode.OK, "{\"locations\":[]}");
        using var client = CreateClient(handler);

        await client.Locations.SearchAsync(new LocationSearchParams { Q = "cafe", Platform = "threads" });
        await client.Locations.SearchAsync(new LocationSearchParams
        {
            Platform = "threads",
            Latitude = 52.37,
            Longitude = -4.89,
        });

        Assert.Equal("https://api.test.local/v1/locations/search?q=cafe&platform=threads",
            handler.Requests[0].RequestUri!.OriginalString);
        // Coordinates must serialize with invariant-culture decimal points.
        Assert.Equal("https://api.test.local/v1/locations/search?platform=threads&latitude=52.37&longitude=-4.89",
            handler.Requests[1].RequestUri!.OriginalString);
    }

    [Fact]
    public async Task Pinterest_list_products_builds_the_query_and_validate_sends_the_id()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK,
            "{\"products\":[{\"pin_id\":\"813744226420795884\",\"price\":24.99}],\"bookmark\":null,\"source\":\"catalog\",\"catalog_access\":true}");
        handler.Enqueue(HttpStatusCode.OK,
            "{\"error\":{\"code\":\"pinterest_not_connected\",\"message\":\"No Pinterest account is connected.\"}}");
        handler.Enqueue(HttpStatusCode.OK, "{\"valid\":true,\"pin_id\":\"813744226420795884\"}");
        using var client = CreateClient(handler);

        var list = await client.Pinterest.ListProductsAsync(new PinterestProductListParams
        {
            Source = "catalog",
            ProductGroupId = "443727193917",
            Bookmark = "abc",
            PageSize = 50,
        });
        var notConnected = await client.Pinterest.ListProductsAsync();
        var check = await client.Pinterest.ValidateProductAsync("813744226420795884");

        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal(
            "https://api.test.local/v1/pinterest/products?source=catalog&product_group_id=443727193917&bookmark=abc&page_size=50",
            handler.Requests[0].RequestUri!.OriginalString);
        Assert.Equal("813744226420795884", list!.Value.GetProperty("products")[0].GetProperty("pin_id").GetString());
        Assert.True(list.Value.GetProperty("catalog_access").GetBoolean());

        // No parameters: no query string. A list that could not be read is
        // HTTP 200 with an error object and no products.
        Assert.Equal("https://api.test.local/v1/pinterest/products",
            handler.Requests[1].RequestUri!.OriginalString);
        Assert.False(notConnected!.Value.TryGetProperty("products", out _));
        Assert.Equal("pinterest_not_connected",
            notConnected.Value.GetProperty("error").GetProperty("code").GetString());

        Assert.Equal("https://api.test.local/v1/pinterest/products/validate?id=813744226420795884",
            handler.Requests[2].RequestUri!.OriginalString);
        Assert.True(check!.Value.GetProperty("valid").GetBoolean());
    }

    [Fact]
    public async Task Comment_dm_automation_serializes_on_create_and_null_removes_it_on_update()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK,
            "{\"data\":{\"id\":\"9\",\"status\":\"scheduled\",\"comment_dm_automation\":{\"id\":\"100000004\",\"status\":\"active\",\"post\":null}}}");
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"9\",\"status\":\"scheduled\",\"comment_dm_automation\":null}}");
        handler.Enqueue(HttpStatusCode.OK,
            "{\"data\":[{\"id\":\"1\",\"kind\":\"comment\",\"outcome\":\"sent\"}],\"counts\":{\"sent\":12,\"skipped\":3},\"pagination\":{\"total\":15,\"limit\":25,\"offset\":0,\"has_more\":false}}");
        using var client = CreateClient(handler);

        var created = await client.Posts.CreateAsync(new PostCreateParams
        {
            Content = "Comment GUIDE",
            Channels = new[] { "instagram" },
            CommentDmAutomation = new Dictionary<string, object?>
            {
                ["trigger"] = new { keywords = new[] { "guide" }, match = "word" },
                ["actions"] = new { dm = new { text = "Here is the guide" } },
                ["limits"] = new { max_sends = 500 },
            },
        });

        var body = Parse(handler.RequestBodies[0]!);
        var automation = body.GetProperty("comment_dm_automation");
        Assert.Equal("guide", automation.GetProperty("trigger").GetProperty("keywords")[0].GetString());
        Assert.Equal("Here is the guide", automation.GetProperty("actions").GetProperty("dm").GetProperty("text").GetString());
        Assert.Equal(500, automation.GetProperty("limits").GetProperty("max_sends").GetInt32());
        Assert.Equal("100000004", created!.Value.GetProperty("data").GetProperty("comment_dm_automation").GetProperty("id").GetString());

        // RemoveCommentDmAutomation sends an explicit null, which the
        // null-omitting serializer would otherwise drop; other unset fields
        // stay omitted.
        await client.Posts.UpdateAsync("9", new PostUpdateParams { RemoveCommentDmAutomation = true });

        var update = Parse(handler.RequestBodies[1]!);
        Assert.Equal(JsonValueKind.Null, update.GetProperty("comment_dm_automation").ValueKind);
        Assert.False(update.TryGetProperty("content", out _));
        Assert.False(update.TryGetProperty("RemoveCommentDmAutomation", out _));
        Assert.False(update.TryGetProperty("discord", out _));

        // The events response is not the plain list: counts sit next to data.
        var events = await client.Automations.ListEventsAsync("100000004", new AutomationEventListParams { Outcome = "sent", Limit = 25 });
        Assert.Equal("https://api.test.local/v1/automations/100000004/events?outcome=sent&limit=25",
            handler.Requests[2].RequestUri!.OriginalString);
        Assert.Equal(12, events!.Value.GetProperty("counts").GetProperty("sent").GetInt32());
        Assert.Equal(15, events.Value.GetProperty("pagination").GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Facebook_automation_sends_platform_and_post_id_and_list_filters_on_platform()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":[]}");
        handler.Enqueue(HttpStatusCode.OK,
            "{\"data\":{\"id\":\"100000005\",\"platform\":\"facebook\",\"instagram_media_id\":null,\"facebook_post_id\":\"111_222\"}}");
        using var client = CreateClient(handler);

        await client.Automations.ListAsync(new AutomationListParams { Platform = "facebook" });
        Assert.Equal("https://api.test.local/v1/automations?platform=facebook",
            handler.Requests[0].RequestUri!.OriginalString);

        var created = await client.Automations.CreateAsync(new AutomationCreateParams
        {
            Platform = "facebook",
            FacebookPostId = "111_222",
            Trigger = new Dictionary<string, object?> { ["keywords"] = new[] { "guide" } },
            Actions = new Dictionary<string, object?> { ["dm"] = new { text = "Here is the guide" } },
        });

        var body = Parse(handler.RequestBodies[1]!);
        Assert.Equal("facebook", body.GetProperty("platform").GetString());
        Assert.Equal("111_222", body.GetProperty("facebook_post_id").GetString());
        Assert.False(body.TryGetProperty("instagram_media_id", out _));
        Assert.Equal("111_222", created!.Value.GetProperty("data").GetProperty("facebook_post_id").GetString());
    }

    [Fact]
    public async Task Pinterest_product_tags_serialize_on_post_create()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"1\"}}");
        using var client = CreateClient(handler);

        await client.Posts.CreateAsync(new PostCreateParams
        {
            Content = "Our summer picks",
            Channels = new[] { "pinterest" },
            Pinterest = new Dictionary<string, object?>
            {
                ["board_id"] = "1234567890",
                ["product_tags"] = new[] { "813744226420795884", "813744226420795885" },
            },
        });

        var tags = Parse(handler.RequestBodies[0]!).GetProperty("pinterest").GetProperty("product_tags");
        Assert.Equal(2, tags.GetArrayLength());
        Assert.Equal("813744226420795884", tags[0].GetString());
    }

    [Fact]
    public async Task Hashtag_set_fields_serialize_on_post_create()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"1\"}}");
        handler.Enqueue(HttpStatusCode.Created, "{\"data\":{\"id\":\"hs1\"}}");
        using var client = CreateClient(handler);

        await client.Posts.CreateAsync(new PostCreateParams
        {
            Content = "Launch day!",
            Channels = new[] { "instagram", "x" },
            HashtagSet = "Launch",
            HashtagPlacement = "first_comment",
            HashtagPlatforms = new[] { "instagram" },
        });

        var body = Parse(handler.RequestBodies[0]!);
        Assert.Equal("Launch", body.GetProperty("hashtag_set").GetString());
        Assert.Equal("first_comment", body.GetProperty("hashtag_placement").GetString());
        Assert.Equal("instagram", body.GetProperty("hashtag_platforms")[0].GetString());
        Assert.False(body.TryGetProperty("hashtag_set_id", out _));

        // Create-side: hashtags accepts a string[] (or a single string).
        await client.HashtagSets.CreateAsync(new HashtagSetCreateParams
        {
            Name = "Launch",
            Hashtags = new[] { "saas", "startup" },
        });

        var setBody = Parse(handler.RequestBodies[1]!);
        Assert.Equal("https://api.test.local/v1/hashtag-sets", handler.Requests[1].RequestUri!.ToString());
        Assert.Equal("saas", setBody.GetProperty("hashtags")[0].GetString());
    }

    [Fact]
    public async Task Folder_move_to_top_level_sends_explicit_null_parent_id()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"5\"}}");
        using var client = CreateClient(handler);

        await client.Folders.UpdateAsync("5", new FolderUpdateParams { MoveToTopLevel = true });

        var body = Parse(handler.RequestBodies[0]!);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("parent_id").ValueKind);
        Assert.False(body.TryGetProperty("name", out _));
    }

    [Fact]
    public async Task Media_move_to_root_sends_explicit_null_folder_id()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"9\"}}");
        using var client = CreateClient(handler);

        await client.Media.UpdateAsync("9", new MediaUpdateParams { Name = "hero-v2", MoveToRoot = true });

        var body = Parse(handler.RequestBodies[0]!);
        Assert.Equal("hero-v2", body.GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("folder_id").ValueKind);
    }

    [Fact]
    public async Task Query_parameters_serialize_and_skip_unset_values()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":[]}");
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":[]}");
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{}}");
        using var client = CreateClient(handler);

        await client.Posts.ListAsync(new PostListParams { Status = "scheduled", Limit = 50 });
        await client.Posts.RecentPlatformAsync(new RecentPlatformParams
        {
            Limit = 10,
            Platforms = new[] { "instagram", "x" },
        });
        await client.Analytics.PostsAsync(new[] { "a", "b", "c" });

        Assert.Equal("https://api.test.local/v1/posts?status=scheduled&limit=50",
            handler.Requests[0].RequestUri!.OriginalString);
        Assert.Equal("https://api.test.local/v1/posts/recent-platform?limit=10&platforms=instagram%2Cx",
            handler.Requests[1].RequestUri!.OriginalString);
        Assert.Equal("https://api.test.local/v1/analytics/posts?ids=a%2Cb%2Cc",
            handler.Requests[2].RequestUri!.OriginalString);
    }

    [Fact]
    public async Task Path_ids_are_url_escaped()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{}}");
        using var client = CreateClient(handler);

        await client.Posts.GetAsync("weird/id?x=1");

        Assert.Equal("https://api.test.local/v1/posts/weird%2Fid%3Fx%3D1",
            handler.Requests[0].RequestUri!.OriginalString);
    }

    [Fact]
    public async Task Get_approval_reads_the_review_and_deserializes()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK,
            "{\"data\":{\"post_id\":\"123456\",\"status\":\"rejected\"," +
            "\"workflow\":{\"id\":null,\"name\":\"Content approval\"}," +
            "\"requested_by\":{\"id\":\"7d1f3c52\",\"name\":\"Alex\"}," +
            "\"requested_at\":\"2026-10-01T09:00:00.000Z\",\"current_step\":null," +
            "\"steps\":[{\"order\":1,\"name\":\"Client approval\",\"require_mode\":\"all\",\"status\":\"rejected\"," +
            "\"approvers\":[{\"id\":\"c4a09e1d\",\"name\":\"Jordan\",\"email\":null,\"status\":\"rejected\"," +
            "\"decided_at\":\"2026-10-01T14:30:00.000Z\",\"comment\":\"Wrong product photo\"}]}]," +
            "\"rejection\":{\"by\":{\"id\":\"c4a09e1d\",\"name\":\"Jordan\"},\"reason\":\"Wrong product photo\"," +
            "\"at\":\"2026-10-01T14:30:00.000Z\",\"step\":1}," +
            "\"comments\":[{\"id\":\"5f3a2b1c\",\"author\":null,\"message\":\"Caption edited\"," +
            "\"account\":null,\"created_at\":\"2026-10-01T14:28:00.000Z\"}]}}");
        handler.Enqueue(HttpStatusCode.OK,
            "{\"data\":{\"post_id\":\"1\",\"status\":\"none\",\"workflow\":null,\"requested_by\":null," +
            "\"requested_at\":null,\"current_step\":null,\"steps\":[],\"rejection\":null,\"comments\":[]}}");
        using var client = CreateClient(handler);

        var raw = await client.Posts.GetApprovalAsync("123456");

        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal("https://api.test.local/v1/posts/123456/approval",
            handler.Requests[0].RequestUri!.OriginalString);

        var review = raw!.Value.Deserialize<PostApprovalResponse>()!.Data;
        Assert.Equal("rejected", review.Status);
        Assert.Null(review.Workflow!.Id);
        Assert.Equal("Content approval", review.Workflow.Name);
        Assert.Null(review.CurrentStep);
        Assert.Equal("all", review.Steps[0].RequireMode);
        Assert.Null(review.Steps[0].Approvers[0].Email);
        Assert.Equal("Wrong product photo", review.Steps[0].Approvers[0].Comment);
        Assert.Equal("c4a09e1d", review.Rejection!.By.Id);
        Assert.Equal(1, review.Rejection.Step);
        Assert.Null(review.Comments[0].Author);
        Assert.Null(review.Comments[0].Account);

        var none = (await client.Posts.GetApprovalAsync("1"))!.Value.Deserialize<PostApprovalResponse>()!.Data;
        Assert.Equal("none", none.Status);
        Assert.Null(none.Workflow);
        Assert.Null(none.RequestedBy);
        Assert.Null(none.Rejection);
        Assert.Empty(none.Steps);
        Assert.Empty(none.Comments);
    }

    [Fact]
    public async Task Multipart_upload_sends_file_and_fields()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"m1\"}}");
        using var client = CreateClient(handler);

        await client.Media.UploadAsync(new MediaUploadParams
        {
            Bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 },
            Filename = "product.png",
            Name = "product-hero",
            Folder = "Campaigns",
        });

        var request = handler.Requests[0];
        Assert.Equal("https://api.test.local/v1/media/upload", request.RequestUri!.ToString());
        Assert.StartsWith("multipart/form-data", request.Content!.Headers.ContentType!.ToString());

        var body = handler.RequestBodies[0]!;
        Assert.Contains("product.png", body);
        Assert.Contains("product-hero", body);
        Assert.Contains("Campaigns", body);
    }

    [Fact]
    public async Task Stream_and_file_path_uploads_are_supported()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"m1\"}}");
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"m2\"}}");
        using var client = CreateClient(handler);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        await client.Media.UploadAsync(MediaUploadParams.FromStream(stream, "from-stream.jpg"));

        var tempFile = Path.Combine(Path.GetTempPath(), $"omnisocials-sdk-test-{Guid.NewGuid():N}.jpg");
        await File.WriteAllBytesAsync(tempFile, new byte[] { 4, 5, 6 });
        try
        {
            await client.Media.UploadAsync(MediaUploadParams.FromFile(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }

        Assert.Contains("from-stream.jpg", handler.RequestBodies[0]!);
        Assert.Contains(Path.GetFileName(tempFile), handler.RequestBodies[1]!);
    }

    [Fact]
    public async Task Upload_without_a_file_throws_ArgumentException()
    {
        var handler = new StubHttpMessageHandler();
        using var client = CreateClient(handler);

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.Media.UploadAsync(new MediaUploadParams { Filename = "nothing.png" }));

        Assert.Empty(handler.Requests);
    }
}
