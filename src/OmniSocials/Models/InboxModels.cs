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

/// <summary>The post a comment/mention conversation is attached to (null for DMs).</summary>
public sealed class InboxPostRef
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("caption")]
    public string? Caption { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }
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
    /// Threads replies only: true when the reply is hidden on Threads (see
    /// <see cref="InboxResource.HideAsync"/>). Null for every other
    /// platform/message.
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
