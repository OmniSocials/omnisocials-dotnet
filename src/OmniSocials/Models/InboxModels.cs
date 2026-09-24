using System.Text.Json.Serialization;

namespace OmniSocials;

// Typed response models for the inbox endpoints. Like every resource, the
// InboxResource methods return the raw JsonElement? so callers can read the
// payload directly. These classes are optional, strongly-typed deserialization
// targets: e.g. (await client.Inbox.ListConversationsAsync())
//   ?.Deserialize<InboxConversationsResponse>(). They mirror the API's snake_case
// fields via [JsonPropertyName].

/// <summary>
/// Cursor-based pagination used by the inbox list endpoints. Unlike offset
/// pagination, page on by passing <see cref="NextCursor"/> back as the request's
/// cursor while <see cref="HasMore"/> is true. <see cref="NextCursor"/> is null on
/// the last page.
/// </summary>
public sealed class InboxCursorPagination
{
    [JsonPropertyName("next_cursor")]
    public string? NextCursor { get; set; }

    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }
}

/// <summary>A person on the other side of a conversation (or the sender of a message).</summary>
public sealed class InboxParticipant
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("profile_picture")]
    public string? ProfilePicture { get; set; }
}

/// <summary>
/// The post a comment/mention conversation is attached to (null for DMs), so a
/// reply can be drafted with the post in view.
/// </summary>
public sealed class InboxPostRef
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("caption")]
    public string? Caption { get; set; }

    /// <summary>Image URL of the post (or the video's cover).</summary>
    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    /// <summary>
    /// Public link to the post when the platform provides one (Instagram,
    /// Facebook, YouTube, TikTok, LinkedIn, Threads); null otherwise.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// The platform's own media label when known (e.g. "IMAGE", "VIDEO",
    /// "CAROUSEL_ALBUM" on Instagram); null otherwise.
    /// </summary>
    [JsonPropertyName("media_type")]
    public string? MediaType { get; set; }
}

/// <summary>The most recent message summary shown on a conversation.</summary>
public sealed class InboxLastMessage
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    /// <summary>"incoming" (from the participant) or "outgoing" (from you).</summary>
    [JsonPropertyName("direction")]
    public string Direction { get; set; } = "";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = "";

    [JsonPropertyName("is_read")]
    public bool IsRead { get; set; }
}

/// <summary>A social inbox conversation: a DM thread, a comment, or a mention.</summary>
public sealed class InboxConversation
{
    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; } = "";

    /// <summary>Platform identifier, e.g. "instagram", "facebook", "linkedin", "tiktok", "youtube", "x", "threads".</summary>
    [JsonPropertyName("platform")]
    public string Platform { get; set; } = "";

    /// <summary>Conversation kind: "dm", "comment", or "mention" (Threads has "comment" and "mention" only).</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("participant")]
    public InboxParticipant Participant { get; set; } = new();

    [JsonPropertyName("unread_count")]
    public int UnreadCount { get; set; }

    [JsonPropertyName("last_message")]
    public InboxLastMessage? LastMessage { get; set; }

    /// <summary>The related post for comment/mention conversations; null for DMs.</summary>
    [JsonPropertyName("post")]
    public InboxPostRef? Post { get; set; }
}

