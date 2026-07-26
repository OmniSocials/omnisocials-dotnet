using System.Text.Json.Serialization;

namespace OmniSocials;

/// <summary>An Instagram user tag pinned onto an image (coordinates are 0..1).</summary>
public sealed class UserTag
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    /// <summary>Which carousel image the tag belongs to (0-based).</summary>
    [JsonPropertyName("image_index")]
    public int? ImageIndex { get; set; }
}

/// <summary>
/// Body for <c>POST /posts/create</c> and <c>POST /posts/create-and-publish</c>.
/// Null properties are omitted from the request.
/// </summary>
public class PostCreateParams
{
    /// <summary>
    /// Caption. Either a single <see cref="string"/> or a per-platform
    /// <c>Dictionary&lt;string, string&gt;</c> with a <c>"default"</c> key.
    /// </summary>
    [JsonPropertyName("content")]
    public object? Content { get; set; }

    /// <summary>Platform identifiers, e.g. ["instagram", "linkedin", "x"].</summary>
    [JsonPropertyName("channels")]
    public IList<string>? Channels { get; set; }

    /// <summary>ISO 8601 datetime. Omit to create a draft.</summary>
    [JsonPropertyName("scheduled_at")]
    public string? ScheduledAt { get; set; }

    /// <summary>
    /// Media Library ids: either a <c>string[]</c> shared across platforms or a
    /// per-platform <c>Dictionary&lt;string, string[]&gt;</c>. Entries may also
    /// be <c>{ id, alt }</c> objects (e.g. <c>new { id = "...", alt = "..." }</c>)
    /// carrying a per-media alt text / accessibility description (max 1500
    /// chars), delivered to Mastodon, Bluesky, X (photos/GIFs), and Pinterest.
    /// </summary>
    [JsonPropertyName("media_ids")]
    public object? MediaIds { get; set; }

    /// <summary>
    /// Public media URLs: either a <c>string[]</c> shared across platforms or a
    /// per-platform <c>Dictionary&lt;string, string[]&gt;</c>. Entries may also
    /// be <c>{ url, alt }</c> objects (e.g. <c>new { url = "https://...", alt = "..." }</c>)
    /// carrying a per-media alt text / accessibility description (max 1500
    /// chars), delivered to Mastodon, Bluesky, X (photos/GIFs), and Pinterest.
    /// </summary>
    [JsonPropertyName("media_urls")]
    public object? MediaUrls { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("link_url")]
    public string? LinkUrl { get; set; }

    [JsonPropertyName("link_title")]
    public string? LinkTitle { get; set; }

    [JsonPropertyName("link_description")]
    public string? LinkDescription { get; set; }

    [JsonPropertyName("link_thumbnail_url")]
    public string? LinkThumbnailUrl { get; set; }

    /// <summary>Instagram location tag (a Facebook Place id, see Locations.SearchAsync).</summary>
    [JsonPropertyName("location_id")]
    public string? LocationId { get; set; }

    [JsonPropertyName("collaborators")]
    public IList<string>? Collaborators { get; set; }

    [JsonPropertyName("user_tags")]
    public IList<UserTag>? UserTags { get; set; }

    /// <summary>
    /// Name of a saved hashtag set (case-insensitive). Applies the set once at
    /// create time; tags already in a caption are skipped; Instagram's
    /// 30-hashtag cap returns error code <c>hashtag_limit_exceeded</c>.
    /// </summary>
    [JsonPropertyName("hashtag_set")]
    public string? HashtagSet { get; set; }

    /// <summary>Id of a saved hashtag set to apply at create time (see <see cref="HashtagSet"/>).</summary>
    [JsonPropertyName("hashtag_set_id")]
    public string? HashtagSetId { get; set; }

    /// <summary>Where the hashtags go: "caption_append" (default) or "first_comment".</summary>
    [JsonPropertyName("hashtag_placement")]
    public string? HashtagPlacement { get; set; }

    /// <summary>Restrict the hashtags to a subset of the post's channels. Omit for all.</summary>
    [JsonPropertyName("hashtag_platforms")]
    public IList<string>? HashtagPlatforms { get; set; }

    // Platform-specific option blocks. Dictionary values serialize as-is,
    // including explicit nulls (unlike null POCO properties, which are omitted).

    [JsonPropertyName("pinterest")]
    public Dictionary<string, object?>? Pinterest { get; set; }

    [JsonPropertyName("youtube")]
    public Dictionary<string, object?>? Youtube { get; set; }

    [JsonPropertyName("instagram")]
    public Dictionary<string, object?>? Instagram { get; set; }

    [JsonPropertyName("facebook")]
    public Dictionary<string, object?>? Facebook { get; set; }

    [JsonPropertyName("linkedin")]
    public Dictionary<string, object?>? Linkedin { get; set; }

    [JsonPropertyName("linkedin_page")]
    public Dictionary<string, object?>? LinkedinPage { get; set; }

