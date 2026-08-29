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
    /// Filter by <c>platform</c>
    /// ("instagram"/"facebook"/"linkedin"/"tiktok"/"youtube"/"x"/"threads"),
    /// <c>type</c> ("dm"/"comment"/"mention"), and <c>unread</c>. Threads
    /// conversations are <c>type</c> "comment" (replies people leave on your
    /// Threads posts; conversation ids look like
    /// <c>threads_comment_&lt;rootPostId&gt;</c>) and "mention"
    /// (<c>threads_mention_&lt;postId&gt;</c>); there are no Threads DMs. The
    /// Threads inbox is currently rolling out: until Meta approves the
    /// permissions it is disabled on production, and it needs a Threads
    /// connection with the reply permission. Uses cursor pagination: pass the
    /// previous response's <c>pagination.next_cursor</c> as
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
    ///
    /// On a Threads conversation the reply publishes as a native Threads reply.
    /// The Threads inbox is currently rolling out: until Meta approves the
    /// permissions it is disabled on production, and it needs a Threads
    /// connection with the reply permission. When the Threads connection lacks
    /// that permission this throws a 401 <see cref="AuthenticationException"/>
    /// with code <c>reauth_required</c> (reconnect Threads to fix it).
    ///
    /// X DM replies cost 2 prepaid credits per send, debited from the company
    /// balance before the send and auto-refunded if the send fails. Two 402
    /// <see cref="ApiException"/> codes can result (402 has no dedicated
    /// subclass, so check <c>Status</c>/<c>Code</c>): <c>insufficient_credits</c>
    /// (the balance can't cover the 2 credits) and <c>x_inbox_suspended</c>
    /// (the workspace's X inbox auto-suspended when the balance hit zero - top
    /// up and re-enable it in the dashboard to resume; DMs that arrive while
    /// suspended are not recovered).
    /// </summary>
    public Task<JsonElement?> ReplyAsync(string conversationId, InboxReplyParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync($"/inbox/conversations/{Uri.EscapeDataString(conversationId)}/reply", parameters, cancellationToken);

    /// <summary>
    /// <c>POST /inbox/messages/:id/hide</c>: hide (<paramref name="hide"/> true,
    /// the default) or unhide (false) a reply someone left on one of your
    /// Threads posts, as the post owner (scope <c>inbox:write</c>). Threads only
    /// for now, and only incoming top-level replies can be hidden (Threads does
    /// not allow hiding nested replies); the message keeps its place in the
    /// conversation. Returns <c>{ "data": &lt;message&gt; }</c> with
    /// <c>hidden</c> flipped. Errors: 400 <c>unsupported_platform</c> (not an
    /// incoming Threads reply, or the Threads inbox is not available yet), 400
    /// <c>not_hideable</c> (nested reply or Threads refused), 401
    /// <c>reauth_required</c> (the connection lacks the reply permission), 404
    /// <c>not_found</c> (message not in this workspace) or
    /// <c>account_not_connected</c> (no Threads account). The Threads inbox is
    /// currently rolling out: until Meta approves the permissions it is
    /// disabled on production and calls return a clear error.
    /// <paramref name="messageId"/> is URL-encoded for you.
    /// </summary>
    public Task<JsonElement?> HideAsync(string messageId, bool hide = true, CancellationToken cancellationToken = default)
        => _client.PostAsync($"/inbox/messages/{Uri.EscapeDataString(messageId)}/hide", new { hide }, cancellationToken);
}
