using System.Globalization;
using System.Text.Json;

namespace OmniSocials;

/// <summary>
/// Comment-to-DM automations. An automation answers a keyword in an Instagram
/// comment (or a DM, or a story reply) with a DM, once per person, with an
/// optional public reply under the comment. With <c>Platform = "facebook"</c>
/// it runs on a Facebook Page instead, and the DM lands in Messenger (a Page
/// has no story replies). With <c>Platform = "x"</c> it answers a keyword in a
/// DM to the X account ("dm" kind only, no post, no public reply) while X DMs
/// are on for the workspace; every X answer ends with "Reply STOP to stop
/// automatic messages." and a person who replies STOP gets no automatic DM
/// again. Meta rules, the same on Instagram and Facebook: one private reply
/// per comment, within 7 days. OmniSocials caps sends at 180 DMs an hour per
/// account on Instagram and Facebook, and at 15 per 15 minutes and 450 a day
/// on X; an answer over a cap waits in a queue ("queued") for up to an hour.
/// The scopes are opt-in on the API key: <c>automations:read</c> for
/// list / get / events, <c>automations:write</c> for create / update / delete
/// (and for <see cref="PostCreateParams.CommentDmAutomation"/>).
/// </summary>
public sealed class AutomationsResource
{
    private readonly OmniSocialsClient _client;

    internal AutomationsResource(OmniSocialsClient client) => _client = client;

    /// <summary>
    /// <c>GET /automations</c>: list the workspace's comment-to-DM automations,
    /// optionally by <see cref="AutomationListParams.Status"/>, by
    /// <see cref="AutomationListParams.Platform"/> or by the OmniSocials
    /// <see cref="AutomationListParams.PostId"/> they are bound to. Requires the
    /// <c>automations:read</c> scope (opt-in on the API key).
    /// </summary>
    public Task<JsonElement?> ListAsync(AutomationListParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<KeyValuePair<string, string?>>
        {
            new("status", parameters?.Status),
            new("platform", parameters?.Platform),
            new("post_id", parameters?.PostId),
        };
        return _client.GetAsync("/automations", queryParams, cancellationToken);
    }

    /// <summary><c>GET /automations/:id</c>: fetch a single automation. Requires the <c>automations:read</c> scope (opt-in on the API key).</summary>
    public Task<JsonElement?> GetAsync(string id, CancellationToken cancellationToken = default)
        => _client.GetAsync($"/automations/{Uri.EscapeDataString(id)}", null, cancellationToken);