    [JsonPropertyName("tiktok")]
    public Dictionary<string, object?>? Tiktok { get; set; }

    /// <summary>
    /// X (Twitter) options: <c>reply_settings</c>, <c>paid_partnership</c>,
    /// <c>made_with_ai</c>, and <c>thread_parts</c> (2-25 parts, each
    /// <c>{ text, media_ids?, media_urls? }</c>, 280 chars per part).
    /// Thread-part media entries accept the same <c>{ url|id, alt }</c>
    /// objects as the top-level media fields.
    /// </summary>
    [JsonPropertyName("x")]
    public Dictionary<string, object?>? X { get; set; }

    /// <summary>Bluesky options: <c>thread_parts</c> (2-25 parts, 300 chars per part).</summary>
    [JsonPropertyName("bluesky")]
    public Dictionary<string, object?>? Bluesky { get; set; }

    /// <summary>Mastodon options: <c>thread_parts</c> (2-25 parts, 500 chars per part).</summary>
    [JsonPropertyName("mastodon")]
    public Dictionary<string, object?>? Mastodon { get; set; }

    [JsonPropertyName("google_business")]
    public Dictionary<string, object?>? GoogleBusiness { get; set; }
}

/// <summary>
/// Body for <c>PATCH /posts/:id</c>. Null properties are omitted. To clear a
/// thread on update, set the platform dictionary entry to an explicit null:
/// <c>X = new Dictionary&lt;string, object?&gt; { ["thread_parts"] = null }</c>
/// (dictionary nulls are serialized; omitting the key leaves the thread untouched).
/// </summary>
public class PostUpdateParams
{
    /// <summary>Caption: a string or a per-platform map with a "default" key.</summary>
    [JsonPropertyName("content")]
    public object? Content { get; set; }

    [JsonPropertyName("scheduled_at")]
    public string? ScheduledAt { get; set; }

    [JsonPropertyName("channels")]
    public IList<string>? Channels { get; set; }

    /// <summary>Media Library ids: a string[] or a per-platform map. Entries accept <c>{ id, alt }</c> objects for per-media alt text.</summary>
    [JsonPropertyName("media_ids")]
    public object? MediaIds { get; set; }

    /// <summary>Public media URLs: a string[] or a per-platform map. Entries accept <c>{ url, alt }</c> objects for per-media alt text.</summary>
    [JsonPropertyName("media_urls")]
    public object? MediaUrls { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("location_id")]
    public string? LocationId { get; set; }

    [JsonPropertyName("collaborators")]
    public IList<string>? Collaborators { get; set; }

    [JsonPropertyName("user_tags")]
    public IList<UserTag>? UserTags { get; set; }

    [JsonPropertyName("pinterest")]
    public Dictionary<string, object?>? Pinterest { get; set; }

    [JsonPropertyName("youtube")]
    public Dictionary<string, object?>? Youtube { get; set; }

    [JsonPropertyName("instagram")]
    public Dictionary<string, object?>? Instagram { get; set; }

    [JsonPropertyName("facebook")]
    public Dictionary<string, object?>? Facebook { get; set; }

    [JsonPropertyName("linkedin")]
    public Dictionary<string, object?>? Linkedin { get; set; }

    [JsonPropertyName("linkedin_page")]
    public Dictionary<string, object?>? LinkedinPage { get; set; }

    [JsonPropertyName("tiktok")]
    public Dictionary<string, object?>? Tiktok { get; set; }

    /// <summary><c>["thread_parts"] = null</c> clears thread mode; omit the key to leave it untouched.</summary>
    [JsonPropertyName("x")]
    public Dictionary<string, object?>? X { get; set; }

    /// <summary><c>["thread_parts"] = null</c> clears thread mode; omit the key to leave it untouched.</summary>
    [JsonPropertyName("bluesky")]
    public Dictionary<string, object?>? Bluesky { get; set; }

    /// <summary><c>["thread_parts"] = null</c> clears thread mode; omit the key to leave it untouched.</summary>
    [JsonPropertyName("mastodon")]
    public Dictionary<string, object?>? Mastodon { get; set; }

    [JsonPropertyName("google_business")]
    public Dictionary<string, object?>? GoogleBusiness { get; set; }
}

/// <summary>Query parameters for <c>GET /posts</c>.</summary>
public sealed class PostListParams
{
    /// <summary>Filter by status, e.g. "draft", "scheduled", "published", "failed".</summary>
    public string? Status { get; set; }

    /// <summary>Max items to return (default 20, max 100).</summary>
    public int? Limit { get; set; }

    /// <summary>Items to skip (default 0).</summary>
    public int? Offset { get; set; }
}

/// <summary>Query parameters for <c>GET /posts/recent-platform</c>.</summary>
public sealed class RecentPlatformParams
{
    /// <summary>Max posts per platform.</summary>
    public int? Limit { get; set; }

    /// <summary>Platforms to fetch, e.g. ["instagram", "x"]. Omit for all connected.</summary>
    public IList<string>? Platforms { get; set; }
}
