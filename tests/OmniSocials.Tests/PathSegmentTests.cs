using System.Net;
using Xunit;

namespace OmniSocials.Tests;

/// <summary>
/// An id stays inside one encoded path segment, and an id of "", "." or ".." is
/// refused before anything is sent: URL normalisation would drop that segment or
/// climb a level and reach another endpoint of the same API key.
/// </summary>
public class PathSegmentTests
{
    private static OmniSocialsClient CreateClient(StubHttpMessageHandler handler)
        => new(new OmniSocialsOptions
        {
            ApiKey = "omsk_test_key",
            BaseUrl = "https://api.test.local/v1",
            MaxRetries = 0,
            HttpMessageHandler = handler,
        });

    [Fact]
    public async Task A_hostile_id_stays_inside_one_segment()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{}}");
        using var client = CreateClient(handler);

        await client.Posts.PublishAsync("../webhooks/wh_fake/rotate-secret#");

        Assert.Equal(
            "/v1/posts/..%2Fwebhooks%2Fwh_fake%2Frotate-secret%23/publish",
            handler.Requests[0].RequestUri!.AbsolutePath);
    }

    [Theory]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("..")]
    public async Task Dot_and_empty_ids_are_refused_before_sending(string bad)
    {
        var handler = new StubHttpMessageHandler();
        using var client = CreateClient(handler);

        var calls = new Func<Task>[]
        {
            () => client.Posts.DeleteAsync(bad),
            () => client.Webhooks.RotateSecretAsync(bad),
            () => client.Inbox.MarkReadAsync(bad),
            () => client.Ads.Drafts.GetAsync(bad),
        };
        foreach (var call in calls)
        {
            var error = await Assert.ThrowsAsync<ArgumentException>(call);
            Assert.StartsWith("Invalid request path", error.Message);
        }
        Assert.Empty(handler.Requests);
    }
}
