using System.Text.Json.Serialization;

namespace OmniSocials;

/// <summary>Body for <c>POST /webhooks</c>.</summary>
public class WebhookCreateParams
{
    /// <summary>HTTPS endpoint that will receive event deliveries.</summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    /// <summary>Events to subscribe to: post.scheduled, post.published, post.failed.</summary>
    [JsonPropertyName("events")]
    public IList<string> Events { get; set; } = new List<string>();
}

/// <summary>Body for <c>PATCH /webhooks/:id</c>. Null properties are omitted.</summary>
public class WebhookUpdateParams
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("events")]
    public IList<string>? Events { get; set; }

    [JsonPropertyName("is_active")]
    public bool? IsActive { get; set; }
}
