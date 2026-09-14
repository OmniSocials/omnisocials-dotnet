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
    /// <c>type</c> ("dm"/"comment"/"mention"), <c>unread</c>, and
    /// <c>unanswered</c> (only conversations that still need an answer; see
    /// <see cref="InboxConversationListParams.Unanswered"/>). Threads
    /// conversations are <c>type</c> "comment" (replies people leave on your
    /// Threads posts; conversation ids look like
    /// <c>threads_comment_&lt;rootPostId&gt;</c>) and "mention"
    /// (<c>threads_mention_&lt;postId&gt;</c>); there are no Threads DMs. The
    /// Threads inbox needs a Threads connection with the reply permissions;
    /// connections made before those permissions existed must be reconnected
    /// once. Uses cursor pagination: pass the
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
            new("unanswered", parameters?.Unanswered is bool unanswered ? (unanswered ? "true" : "false") : null),
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
    /// The Threads inbox needs a Threads connection with the reply permission.
    /// When the Threads connection lacks that permission (connected before it
    /// existed) this throws a 401 <see cref="AuthenticationException"/>
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
    ///
    /// Set <see cref="InboxReplyParams.IncludeNext"/> to also get <c>next</c>
    /// (the next conversation that needs an answer, the same object
    /// <see cref="NextAsync"/> returns under <c>data</c>, using its default
    /// queue order and filters; null when nothing is waiting) and
    /// <c>remaining</c> in the response. Saves the extra call when working
    /// through the inbox.
    /// </summary>
    public Task<JsonElement?> ReplyAsync(string conversationId, InboxReplyParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync($"/inbox/conversations/{Uri.EscapeDataString(conversationId)}/reply", parameters, cancellationToken);

    /// <summary>
    /// <c>POST /inbox/messages/:id/hide</c>: hide (<paramref name="hide"/> true,
    /// the default) or unhide (false) a comment someone left on one of your
    /// posts, on the platform, as the post owner (scope <c>inbox:write</c>).
    /// Facebook, Instagram, TikTok, YouTube and Threads comments (Threads:
    /// incoming top-level replies only; Threads does not allow hiding nested
    /// replies). On YouTube, hide sets the comment's moderation status to
    /// rejected, which removes it and its replies from public view; unhide
    /// publishes it again. The message keeps its place in the conversation
    /// and the response is <c>{ "data": &lt;message&gt; }</c> with
    /// <c>hidden</c> flipped; a hidden comment no longer counts as
    /// unanswered. The account must have been connected with the moderation
    /// permission (Facebook <c>pages_manage_engagement</c>, Instagram
    /// <c>instagram_business_manage_comments</c>). Errors: 400
    /// <c>unsupported_platform</c> (not an incoming comment on a supported
    /// platform), 400 <c>not_hideable</c> (Threads nested reply, or Threads
    /// refused), 401 <c>reauth_required</c> (the Threads reply permission or
    /// the TikTok comments authorization is missing or expired), 403
    /// <c>reconnect_required</c> (the account was connected without the
    /// comment-moderation permission; reconnect it in the dashboard), 404
    /// <c>not_found</c> (message not in this workspace) or
    /// <c>account_not_connected</c>, 429 <c>quota_exceeded</c> (YouTube's
    /// daily API quota is used up; retry after midnight Pacific), 502
    /// <c>platform_error</c> (the platform rejected the call). The Threads
    /// inbox needs a Threads connection with the reply permissions; a
    /// connection made before those permissions existed answers 401
    /// <c>reauth_required</c> until reconnected.
    /// <paramref name="messageId"/> is URL-encoded for you.
    /// </summary>
    public Task<JsonElement?> HideAsync(string messageId, bool hide = true, CancellationToken cancellationToken = default)
        => _client.PostAsync($"/inbox/messages/{Uri.EscapeDataString(messageId)}/hide", new { hide }, cancellationToken);

    /// <summary>
    /// <c>DELETE /inbox/messages/:id</c>: delete a comment someone left on one
    /// of your posts, on the platform and from the inbox (scope
    /// <c>inbox:write</c>). Facebook, Instagram and TikTok comments only:
    /// YouTube's API does not let a channel delete other people's comments,
    /// hide those instead (<see cref="HideAsync"/>). Replies under the deleted
    /// comment go with it (the platforms cascade the delete and the inbox
    /// mirrors that); their inbox ids come back as <c>removed_reply_ids</c>.
    /// A comment that is already gone on the platform is still removed from
    /// the inbox. This cannot be undone. Returns
    /// <c>{ "data": { id, conversation_id, removed_reply_ids } }</c> (see
    /// <see cref="InboxDeleteMessageResponse"/>). Errors: 400
    /// <c>unsupported_platform</c> (not an incoming Facebook, Instagram or
    /// TikTok comment), 401 <c>reauth_required</c> (the TikTok comments
    /// authorization expired), 403 <c>reconnect_required</c> (the account was
    /// connected without the comment-moderation permission; reconnect it in
    /// the dashboard), 404 <c>not_found</c> (message not in this workspace)
    /// or <c>account_not_connected</c>, 502 <c>platform_error</c> (the
    /// platform rejected the call). <paramref name="messageId"/> is
    /// URL-encoded for you.
    /// </summary>
    public Task<JsonElement?> DeleteMessageAsync(string messageId, CancellationToken cancellationToken = default)
        => _client.DeleteAsync($"/inbox/messages/{Uri.EscapeDataString(messageId)}", cancellationToken);

    /// <summary>
    /// <c>GET /inbox/next</c>: the next conversation that needs an answer, a
    /// work queue for answering the inbox (scope <c>inbox:read</c>). Returns
    /// the oldest (by default) item that still needs a reply, together with
    /// its conversation so far and the post it belongs to, so a reply can be
    /// drafted from one call. An item needs an answer when it is the
    /// customer's latest DM with no reply after it (Instagram/Facebook DMs
    /// within the 24-hour messaging window only, since Meta refuses replies
    /// outside it), or a comment/mention that has not been replied to and is
    /// not hidden. Replies typed in the native apps count as answers (they
    /// are mirrored into the inbox), so a thread a colleague answered on
    /// their phone is not served again. Instagram mentions are skipped (no
    /// reply path). Looks at the last 30 days of activity.
    ///
    /// Only unread items are served by default: marking a conversation read
    /// (<see cref="MarkReadAsync"/>) is how to skip one for good; set
    /// <see cref="InboxNextParams.IncludeRead"/> to include
    /// read-but-unanswered items. <see cref="InboxNextParams.Exclude"/> is a
    /// session-local skip. The response is
    /// <c>{ "data": ..., "remaining": n }</c> (see
    /// <see cref="InboxNextResponse"/>): <c>data</c> is
    /// <c>{ conversation, message, messages }</c>, or null when nothing is
    /// waiting; <c>message</c> is the unanswered incoming item itself, whose
    /// <c>id</c> is what <see cref="HideAsync"/> and
    /// <see cref="DeleteMessageAsync"/> take and whose <c>conversation_id</c>
    /// is what <see cref="ReplyAsync"/> takes; <c>messages</c> is the
    /// conversation so far, oldest first; <c>remaining</c> counts the
    /// unanswered items still waiting after this one (capped at 500), 0 when
    /// <c>data</c> is null. To chain the queue, set
    /// <see cref="InboxReplyParams.IncludeNext"/> on <see cref="ReplyAsync"/>
    /// and it returns the next item in the same response. Errors: 400
    /// <c>validation_error</c> (unknown platform, type or order).
    /// </summary>
    public Task<JsonElement?> NextAsync(InboxNextParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var exclude = parameters?.Exclude is { Count: > 0 } ids ? string.Join(",", ids) : null;
        var query = new List<KeyValuePair<string, string?>>
        {
            new("platform", parameters?.Platform),
            new("type", parameters?.Type),
            new("order", parameters?.Order),
            new("include_read", parameters?.IncludeRead is bool includeRead ? (includeRead ? "true" : "false") : null),
            new("exclude", exclude),
        };
        return _client.GetAsync("/inbox/next", query, cancellationToken);
    }
}
