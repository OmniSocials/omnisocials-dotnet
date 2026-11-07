using System.Text.Json.Serialization;

namespace OmniSocials;

// Typed response models for the /webhooks endpoints. Like every resource
// method, WebhooksResource returns the raw JsonElement? so callers can read
// the payload directly. These classes are optional, strongly-typed
// deserialization targets: e.g.
//   (await client.Webhooks.ListAsync())?.Deserialize<WebhookListResponse>().
// They mirror the API's snake_case fields via [JsonPropertyName].

/// <summary>The last failed delivery of a webhook.</summary>
public sealed class WebhookFailure
{
    [JsonPropertyName("at")]
    public string At { get; set; } = "";

    /// <summary>HTTP status of the failed delivery; null after an automatic switch-off.</summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>What went wrong; after an automatic switch-off, why the webhook was switched off.</summary>
    [JsonPropertyName("error")]
    public string Error { get; set; } = "";

    /// <summary>Delivery attempts; 0 after an automatic switch-off.</summary>
    [JsonPropertyName("attempts")]
    public int Attempts { get; set; }
}

/// <summary>A registered webhook endpoint.</summary>
public sealed class Webhook
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("events")]
    public List<string> Events { get; set; } = new();

    /// <summary>
    /// The workspace whose events the webhook receives. Null for an
    /// organisation-wide webhook (made in the OmniSocials dashboard), which
    /// receives the events of every workspace of the organisation. Webhooks
    /// created through the API belong to the key's workspace.
    /// </summary>
    [JsonPropertyName("workspace_id")]
    public long? WorkspaceId { get; set; }

    /// <summary>
    /// False when switched off: by an update, or automatically when the person
    /// behind the webhook lost access to its organisation or workspace. An
    /// automatic switch-off says why in <see cref="LastFailure"/>.
    /// </summary>
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("last_triggered_at")]
    public string? LastTriggeredAt { get; set; }

    [JsonPropertyName("failure_count")]
    public int FailureCount { get; set; }

    [JsonPropertyName("last_failure")]
    public WebhookFailure? LastFailure { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>Signing secret; only returned on create and rotate-secret.</summary>
    [JsonPropertyName("secret")]
    public string? Secret { get; set; }
}

/// <summary>Envelope of <c>GET /webhooks/:id</c>, create and update.</summary>
public sealed class WebhookResponse
{
    [JsonPropertyName("data")]
    public Webhook Data { get; set; } = new();
}

/// <summary>Envelope of <c>GET /webhooks</c>.</summary>
public sealed class WebhookListResponse
{
    [JsonPropertyName("data")]
    public List<Webhook> Data { get; set; } = new();
}