/// <summary>A single social inbox message.</summary>
public sealed class InboxMessage
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; } = "";

    /// <summary>Platform identifier, e.g. "instagram", "facebook", "linkedin", "tiktok", "youtube", "x", "threads".</summary>
    [JsonPropertyName("platform")]
    public string Platform { get; set; } = "";

    /// <summary>Message kind: "dm", "comment", or "mention" (Threads has "comment" and "mention" only).</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    /// <summary>"incoming" (from the sender) or "outgoing" (from you).</summary>
    [JsonPropertyName("direction")]
    public string Direction { get; set; } = "";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = "";

    [JsonPropertyName("is_read")]
    public bool IsRead { get; set; }

    [JsonPropertyName("is_replied")]
    public bool IsReplied { get; set; }

    /// <summary>Emoji reaction on the message, if any.</summary>
    [JsonPropertyName("reaction")]
    public string? Reaction { get; set; }

    /// <summary>Parent comment id when this is a threaded comment reply.</summary>
    [JsonPropertyName("parent_comment_id")]
    public string? ParentCommentId { get; set; }

    /// <summary>
    /// Comments and mentions only: true when the comment is hidden on the
    /// platform (see <see cref="InboxResource.HideAsync"/>), false when it is
    /// not. Null for DMs, which have no notion of hidden.
    /// </summary>
    [JsonPropertyName("hidden")]
    public bool? Hidden { get; set; }

    /// <summary>Link to the reply or mentioning post on the platform, when known.</summary>
    [JsonPropertyName("permalink")]
    public string? Permalink { get; set; }

    /// <summary>
    /// Media on this message, when present. Incoming Instagram/Facebook DM
    /// images, videos, voice messages, and story mentions are re-hosted on
    /// our CDN so the URL stays valid indefinitely. Null when the message
    /// has no media.
    /// </summary>
    [JsonPropertyName("attachment")]
    public InboxAttachment? Attachment { get; set; }

    [JsonPropertyName("sender")]
    public InboxParticipant Sender { get; set; } = new();

    /// <summary>The related post for comment/mention messages; null for DMs.</summary>
    [JsonPropertyName("post")]
    public InboxPostRef? Post { get; set; }
}

/// <summary>Media sent with an inbox message.</summary>
public sealed class InboxAttachment
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    /// <summary>"image", "video", "audio", or "file".</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
}

/// <summary>Envelope for <c>GET /inbox/conversations</c> (cursor-paginated).</summary>
public sealed class InboxConversationsResponse
{
    [JsonPropertyName("data")]
    public IList<InboxConversation> Data { get; set; } = new List<InboxConversation>();

    [JsonPropertyName("pagination")]
    public InboxCursorPagination Pagination { get; set; } = new();
}

/// <summary>Envelope for <c>GET /inbox/conversations/:id/messages</c> (cursor-paginated).</summary>
public sealed class InboxMessagesResponse
{
    [JsonPropertyName("data")]
    public IList<InboxMessage> Data { get; set; } = new List<InboxMessage>();

    [JsonPropertyName("pagination")]
    public InboxCursorPagination Pagination { get; set; } = new();
}

/// <summary>Response for <c>POST /inbox/conversations/:id/read</c>.</summary>
public sealed class InboxMarkReadResponse
{
    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; } = "";

    /// <summary>Number of messages that were newly marked read.</summary>
    [JsonPropertyName("marked_read")]
    public int MarkedRead { get; set; }
}

/// <summary>Single-item envelope for the message created by <c>POST /inbox/conversations/:id/reply</c>.</summary>
public sealed class InboxReplyResponse
{
    [JsonPropertyName("data")]
    public InboxMessage Data { get; set; } = new();

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Only when <see cref="InboxReplyParams.IncludeNext"/> was set: the next
    /// conversation that needs an answer (the same object
    /// <see cref="InboxResource.NextAsync"/> returns under <c>data</c>), or
    /// null when nothing is waiting.
    /// </summary>
    [JsonPropertyName("next")]
    public InboxNextUnanswered? Next { get; set; }

    /// <summary>
    /// Only when <see cref="InboxReplyParams.IncludeNext"/> was set: unanswered
    /// items still waiting after <see cref="Next"/> (capped at 500).
    /// </summary>
    [JsonPropertyName("remaining")]
    public int? Remaining { get; set; }
}

/// <summary>
/// Single-item envelope for <c>POST /inbox/messages/:id/hide</c>: the updated
/// message with <see cref="InboxMessage.Hidden"/> flipped.
/// </summary>
public sealed class InboxHideResponse
{
    [JsonPropertyName("data")]
    public InboxMessage Data { get; set; } = new();
}

