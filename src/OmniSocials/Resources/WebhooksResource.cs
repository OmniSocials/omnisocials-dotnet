using System.Text.Json;

namespace OmniSocials;

/// <summary>Webhook endpoint management. For verifying deliveries, see <see cref="WebhookSignature"/>.</summary>
public sealed class WebhooksResource
{
    private readonly OmniSocialsClient _client;

    internal WebhooksResource(OmniSocialsClient client) => _client = client;

    /// <summary><c>GET /webhooks</c>: list the workspace's webhook endpoints.</summary>
    public Task<JsonElement?> ListAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync("/webhooks", null, cancellationToken);

    /// <summary><c>GET /webhooks/:id</c>: fetch a single webhook.</summary>
    public Task<JsonElement?> GetAsync(string id, CancellationToken cancellationToken = default)
        => _client.GetAsync($"/webhooks/{Uri.EscapeDataString(id)}", null, cancellationToken);

    /// <summary>
    /// <c>POST /webhooks</c>: register an endpoint for event deliveries
    /// (post.scheduled, post.published, post.failed). The response includes the
    /// signing <c>secret</c>; save it, it is only shown once.
    /// </summary>
    public Task<JsonElement?> CreateAsync(WebhookCreateParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/webhooks", parameters, cancellationToken);

    /// <summary><c>PATCH /webhooks/:id</c>: change the URL, events, or active state.</summary>
    public Task<JsonElement?> UpdateAsync(string id, WebhookUpdateParams parameters, CancellationToken cancellationToken = default)
        => _client.PatchAsync($"/webhooks/{Uri.EscapeDataString(id)}", parameters, cancellationToken);

    /// <summary><c>DELETE /webhooks/:id</c>: delete a webhook. Resolves to null (204).</summary>
    public Task<JsonElement?> DeleteAsync(string id, CancellationToken cancellationToken = default)
        => _client.DeleteAsync($"/webhooks/{Uri.EscapeDataString(id)}", cancellationToken);

    /// <summary>
    /// <c>POST /webhooks/:id/rotate-secret</c>: generate a new signing secret.
    /// Save the returned secret; it is only shown once.
    /// </summary>
    public Task<JsonElement?> RotateSecretAsync(string id, CancellationToken cancellationToken = default)
        => _client.PostAsync($"/webhooks/{Uri.EscapeDataString(id)}/rotate-secret", null, cancellationToken);
}
