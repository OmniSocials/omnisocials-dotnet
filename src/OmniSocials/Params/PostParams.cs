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

    /// <summary>
    /// Route the post through a saved approval workflow (id from
    /// <see cref="ApprovalWorkflowsResource.ListAsync"/>). The post is created as
    /// <c>in_approval</c> (<c>approval_status: "pending"</c>) instead of <c>scheduled</c>;
    /// its approvers are notified and it publishes at <see cref="ScheduledAt"/> once the
    /// last step approves (dashboard, or <c>Posts.ApproveAsync</c>). Requires
    /// <see cref="ScheduledAt"/>; not allowed with <c>publish_now</c>. Errors:
    /// <c>404 workflow_not_found</c>, <c>400 validation_error</c>.
    /// </summary>
    [JsonPropertyName("approval_workflow_id")]
    public string? ApprovalWorkflowId { get; set; }

    /// <summary>
    /// Planned draft. With <see cref="ScheduledAt"/>, the post is stored as <c>draft</c> and
    /// keeps that time: it shows on the dashboard calendar at that slot with a dashed
    /// outline, the scheduler never publishes it, and a person schedules it from the
    /// composer (or you do with <c>Posts.UpdateAsync</c> and <see cref="PostUpdateParams.Draft"/>
    /// = false). Use it when an agent proposes a slot that a human should confirm. Not allowed
    /// with <c>publish_now</c> or <see cref="ApprovalWorkflowId"/> (<c>400 validation_error</c>).
    /// Without <see cref="ScheduledAt"/> it changes nothing: that post is a draft already.
    /// </summary>
    [JsonPropertyName("draft")]
    public bool? Draft { get; set; }

    /// <summary>
    /// Image fit per channel for feed images whose ratio is outside what a channel shows
    /// uncropped, keyed by platform id (<c>instagram</c>, <c>pinterest</c>, ...), each
    /// <c>auto</c>, <c>crop</c> or <c>keep</c>. <c>auto</c> (default): Instagram crops a tall
    /// or wide image to 4:5 / 1.91:1 around the subject (Instagram cuts it to that window
    /// anyway and Meta cannot boost a feed post outside it); Pinterest keeps a Pin taller
    /// than 2:3 and reports it. <c>crop</c> / <c>keep</c> force it per channel. Decisions
    /// come back in the response's <c>warnings</c> with code <c>image_fit</c>.
    /// </summary>
    [JsonPropertyName("media_fit")]
    public Dictionary<string, string>? MediaFit { get; set; }

    /// <summary>
    /// Comment-to-DM automation bound to this post (Instagram or Facebook must be in
    /// <see cref="Channels"/>): a keyword in a comment is answered with a DM, once
    /// per person, with an optional public reply under the comment.
    /// <c>{ ["trigger"] = new { keywords = new[] { "guide" }, match = "word" }, ["actions"] = new { dm = new { text = "..." }, public_reply = new { enabled = true, variations = new[] { "..." } } }, ["limits"] = new { max_sends = 500 }, ["platforms"] = new[] { "instagram", "facebook" } }</c>
    /// (see <see cref="AutomationCreateParams"/> for the fields). <c>platforms</c> makes
    /// one automation per listed channel; each must be a channel of the post. Without
    /// it: Instagram when the post goes there, else Facebook. Needs the
    /// <c>automations:write</c> scope on the key (<c>403 insufficient_scope</c>, checked
    /// before anything else); a bad object is a <c>400 validation_error</c> whose message
    /// starts with <c>comment_dm_automation: </c>. The response then carries
    /// <c>data.comment_dm_automation</c> (the first automation, with <c>post</c> null,
    /// plus <c>platforms</c> and <c>automations</c>, one <c>id</c> / <c>platform</c> /
    /// <c>status</c> per channel); each starts listening once the post is live on its
    /// channel.
    /// </summary>
    [JsonPropertyName("comment_dm_automation")]
    public Dictionary<string, object?>? CommentDmAutomation { get; set; }

    // Platform-specific option blocks. Dictionary values serialize as-is,
    // including explicit nulls (unlike null POCO properties, which are omitted).

    /// <summary>
    /// Pinterest options (<c>board_id</c>, <c>title</c>, <c>link</c>,
    /// <c>alt_text</c>, ...). <c>product_tags</c> tags products on the Pin: up
    /// to 24 product Pins of the connected Pinterest account, each as a Pin id
    /// string (see <see cref="PinterestResource.ListProductsAsync"/>) or a Pin
    /// link. Products of other merchants cannot be tagged. The tags are added
    /// right after the Pin is published; a product Pinterest refuses never
    /// fails the post, and the post's <c>pinterest</c> block then carries
    /// <c>product_tags_result</c> (<c>requested</c>, <c>tagged</c>,
    /// <c>skipped</c>, <c>error</c>). More than 24 entries or an invalid entry
    /// returns <c>400 validation_error</c>.
    /// </summary>
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
    /// <c>made_with_ai</c>, <c>poll</c> (<c>{ options: 2-4 strings of up to
    /// 25 chars, duration_minutes: 5-10080 }</c>; no media together with a
    /// poll), and <c>thread_parts</c> (2-25 parts, each
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
    /// calls return a clear error. <c>topic_tag</c> sets the topic of the
    /// post, for example "Build in public" (1 to 50 characters, no period and
    /// no ampersand; on a multi-part thread it goes on part 1; <c>null</c> on
    /// update removes it).
    /// </summary>
    [JsonPropertyName("threads")]
    public Dictionary<string, object?>? Threads { get; set; }

    [JsonPropertyName("google_business")]
    public Dictionary<string, object?>? GoogleBusiness { get; set; }

    /// <summary>
    /// Telegram options (the connected channel or group, posted by the
    /// OmniSocials bot): <c>silent</c>, <c>link_preview</c>, <c>pin</c>,
    /// <c>protect_content</c>, <c>spoiler</c>, <c>caption_above_media</c>
    /// (booleans), up to 3 <c>buttons</c> (<c>{ text, url }</c>, not on an
    /// album or a poll), a native <c>poll</c> (<c>{ options, open_period?,
    /// multiple? }</c>: 2 to 12 options of up to 100 chars, 5 to 2628000
    /// seconds; the post text is the question, max 300 chars, no media) and
    /// <c>first_comment</c> (a reply under the post). Text up to 4,096 chars;
    /// with media, text over 1,024 chars is sent as a reply under the media.
    /// No Stories.
    /// </summary>
    [JsonPropertyName("telegram")]
    public Dictionary<string, object?>? Telegram { get; set; }

    /// <summary>
    /// Discord options (one Discord channel, posted through its incoming
    /// webhook with the webhook's name and picture): <c>silent</c>,
    /// <c>link_preview</c> (default on), <c>allow_mentions</c> (default off:
    /// @everyone, @here, role and user mentions show as text and ping nobody),
    /// <c>spoiler</c> (booleans), <c>thread_name</c> (the post title in a forum
    /// or media channel, 1 to 100 chars; on a text channel the post goes out
    /// without it), up to 5
    /// <c>buttons</c> (<c>{ text, url }</c>: 1 to 80 chars, https:// or
    /// http://, one row, not with a poll) and a native <c>poll</c>
    /// (<c>{ options, duration_hours?, multiple? }</c>: 2 to 10 options of up
    /// to 55 chars, 1 to 768 hours, default 24; the post text is the question,
    /// max 300 chars, no media and no buttons). Text up to 2,000 chars; up to
    /// 10 images, GIFs and videos per message, a video or GIF over 20 MB is
    /// sent as a link in the text. No Stories.
    /// </summary>
    [JsonPropertyName("discord")]
    public Dictionary<string, object?>? Discord { get; set; }

    /// <summary>
    /// Non-sponsored LinkedIn poll: <c>question</c> (max 140 chars), <c>options</c>
    /// (2-4 entries, each max 30 chars), and <c>duration</c> ("ONE_DAY",
    /// "THREE_DAYS", "SEVEN_DAYS", or "FOURTEEN_DAYS"). Mutually exclusive with
    /// media and a link share on the same post.
    /// </summary>
    [JsonPropertyName("linkedin_poll")]
    public Dictionary<string, object?>? LinkedinPoll { get; set; }

    /// <summary>
    /// Video thumbnail for a post whose media is one video:
    /// <c>{ ["type"] = "frame", ["thumb_offset"] = 3000 }</c> or
    /// <c>{ ["type"] = "custom", ["cover_url"] = "https://..." }</c>, plus an
    /// optional <c>overrides</c> dictionary keyed by platform id. Applied on
    /// Instagram, Facebook, LinkedIn, TikTok (frame only), Pinterest and
    /// YouTube Shorts (shown on Shorts only on channels where YouTube has
    /// enabled custom Shorts thumbnails).
    /// </summary>
    [JsonPropertyName("video_cover")]
    public Dictionary<string, object?>? VideoCover { get; set; }
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

    /// <summary>
    /// Planned-draft switch. <c>false</c> without <see cref="ScheduledAt"/> schedules a draft at
    /// the time it already carries (<c>400 validation_error</c> when it has none or it is in the
    /// past). <c>true</c> without <see cref="ScheduledAt"/> unschedules a scheduled post: it keeps
    /// its time and stays on the calendar as a dashed draft, nothing publishes; a post waiting for
    /// approval leaves its workflow. <c>true</c> with <see cref="ScheduledAt"/> moves the time and
    /// keeps (or makes) the post a draft.
    /// </summary>
    [JsonPropertyName("draft")]
    public bool? Draft { get; set; }

    /// <summary>
    /// Image fit per channel (same shape as <see cref="PostCreateParams.MediaFit"/>). On
    /// update, <c>media_fit</c> alone re-decides the stored images (<c>keep</c> gives the
    /// original back). Decisions come back in the response's <c>warnings</c> with code
    /// <c>image_fit</c>.
    /// </summary>
    [JsonPropertyName("media_fit")]
    public Dictionary<string, string>? MediaFit { get; set; }

    /// <summary>
    /// Replaces the post's comment-to-DM automations (same shape as
    /// <see cref="PostCreateParams.CommentDmAutomation"/>; a channel no longer in
    /// <c>platforms</c> loses its automation). Omit to leave them untouched; to remove
    /// them all set <see cref="RemoveCommentDmAutomation"/>. Needs the
    /// <c>automations:write</c> scope.
    /// </summary>
    [JsonPropertyName("comment_dm_automation")]
    public Dictionary<string, object?>? CommentDmAutomation { get; set; }

    /// <summary>
    /// When true, sends an explicit <c>"comment_dm_automation": null</c> to remove the
    /// post's comment-to-DM automation. Takes precedence over
    /// <see cref="CommentDmAutomation"/>.
    /// </summary>
    [JsonIgnore]
    public bool RemoveCommentDmAutomation { get; set; }

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

    /// <summary>
    /// Replaces the stored Pinterest options wholesale, so leave
    /// <c>product_tags</c> out (or send an empty list) to remove the product
    /// tags.
    /// </summary>
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
    /// <c>["thread_parts"] = null</c> clears thread mode,
    /// <c>["location_id"] = null</c> (or <c>["location"] = null</c>) clears the
    /// Threads location tag and <c>["topic_tag"] = null</c> removes the topic;
    /// omit a key to leave it untouched.
    /// </summary>
    [JsonPropertyName("threads")]
    public Dictionary<string, object?>? Threads { get; set; }

    [JsonPropertyName("google_business")]
    public Dictionary<string, object?>? GoogleBusiness { get; set; }

    /// <summary>
    /// Telegram options (see <see cref="PostCreateParams.Telegram"/> for the
    /// keys). Every key merges into the stored options; a key set to
    /// <c>null</c> removes it (for example <c>["poll"] = null</c>).
    /// </summary>
    [JsonPropertyName("telegram")]
    public Dictionary<string, object?>? Telegram { get; set; }

    /// <summary>
    /// Discord options (see <see cref="PostCreateParams.Discord"/> for the
    /// keys). Every key merges into the stored options; a key set to
    /// <c>null</c> removes it (for example <c>["poll"] = null</c>). To remove
    /// every Discord option set <see cref="RemoveDiscord"/>.
    /// </summary>
    [JsonPropertyName("discord")]
    public Dictionary<string, object?>? Discord { get; set; }

    /// <summary>
    /// When true, sends an explicit <c>"discord": null</c> to remove every
    /// Discord option of the post (the publish result stays). Takes
    /// precedence over <see cref="Discord"/>.
    /// </summary>
    [JsonIgnore]
    public bool RemoveDiscord { get; set; }

    /// <summary>
    /// Non-sponsored LinkedIn poll (see <see cref="PostCreateParams.LinkedinPoll"/>
    /// for the shape). Pass an explicit <c>null</c> to clear the poll and revert
    /// the post to normal; omit to leave it untouched.
    /// </summary>
    [JsonPropertyName("linkedin_poll")]
    public Dictionary<string, object?>? LinkedinPoll { get; set; }

    /// <summary>
    /// Replaces the stored video cover wholesale (see
    /// <see cref="PostCreateParams.VideoCover"/> for the shape). Omit to leave
    /// it untouched.
    /// </summary>
    [JsonPropertyName("video_cover")]
    public Dictionary<string, object?>? VideoCover { get; set; }
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
