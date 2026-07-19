using System.Text.Json;

namespace OmniSocials;

/// <summary>
/// Social inbox: conversations (DMs, comments, mentions) across connected
/// platforms, their message threads, and replies. The list endpoints use cursor
/// pagination (see <see cref="InboxCursorPagination"/>), unlike the offset-based
/// paging elsewhere in the API.
/// </summary>
public sealed class InboxResource
{
    private readonly OmniSocialsClient _client;

    internal InboxResource(OmniSocialsClient client) => _client = client;

    /// <summary>
    /// <c>GET /inbox/conversations</c>: list social inbox conversations (DMs,
    /// comments, mentions) across connected platforms, newest activity first.
    /// Filter by <c>platform</c> ("instagram"/"facebook"/"linkedin"), <c>type</c>
    /// ("dm"/"comment"/"mention"), and <c>unread</c>. Uses cursor pagination:
    /// pass the previous response's <c>pagination.next_cursor</c> as
    /// <see cref="InboxConversationListParams.Cursor"/> to keep paging while
    /// <c>pagination.has_more</c> is true.
    /// </summary>
    public Task<JsonElement?> ListConversationsAsync(InboxConversationListParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var query = new List<KeyValuePair<string, string?>>
        {
            new("platform", parameters?.Platform),
            new("type", parameters?.Type),
            new("unread", parameters?.Unread is bool unread ? (unread ? "true" : "false") : null),
            new("limit", parameters?.Limit?.ToString()),
            new("cursor", parameters?.Cursor),
        };
        return _client.GetAsync("/inbox/conversations", query, cancellationToken);
    }

    /// <summary>
    /// <c>GET /inbox/conversations/:id/messages</c>: the full message thread for
    /// one conversation, newest first. Uses cursor pagination (<c>limit</c> /
    /// <c>cursor</c>). <paramref name="conversationId"/> is URL-encoded for you,
    /// so pass it exactly as returned (LinkedIn ids contain ":" and "()").
    /// </summary>
    public Task<JsonElement?> GetMessagesAsync(string conversationId, InboxMessageListParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var query = new List<KeyValuePair<string, string?>>
        {
            new("limit", parameters?.Limit?.ToString()),
            new("cursor", parameters?.Cursor),
        };
        return _client.GetAsync($"/inbox/conversations/{Uri.EscapeDataString(conversationId)}/messages", query, cancellationToken);
    }

    /// <summary>
    /// <c>POST /inbox/conversations/:id/read</c>: mark every message in the
    /// conversation as read. Returns <c>{ conversation_id, marked_read }</c>,
    /// where <c>marked_read</c> is the count of messages newly marked read.
    /// <paramref name="conversationId"/> is URL-encoded for you.
    /// </summary>
    public Task<JsonElement?> MarkReadAsync(string conversationId, CancellationToken cancellationToken = default)
        => _client.PostAsync($"/inbox/conversations/{Uri.EscapeDataString(conversationId)}/read", null, cancellationToken);

    /// <summary>
    /// <c>POST /inbox/conversations/:id/reply</c>: send a reply into the
    /// conversation (a DM message, or a reply to the comment/mention). Optionally
    /// attach a single media asset by public URL with
    /// <see cref="InboxReplyParams.AttachmentUrl"/> +
    /// <see cref="InboxReplyParams.AttachmentType"/>. Returns the created outbound
    /// message. <paramref name="conversationId"/> is URL-encoded for you.
    /// </summary>
    public Task<JsonElement?> ReplyAsync(string conversationId, InboxReplyParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync($"/inbox/conversations/{Uri.EscapeDataString(conversationId)}/reply", parameters, cancellationToken);
}