/// <summary>The <c>data</c> of <c>DELETE /inbox/messages/:id</c>.</summary>
public sealed class InboxDeletedMessage
{
    /// <summary>The deleted message id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; } = "";

    /// <summary>Inbox ids of replies removed together with the comment.</summary>
    [JsonPropertyName("removed_reply_ids")]
    public IList<string> RemovedReplyIds { get; set; } = new List<string>();
}

/// <summary>Envelope for <c>DELETE /inbox/messages/:id</c>.</summary>
public sealed class InboxDeleteMessageResponse
{
    [JsonPropertyName("data")]
    public InboxDeletedMessage Data { get; set; } = new();
}

/// <summary>
/// The next conversation that needs an answer, with everything needed to draft
/// the reply. What <c>GET /inbox/next</c> returns under <c>data</c>, and what
/// a reply with <see cref="InboxReplyParams.IncludeNext"/> returns as
/// <c>next</c>.
/// </summary>
public sealed class InboxNextUnanswered
{
    [JsonPropertyName("conversation")]
    public InboxConversation Conversation { get; set; } = new();

    /// <summary>
    /// The unanswered incoming message itself: the customer's latest DM, or
    /// the specific comment. Its <c>id</c> is what
    /// <see cref="InboxResource.HideAsync"/> and
    /// <see cref="InboxResource.DeleteMessageAsync"/> take, and the
    /// <see cref="InboxReplyParams.MessageId"/> to set on
    /// <see cref="InboxResource.ReplyAsync"/> for comment threads (so the
    /// reply lands under this comment, not under the newest one on the
    /// post); its <c>conversation_id</c> is what
    /// <see cref="InboxResource.ReplyAsync"/> takes.
    /// </summary>
    [JsonPropertyName("message")]
    public InboxMessage Message { get; set; } = new();

    /// <summary>
    /// The conversation so far, oldest first (the most recent 50 messages for
    /// long DM threads).
    /// </summary>
    [JsonPropertyName("messages")]
    public IList<InboxMessage> Messages { get; set; } = new List<InboxMessage>();

    /// <summary>
    /// Whether <see cref="InboxResource.ReplyAsync"/> can still answer this
    /// item; see <see cref="InboxReplyWindow"/>.
    /// </summary>
    [JsonPropertyName("reply_window")]
    public InboxReplyWindow ReplyWindow { get; set; } = new();
}

/// <summary>
/// Whether a reply can still be sent through the API. Only Instagram and
/// Facebook DMs have a window (Meta: 24 hours after the customer's last
/// message); every other item has <see cref="Open"/> true and
/// <see cref="ClosesAt"/> null.
/// </summary>
public sealed class InboxReplyWindow
{
    /// <summary>
    /// False when the item is an Instagram/Facebook DM whose 24-hour window
    /// has closed. It is still served (the customer is still waiting), but
    /// <see cref="InboxResource.ReplyAsync"/> answers 422
    /// <c>outside_messaging_window</c>: answer it from the Instagram or
    /// Facebook app (that reply is mirrored into the inbox and clears the
    /// item) or mark the conversation read to skip it.
    /// </summary>
    [JsonPropertyName("open")]
    public bool Open { get; set; } = true;

    /// <summary>
    /// When the window closes or closed (the customer's last message + 24 h);
    /// null when there is no window.
    /// </summary>
    [JsonPropertyName("closes_at")]
    public string? ClosesAt { get; set; }
}

/// <summary>Envelope for <c>GET /inbox/next</c>.</summary>
public sealed class InboxNextResponse
{
    /// <summary>The next item, or null when nothing is waiting.</summary>
    [JsonPropertyName("data")]
    public InboxNextUnanswered? Data { get; set; }

    /// <summary>
    /// Unanswered items still waiting after this one (capped at 500). 0 when
    /// <see cref="Data"/> is null.
    /// </summary>
    [JsonPropertyName("remaining")]
    public int Remaining { get; set; }
}
