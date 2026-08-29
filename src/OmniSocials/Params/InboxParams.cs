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
    /// <summary>Reply text (required).</summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    /// <summary>Public URL of a single media asset to attach.</summary>
    [JsonPropertyName("attachment_url")]
    public string? AttachmentUrl { get; set; }

    /// <summary>Attachment kind: "image", "video", "audio", or "file". Pair with <see cref="AttachmentUrl"/>.</summary>
    [JsonPropertyName("attachment_type")]
    public string? AttachmentType { get; set; }
}
