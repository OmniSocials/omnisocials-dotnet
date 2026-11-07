using System.Net;
using System.Text.Json;
using Xunit;

namespace OmniSocials.Tests;

/// <summary>Request bodies of approve and reject, and the 409 step_changed answer.</summary>
public class ApprovalTests
{
    private const string Ok =
        "{\"data\":{\"id\":\"1024\",\"status\":\"in_approval\",\"approval_status\":\"pending\"}}";

    private static OmniSocialsClient CreateClient(StubHttpMessageHandler handler)
        => new(new OmniSocialsOptions
        {
            ApiKey = "omsk_test_key",
            BaseUrl = "https://api.test.local/v1",
            MaxRetries = 0,
            HttpMessageHandler = handler,
        });

    [Fact]
    public async Task Approve_and_reject_send_step_order_only_when_given()
    {
        var handler = new StubHttpMessageHandler();
        for (var i = 0; i < 8; i++)
        {
            handler.Enqueue(HttpStatusCode.OK, Ok);
        }
        using var client = CreateClient(handler);

        await client.Posts.ApproveAsync("1024");
        await client.Posts.ApproveAsync("1024", CancellationToken.None);
        await client.Posts.ApproveAsync("1024", 2);
        await client.Posts.ApproveAsync("1024", null);
        await client.Posts.RejectAsync("1024");
        await client.Posts.RejectAsync("1024", "Wrong CTA link");
        await client.Posts.RejectAsync("1024", "Wrong CTA link", 2);
        await client.Posts.RejectAsync("1024", null, 3);

        Assert.Equal("https://api.test.local/v1/posts/1024/approve", handler.Requests[0].RequestUri!.ToString());
        Assert.Null(handler.RequestBodies[0]);
        Assert.Null(handler.RequestBodies[1]);
        Assert.Equal("{\"step_order\":2}", handler.RequestBodies[2]);
        Assert.Null(handler.RequestBodies[3]);
        Assert.Equal("https://api.test.local/v1/posts/1024/reject", handler.Requests[4].RequestUri!.ToString());
        Assert.Equal("{}", handler.RequestBodies[4]);
        Assert.Equal("{\"comment\":\"Wrong CTA link\"}", handler.RequestBodies[5]);
        Assert.Equal("{\"comment\":\"Wrong CTA link\",\"step_order\":2}", handler.RequestBodies[6]);
        Assert.Equal("{\"step_order\":3}", handler.RequestBodies[7]);
    }

    [Fact]
    public async Task Step_changed_keeps_current_step_order()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Conflict,
            "{\"error\":{\"code\":\"step_changed\",\"message\":\"The review is on step 2 now.\",\"current_step_order\":2}}");
        using var client = CreateClient(handler);

        var err = await Assert.ThrowsAsync<ApiException>(() => client.Posts.ApproveAsync("1024", 1));

        Assert.Equal(409, err.Status);
        Assert.Equal("step_changed", err.Code);
        Assert.Equal(2, err.Body!.Value.GetProperty("error").GetProperty("current_step_order").GetInt32());
    }

    [Fact]
    public void Webhook_model_reads_workspace_id_and_the_switch_off()
    {
        const string json =
            "{\"data\":[" +
            "{\"id\":\"5\",\"url\":\"https://example.com/a\",\"events\":[\"post.published\"],\"workspace_id\":1000401," +
            "\"is_active\":true,\"last_triggered_at\":null,\"failure_count\":0,\"last_failure\":null,\"created_at\":\"2026-11-06T10:00:00.000Z\"}," +
            "{\"id\":\"6\",\"url\":\"https://example.com/b\",\"events\":[\"post.failed\"],\"workspace_id\":null," +
            "\"is_active\":false,\"last_triggered_at\":null,\"failure_count\":0," +
            "\"last_failure\":{\"at\":\"2026-11-06T11:00:00.000Z\",\"status\":null," +
            "\"error\":\"The person who set up this webhook no longer has access to the organisation.\",\"attempts\":0}," +
            "\"created_at\":\"2026-11-06T10:00:00.000Z\"}]}";

        var list = JsonSerializer.Deserialize<WebhookListResponse>(json)!;

        Assert.Equal(1000401, list.Data[0].WorkspaceId);
        Assert.True(list.Data[0].IsActive);
        Assert.Null(list.Data[0].LastFailure);
        var orgWide = list.Data[1];
        Assert.Null(orgWide.WorkspaceId);
        Assert.False(orgWide.IsActive);
        Assert.Null(orgWide.LastFailure!.Status);
        Assert.Equal(0, orgWide.LastFailure.Attempts);
        Assert.Contains("no longer has access", orgWide.LastFailure.Error);
    }
}
