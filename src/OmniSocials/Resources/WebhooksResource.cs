using System.Text.Json;

namespace OmniSocials;

/// <summary>Webhook endpoint management. For verifying deliveries, see <see cref="WebhookSignature"/>.</summary>
public sealed class WebhooksResource
{
    private readonly OmniSocialsClient _client;

    internal WebhooksResource(OmniSocialsClient client) => _client = client;

    /// <summary>
    /// <c>GET /webhooks</c>: list the webhooks of the key's workspace and the
    /// organisation-wide webhooks of its organisation (<c>workspace_id</c>
    /// null, made in the OmniSocials dashboard; they receive the events of
    /// every workspace). A webhook whose creator lost access to the
    /// organisation or workspace is switched off
    /// automatically: <c>is_active</c> is false and <c>last_failure.error</c>
    /// says why. Deserialize into <see cref="WebhookListResponse"/> for typed
    /// access.
    /// </summary>
    public Task<JsonElement?> ListAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync("/webhooks", null, cancellationToken);

    /// <summary>
    /// <c>GET /webhooks/:id</c>: fetch a webhook of the key's workspace, or an
    /// organisation-wide webhook of its organisation (<c>workspace_id</c>
    /// null). Deserialize into <see cref="WebhookResponse"/> for typed access.
    /// </summary>
    public Task<JsonElement?> GetAsync(string id, CancellationToken cancellationToken = default)
        => _client.GetAsync($"/webhooks/{Uri.EscapeDataString(id)}", null, cancellationToken);

    /// <summary>
    /// <c>POST /webhooks</c>: register an endpoint for event deliveries
    /// (post.scheduled, post.published, post.failed, post.approved,
    /// post.rejected, and for ads ad.status_changed, ad.created, ad.updated,
    /// ad.review, ad.delivery_issue, campaign.created, campaign.updated,
    /// campaign.budget, lead.received, ads.sync_failed, ads.sync_recovered,
    /// ads.initial_sync_completed, ad_rule.triggered). The <c>data</c> of an
    /// <c>ad.status_changed</c> delivery is the campaign, not a post (see
    /// <see cref="AdStatusChangedEventData"/>); the other campaign events carry
    /// the campaign plus <c>details</c>, the rest their own <c>data</c>. Any Ads
    /// event needs the <c>ads:read</c> scope on the key and <c>access_ads</c> for
    /// its person (<c>403 insufficient_scope</c> /
    /// <c>insufficient_permission</c>). The response includes the signing
    /// <c>secret</c>; save it, it is only shown once.
    /// </summary>
    public Task<JsonElement?> CreateAsync(WebhookCreateParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/webhooks", parameters, cancellationToken);

    /// <summary>
    /// <c>PATCH /webhooks/:id</c>: change the URL, events, or active state.
    /// Works on organisation-wide webhooks too. <c>IsActive = true</c>
    /// switches on a webhook that was switched off (also an automatic
    /// switch-off); setting the URL, the events or switching it on makes the
    /// key's creator the person behind the webhook. Setting the URL on an
    /// organisation-wide webhook (even its current one) moves it to the
    /// key's workspace: from then on it gets only that workspace's events,
    /// and the answer shows the new <c>workspace_id</c>. When the webhook has
    /// Ads events after the change, setting the URL or the events or
    /// switching it on needs the <c>ads:read</c> scope and <c>access_ads</c>
    /// for the key's creator (403 otherwise, nothing changed).
    /// </summary>
    public Task<JsonElement?> UpdateAsync(string id, WebhookUpdateParams parameters, CancellationToken cancellationToken = default)
        => _client.PatchAsync($"/webhooks/{Uri.EscapeDataString(id)}", parameters, cancellationToken);

    /// <summary>
    /// <c>DELETE /webhooks/:id</c>: delete a webhook of the key's workspace or
    /// an organisation-wide webhook of its organisation. Resolves to null (204).
    /// </summary>
    public Task<JsonElement?> DeleteAsync(string id, CancellationToken cancellationToken = default)
        => _client.DeleteAsync($"/webhooks/{Uri.EscapeDataString(id)}", cancellationToken);

    /// <summary>
    /// <c>POST /webhooks/:id/rotate-secret</c>: generate a new signing secret.
    /// Save the returned secret; it is only shown once. The old secret keeps
    /// signing for 24 hours: every delivery then carries
    /// <c>X-OmniSocials-Signature</c> (new secret) and
    /// <c>X-OmniSocials-Signature-Previous</c> (old secret, same format, same
    /// timestamp and body). Verify the first with the new secret and, when
    /// that fails, the second with the old one (see
    /// <see cref="WebhookSignature"/>). Rotating again within the 24
    /// hours replaces the previous secret with the one just rotated out. Works
    /// on organisation-wide webhooks too.
    /// </summary>
    public Task<JsonElement?> RotateSecretAsync(string id, CancellationToken cancellationToken = default)
        => _client.PostAsync($"/webhooks/{Uri.EscapeDataString(id)}/rotate-secret", null, cancellationToken);
}
