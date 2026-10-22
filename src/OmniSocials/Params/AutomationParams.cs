using System.Text.Json.Serialization;

namespace OmniSocials;

/// <summary>Query parameters for <c>GET /automations</c>. Every property is optional.</summary>
public sealed class AutomationListParams
{
    /// <summary>"active", "paused" or "completed".</summary>
    public string? Status { get; set; }

    /// <summary>"instagram" or "facebook": only that channel's automations.</summary>
    public string? Platform { get; set; }

    /// <summary>Only the automations bound to this OmniSocials post.</summary>
    public string? PostId { get; set; }
}

/// <summary>Query parameters for <c>GET /automations/:id/events</c>. Every property is optional.</summary>
public sealed class AutomationEventListParams
{
    /// <summary>"sent", "skipped", "failed" or "pending".</summary>
    public string? Outcome { get; set; }

    /// <summary>Events per page, 1 to 100 (default 25).</summary>
    public int? Limit { get; set; }

    /// <summary>Events to skip.</summary>
    public int? Offset { get; set; }
}

/// <summary>Body for <c>POST /automations</c>. Null properties are omitted.</summary>
public class AutomationCreateParams
{
    /// <summary>
    /// "instagram" (default) or "facebook" (a Facebook Page; the DM lands in
    /// Messenger). <see cref="FacebookPostId"/> implies "facebook". Facebook has
    /// no story replies: "story_reply" in the trigger kinds is a
    /// <c>400 validation_error</c>.
    /// </summary>
    [JsonPropertyName("platform")]
    public string? Platform { get; set; }

    /// <summary>Name, max 120 characters.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// For scope "post": an OmniSocials post that goes to the automation's
    /// channel (scheduled or live). One automation per post and platform: a
    /// post that goes to Instagram and Facebook can have one of each. Set at
    /// most one of <see cref="PostId"/>, <see cref="InstagramMediaId"/> and
    /// <see cref="FacebookPostId"/>.
    /// </summary>
    [JsonPropertyName("post_id")]
    public string? PostId { get; set; }

    /// <summary>For scope "post": an Instagram post published outside OmniSocials (numeric Instagram media id).</summary>
    [JsonPropertyName("instagram_media_id")]
    public string? InstagramMediaId { get; set; }

    /// <summary>
    /// For scope "post": a post published on the Facebook Page outside
    /// OmniSocials, as "{page-id}_{post-id}" or the post id alone (the Page id
    /// is put in front). Implies <see cref="Platform"/> "facebook". A post of
    /// another Page is a <c>404 facebook_post_not_found</c>.
    /// </summary>
    [JsonPropertyName("facebook_post_id")]
    public string? FacebookPostId { get; set; }

    /// <summary>
    /// <c>{ ["kinds"] = new[] { "comment" }, ["scope"] = "post", ["keywords"] = new[] { "guide" }, ["match"] = "word" }</c>:
    /// <c>kinds</c> one or more of "comment", "dm", "story_reply" (default
    /// ["comment"]; a Facebook Page has "comment" and "dm" only);
    /// <c>scope</c> "post" (default), "next_post" (the next post the
    /// workspace publishes on the automation's channel) or "any" (every post);
    /// <c>keywords</c> up to 20 of 60 characters, required unless
    /// <c>match</c> is "any"; <c>match</c> "word" (default), "contains" or
    /// "any".
    /// </summary>
    [JsonPropertyName("trigger")]
    public Dictionary<string, object?>? Trigger { get; set; }

    /// <summary>
    /// Required. <c>{ ["dm"] = new { text = "..." } }</c> (max 1000 characters)
    /// plus an optional <c>["public_reply"] = new { enabled = true, variations = new[] { "..." } }</c>
    /// (comment kind only, up to 10 variations of 300 characters).
    /// </summary>
    [JsonPropertyName("actions")]
    public Dictionary<string, object?>? Actions { get; set; }

    /// <summary><c>{ ["max_sends"] = 500, ["stop_at"] = "2026-12-31T00:00:00Z" }</c>, both optional.</summary>
    [JsonPropertyName("limits")]
    public Dictionary<string, object?>? Limits { get; set; }

    /// <summary>"active" (default) or "paused" (create as paused).</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

/// <summary>
/// Body for <c>PATCH /automations/:id</c>. Null properties are omitted; a body
/// with nothing is a <c>400 no_fields</c>.
/// </summary>
public class AutomationUpdateParams
{
    /// <summary>
    /// The platform cannot change: another platform (or
    /// <see cref="InstagramMediaId"/> on a Facebook automation,
    /// <see cref="FacebookPostId"/> on an Instagram one) is a
    /// <c>400 platform_immutable</c>.
    /// </summary>
    [JsonPropertyName("platform")]
    public string? Platform { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Moves the automation to that OmniSocials post.</summary>
    [JsonPropertyName("post_id")]
    public string? PostId { get; set; }

    /// <summary>Moves an Instagram automation to that Instagram post.</summary>
    [JsonPropertyName("instagram_media_id")]
    public string? InstagramMediaId { get; set; }

    /// <summary>Moves a Facebook automation to that Facebook Page post.</summary>
    [JsonPropertyName("facebook_post_id")]
    public string? FacebookPostId { get; set; }

    /// <summary>
    /// Merged into the stored trigger (see <see cref="AutomationCreateParams.Trigger"/>):
    /// keys you omit keep their value, so new <c>keywords</c> keep the kinds, the
    /// scope and the match.
    /// </summary>
    [JsonPropertyName("trigger")]
    public Dictionary<string, object?>? Trigger { get; set; }

    /// <summary>
    /// Merged into the stored actions (see <see cref="AutomationCreateParams.Actions"/>):
    /// a new <c>dm.text</c> keeps the public reply; <c>public_reply</c> with
    /// <c>enabled = false</c> or <c>variations = []</c> turns it off.
    /// </summary>
    [JsonPropertyName("actions")]
    public Dictionary<string, object?>? Actions { get; set; }

    /// <summary>
    /// Merged into the stored limits (see <see cref="AutomationCreateParams.Limits"/>):
    /// <c>{ ["stop_at"] = "..." }</c> keeps <c>max_sends</c>;
    /// <c>{ ["max_sends"] = null }</c> removes that limit (dictionary nulls are sent).
    /// </summary>
    [JsonPropertyName("limits")]
    public Dictionary<string, object?>? Limits { get; set; }

    /// <summary>"paused" pauses, "active" resumes.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
