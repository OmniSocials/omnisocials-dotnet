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
    /// chars), delivered to Mastodon, Bluesky, X (photos/GIFs), Pinterest,
    /// Instagram (images), and LinkedIn (images).
    /// </summary>
    [JsonPropertyName("media_ids")]
    public object? MediaIds { get; set; }

    /// <summary>
    /// Public media URLs: either a <c>string[]</c> shared across platforms or a
    /// per-platform <c>Dictionary&lt;string, string[]&gt;</c>. Entries may also
    /// be <c>{ url, alt }</c> objects (e.g. <c>new { url = "https://...", alt = "..." }</c>)
    /// carrying a per-media alt text / accessibility description (max 1500
    /// chars), delivered to Mastodon, Bluesky, X (photos/GIFs), Pinterest,
    /// Instagram (images), and LinkedIn (images).
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
    /// <c>{ text, media_ids?, media_urls? }</c>, 280 chars per part —
    /// 25,000 for Premium/Premium+ accounts).
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

    /// <summary>
    /// Threads (Meta) options: <c>thread_parts</c> (2-25 parts, 500 chars per
    /// part, up to 10 media per part; parts after the first publish as replies
    /// to the previous part, and the Threads caption is taken from part 1) and
    /// <c>location_id</c> (a Threads location id from
    /// <c>Locations.SearchAsync</c> with platform "threads"; on a multi-part
    /// thread the tag is applied to part 1). Alternatively pass
    /// <c>location</c>: an object <c>{ id, name?, address?, city?, country? }</c>
    /// to store display fields along with the id (<c>location_id</c> wins when
    /// both are given). Threads location tagging is currently rolling out;
    /// until Meta approves the permissions it is disabled on production and
    /// calls return a clear error.
    /// </summary>
    [JsonPropertyName("threads")]
    public Dictionary<string, object?>? Threads { get; set; }

    [JsonPropertyName("google_business")]
    public Dictionary<string, object?>? GoogleBusiness { get; set; }

    /// <summary>
    /// Non-sponsored LinkedIn poll: <c>question</c> (max 140 chars), <c>options</c>
    /// (2-4 entries, each max 30 chars), and <c>duration</c> ("ONE_DAY",
    /// "THREE_DAYS", "SEVEN_DAYS", or "FOURTEEN_DAYS"). Mutually exclusive with
    /// media and a link share on the same post.
    /// </summary>
    [JsonPropertyName("linkedin_poll")]
    public Dictionary<string, object?>? LinkedinPoll { get; set; }
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

    /// <summary>
    /// <c>["thread_parts"] = null</c> clears thread mode and
    /// <c>["location_id"] = null</c> (or <c>["location"] = null</c>) clears the
    /// Threads location tag; omit a key to leave it untouched.
    /// </summary>
    [JsonPropertyName("threads")]
    public Dictionary<string, object?>? Threads { get; set; }

    [JsonPropertyName("google_business")]
    public Dictionary<string, object?>? GoogleBusiness { get; set; }

    /// <summary>
    /// Non-sponsored LinkedIn poll (see <see cref="PostCreateParams.LinkedinPoll"/>
    /// for the shape). Pass an explicit <c>null</c> to clear the poll and revert
    /// the post to normal; omit to leave it untouched.
    /// </summary>
    [JsonPropertyName("linkedin_poll")]
    public Dictionary<string, object?>? LinkedinPoll { get; set; }
}

/// <summary>Query parameters for <c>GET /posts</c>.</summary>
public sealed class PostListParams
{
    /// <summary>Filter by status: "draft", "in_approval", "scheduled", "posting", "published", "failed", "warning". "in_approval" = waiting for a reviewer in an approval workflow.</summary>
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