    /// <summary>
    /// <c>POST /automations</c>: create a comment-to-DM automation.
    /// <see cref="AutomationCreateParams.Platform"/> "facebook" runs it on a
    /// Facebook Page (the DM lands in Messenger); "x" answers DMs to the X
    /// account (trigger kinds ["dm"], no post; an active one needs X DMs on,
    /// else <c>409 x_dms_off</c>, while status "paused" always saves). Bind it
    /// to one post with
    /// <see cref="AutomationCreateParams.PostId"/> (an OmniSocials post that
    /// goes to the channel, scheduled or live; one automation per post and
    /// platform), <see cref="AutomationCreateParams.InstagramMediaId"/> (an
    /// Instagram post published outside OmniSocials) or
    /// <see cref="AutomationCreateParams.FacebookPostId"/> (a Page post
    /// published outside OmniSocials; implies "facebook"), at most one; a
    /// trigger with scope "next_post" or "any" needs none.
    /// <see cref="AutomationCreateParams.Actions"/> is required. Requires the
    /// <c>automations:write</c> scope (opt-in on the API key). Errors:
    /// <c>400 validation_error</c>, <c>400 post_not_instagram</c>,
    /// <c>400 post_not_facebook</c>, <c>400 post_required</c>,
    /// <c>404 post_not_found</c>, <c>404 instagram_media_not_found</c>,
    /// <c>404 facebook_post_not_found</c>, <c>409 instagram_not_connected</c>,
    /// <c>409 instagram_needs_reauth</c>, <c>409 facebook_not_connected</c>,
    /// <c>409 facebook_needs_reauth</c>, <c>409 x_not_connected</c>,
    /// <c>409 x_dms_off</c>, <c>409 automation_exists</c> (the error body also
    /// carries <c>automation_id</c>).
    /// </summary>
    public Task<JsonElement?> CreateAsync(AutomationCreateParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/automations", parameters, cancellationToken);

    /// <summary>
    /// <c>PATCH /automations/:id</c>: partial update. Each section
    /// (<c>Trigger</c>, <c>Actions</c>, <c>Limits</c>) is merged into the
    /// stored one; keys you omit keep their value: new <c>keywords</c> keep
    /// the kinds, the scope and the match; a new <c>dm.text</c> keeps the
    /// public reply; <c>stop_at</c> keeps <c>max_sends</c>.
    /// <c>public_reply.enabled = false</c> or <c>variations = []</c> turns the
    /// public reply off; <c>["max_sends"] = null</c> removes that limit.
    /// <c>Status = "paused"</c> pauses and <c>"active"</c> resumes; another
    /// <c>PostId</c> / <c>InstagramMediaId</c> / <c>FacebookPostId</c> moves the
    /// automation to that post. The platform cannot change
    /// (<c>400 platform_immutable</c>). <c>Status = "active"</c> on an X
    /// automation needs X DMs on for the workspace (<c>409 x_dms_off</c>). A
    /// <c>completed</c> automation starts again when its limits or trigger
    /// change. Requires the
    /// <c>automations:write</c> scope. <c>400 no_fields</c> when the body is
    /// empty.
    /// </summary>
    public Task<JsonElement?> UpdateAsync(string id, AutomationUpdateParams parameters, CancellationToken cancellationToken = default)
        => _client.PatchAsync($"/automations/{Uri.EscapeDataString(id)}", parameters, cancellationToken);

    /// <summary><c>DELETE /automations/:id</c>: delete an automation. Resolves to null (204). Requires the <c>automations:write</c> scope.</summary>
    public Task<JsonElement?> DeleteAsync(string id, CancellationToken cancellationToken = default)
        => _client.DeleteAsync($"/automations/{Uri.EscapeDataString(id)}", cancellationToken);

    /// <summary>
    /// <c>GET /automations/:id/events</c>: the automation's activity, one event
    /// per person it answered, skipped or failed on. Filter by
    /// <see cref="AutomationEventListParams.Outcome"/> ("sent", "skipped",
    /// "failed", "pending", "queued", "opted_out"); <c>Limit</c> is 1 to 100 (default 25),
    /// <c>Offset</c> skips. Requires the <c>automations:read</c> scope.
    /// <para>
    /// The response is not the plain list envelope:
    /// <c>{ "data": [...], "counts": { "sent": 12, "skipped": 3 }, "pagination": {...} }</c>,
    /// where <c>counts</c> is the number of events per outcome (an outcome with
    /// no events is left out). Each event carries <c>kind</c> ("comment",
    /// "dm", "story_reply"), <c>outcome</c>, <c>reason</c> /
    /// <c>reason_label</c> (X adds the skip reasons "dms_closed", "no_credits",
    /// "opted_out" and "automation_off"), <c>person</c> (<c>id</c>, <c>username</c>; on
    /// Facebook the person's name, since Facebook has no @handle),
    /// <c>trigger_id</c>, <c>trigger_text</c>, <c>message</c> (the DM sent),
    /// <c>public_reply</c>, <c>platform_message_id</c>, <c>created_at</c> and
    /// <c>sent_at</c>.
    /// </para>
    /// </summary>
    public Task<JsonElement?> ListEventsAsync(string id, AutomationEventListParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<KeyValuePair<string, string?>>
        {
            new("outcome", parameters?.Outcome),
            new("limit", parameters?.Limit?.ToString(CultureInfo.InvariantCulture)),
            new("offset", parameters?.Offset?.ToString(CultureInfo.InvariantCulture)),
        };
        return _client.GetAsync($"/automations/{Uri.EscapeDataString(id)}/events", queryParams, cancellationToken);
    }
}
