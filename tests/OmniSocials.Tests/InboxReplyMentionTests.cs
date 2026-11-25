using System.Net;
using System.Text.Json;
using Xunit;

namespace OmniSocials.Tests;

/// <summary><c>mention_author</c> on <c>Inbox.ReplyAsync</c>: request body and the <c>mention</c> answer.</summary>
public class InboxReplyMentionTests
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
    public async Task Reply_sends_mention_author_and_reads_mention()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK,
            "{\"data\":{\"id\":\"104970664\",\"text\":\"Jamie Rivera Thanks!\"},"
            + "\"mention\":{\"status\":\"tagged\",\"name\":\"Jamie Rivera\","
            + "\"message\":\"Facebook shows the name as a clickable tag.\"}}");
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"1\"}}");
        using var client = CreateClient(handler);

        var result = await client.Inbox.ReplyAsync("fb_comment_1_2", new InboxReplyParams
        {
            Text = "Thanks!",
            MessageId = "104970331",
            MentionAuthor = true,
        });
        await client.Inbox.ReplyAsync("fb_comment_1_2", new InboxReplyParams { Text = "Thanks!" });

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("/v1/inbox/conversations/fb_comment_1_2/reply", handler.Requests[0].RequestUri!.AbsolutePath);
        var body = Parse(handler.RequestBodies[0]!);
        Assert.True(body.GetProperty("mention_author").GetBoolean());
        Assert.Equal("104970331", body.GetProperty("message_id").GetString());
        Assert.False(Parse(handler.RequestBodies[1]!).TryGetProperty("mention_author", out _));

        var typed = result!.Value.Deserialize<InboxReplyResponse>()!;
        Assert.Equal("tagged", typed.Mention!.Status);
        Assert.Equal("Jamie Rivera", typed.Mention.Name);
    }
}
