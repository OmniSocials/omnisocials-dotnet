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
        });

        var body = Parse(handler.RequestBodies[0]!);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("x").GetProperty("thread_parts").ValueKind);
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
