using System.Text.Json.Serialization;

namespace OmniSocials;

/// <summary>Query parameters for <c>GET /inbox/conversations</c>.</summary>
public sealed class InboxConversationListParams
{
    /// <summary>
    /// Filter by platform: "instagram", "facebook", "linkedin", "tiktok",
    /// "youtube", "x", or "threads". The Threads inbox is currently rolling
    /// out; until Meta approves the permissions it is disabled on production.
    /// </summary>
    public string? Platform { get; set; }

    /// <summary>Filter by conversation kind: "dm", "comment", or "mention". Threads has "comment" and "mention" only (no DMs).</summary>
    public string? Type { get; set; }

    /// <summary>Only return conversations that have unread messages.</summary>
    public bool? Unread { get; set; }

    /// <summary>
    /// Only return conversations that still need an answer: the customer's
    /// latest DM has no reply after it (Instagram/Facebook DMs within the
    /// 24-hour messaging window only), or a comment/mention that has not been
    /// replied to and is not hidden. Replies typed in the native apps count
    /// as answers (they are mirrored into the inbox). Read state is ignored
    /// here; use <see cref="InboxResource.NextAsync"/> for a work queue.
    /// </summary>
    public bool? Unanswered { get; set; }

    /// <summary>Max items to return (1-100).</summary>
    public int? Limit { get; set; }

    /// <summary>Opaque cursor from a previous response's <c>pagination.next_cursor</c>.</summary>
    public string? Cursor { get; set; }
}

/// <summary>Query parameters for <c>GET /inbox/conversations/:id/messages</c>.</summary>
public sealed class InboxMessageListParams
{
    /// <summary>Max items to return.</summary>
    public int? Limit { get; set; }

    /// <summary>Opaque cursor from a previous response's <c>pagination.next_cursor</c>.</summary>
    public string? Cursor { get; set; }
}

/// <summary>
/// Body for <c>POST /inbox/conversations/:id/reply</c>. Null properties are omitted.
/// </summary>
public sealed class InboxReplyParams
{
    /// <summary>Reply text. Optional when <see cref="AttachmentUrl"/> is set — an attachment-only reply is allowed.</summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    /// <summary>Public URL of a single media asset to attach (Facebook and Instagram DMs only; other platforms are text-only).</summary>
    [JsonPropertyName("attachment_url")]
    public string? AttachmentUrl { get; set; }

    /// <summary>Attachment kind: "image", "video", "audio", or "file". Pair with <see cref="AttachmentUrl"/>.</summary>
    [JsonPropertyName("attachment_type")]
    public string? AttachmentType { get; set; }

    /// <summary>
    /// When true, the response also carries <c>next</c> (the next conversation
    /// that needs an answer, the same object <see cref="InboxResource.NextAsync"/>
    /// returns under <c>data</c>, using its default queue order and filters;
    /// null when nothing is waiting) and <c>remaining</c>. Saves the extra
    /// call when working through the inbox.
    /// </summary>
    [JsonPropertyName("include_next")]
    public bool? IncludeNext { get; set; }
}

/// <summary>Query parameters for <c>GET /inbox/next</c>. All optional.</summary>
public sealed class InboxNextParams
{
    /// <summary>Only items from one platform: "instagram", "facebook", "linkedin", "tiktok", "youtube", "x", or "threads".</summary>
    public string? Platform { get; set; }

    /// <summary>Only items of one type: "dm", "comment", or "mention".</summary>
    public string? Type { get; set; }

    /// <summary>"oldest" (the default: the item that has waited longest first) or "newest" (the most recent).</summary>
    public string? Order { get; set; }

    /// <summary>
    /// Also serve items that were marked read but never answered. By default
    /// only unread items are served, so marking a conversation read is the
    /// durable way to skip it.
    /// </summary>
    public bool? IncludeRead { get; set; }

    /// <summary>Conversation ids to leave out of this call (a session-local skip; up to 100). Sent comma-separated.</summary>
    public IList<string>? Exclude { get; set; }
}
