using System.Text.Json.Serialization;

namespace OmniSocials;

/// <summary>Query parameters for <c>GET /automations</c>. Every property is optional.</summary>
public sealed class AutomationListParams
{
    /// <summary>"active", "paused" or "completed".</summary>
    public string? Status { get; set; }

    /// <summary>"instagram", "facebook" or "x": only that channel's automations.</summary>
    public string? Platform { get; set; }

    /// <summary>Only the automations bound to this OmniSocials post.</summary>
    public string? PostId { get; set; }
}

/// <summary>Query parameters for <c>GET /automations/:id/events</c>. Every property is optional.</summary>
public sealed class AutomationEventListParams
{
    /// <summary>
    /// "sent", "skipped", "failed", "pending", "queued" (the answer waits for a
    /// free slot under the send cap and goes out as soon as there is room),
    /// "opted_out" (the person replied STOP on X) or "waiting" (Follow first:
    /// asked to follow, the link goes out after the follow; or the tap step of
    /// a follow-up: the link goes out after the tap).
    /// </summary>
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
    /// "instagram" (default), "facebook" (a Facebook Page; the DM lands in
    /// Messenger) or "x" (the X account: trigger kinds ["dm"] only, no post
    /// fields). <see cref="FacebookPostId"/> implies "facebook". Facebook has
    /// no story replies: "story_reply" in the trigger kinds is a
    /// <c>400 validation_error</c>. An active X automation needs X DMs on for
    /// the workspace (<c>409 x_dms_off</c>; <see cref="Status"/> "paused" always
    /// saves), and every X answer ends with "Reply STOP to stop automatic
    /// messages.".
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
    /// ["comment"]; a Facebook Page has "comment" and "dm" only, X has "dm"
    /// only);
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
    /// (comment kind only, up to 10 variations of 300 characters) and an
    /// optional <c>["follow_gate"] = new { enabled = true, first_message = new { text = "...", button = "..." }, not_following = new { text = "...", button = "..." } }</c>:
    /// Follow first, Instagram only. The link (the <c>dm</c> text) then goes
    /// out only to people who follow the account: a comment gets the first
    /// message as the private reply, with one button, and a tap checks the
    /// follow; anyone who does not follow yet gets the <c>not_following</c>
    /// message with its button, and every later tap checks again. A DM or a
    /// story reply is checked at once. When Instagram gives no answer the
    /// link goes out anyway. Texts hold 640 characters, buttons 20; a text or
    /// button left out is the default. <c>enabled = true</c> on a Facebook or
    /// X automation is a <c>400 follow_gate_instagram_only</c>. The response
    /// carries the gate with its texts (null on Facebook and X) and
    /// <c>stats.followed</c>.
    /// <para>
    /// An optional <c>["follow_up"] = new { enabled = true, text = "...", delay_hours = 23, button = new { title = "...", url = "https://..." }, tap = new { text = "...", button = "..." } }</c>:
    /// the follow-up, Instagram and Facebook. It sends one more DM
    /// <c>delay_hours</c> (1 to 23, default 23) after the answer (the
    /// <c>dm</c> text) went out. <c>text</c> is required when it is on (max
    /// 1000 characters, 640 with a <c>button</c>); <c>button</c> is an optional
    /// link button (<c>title</c> 1 to 20 characters, <c>url</c> an https:// or
    /// http:// address of up to 2000). Meta allows an automatic message only
    /// within 24 hours after the person's last DM, button tap or quick reply;
    /// the comment and the private reply do not open that window. So on an
    /// Instagram comment with Follow first off the first DM becomes a tap
    /// step, a short text with one button (<c>tap</c>, Instagram only,
    /// <c>text</c> max 640 and <c>button</c> max 20 characters; default "Tap
    /// the button and I'll send you the link 👇" / "Send me the link"): the
    /// tap sends the link, and until then the event is "waiting" with
    /// <c>follow_gate.mode</c> "tap". People who never tap get no link and no
    /// follow-up. With Follow first on, its own tap is the tap; DM and story
    /// reply triggers need none. A Facebook comment answer has no button: only
    /// people who answer the DM in Messenger get the follow-up. <c>null</c>,
    /// <c>false</c> or <c>enabled = false</c> turns it off and keeps the texts.
    /// <c>enabled = true</c> on an X automation is a
    /// <c>400 follow_up_not_supported</c>. The response carries
    /// <c>actions.follow_up</c> (<c>enabled</c>, <c>text</c>,
    /// <c>delay_hours</c>, <c>button</c> or null, <c>tap</c> filled in on
    /// Instagram and null on Facebook; the whole follow-up is null on X) and
    /// <c>stats.follow_ups_sent</c> / <c>stats.follow_ups_skipped</c>.
    /// </para>
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
    /// <c>follow_gate</c> merges too: <c>enabled = false</c> keeps the texts,
    /// new texts keep the switch, <c>["follow_gate"] = null</c> turns it off.
    /// <c>follow_up</c> merges the same way: keys left out keep their value,
    /// <c>tap</c> merges key by key, <c>enabled = false</c> turns it off and
    /// keeps the texts. To remove the button send
    /// <c>["follow_up"] = new Dictionary&lt;string, object?&gt; { ["button"] = null }</c>
    /// (a null property of an anonymous object is left out; a dictionary null
    /// is sent).
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

    /// <summary>
    /// "paused" pauses, "active" resumes (an X automation only while X DMs are
    /// on for the workspace, else <c>409 x_dms_off</c>).
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
