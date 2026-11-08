using System.Text.Json.Serialization;

namespace OmniSocials;

/// <summary>Body for <c>POST /webhooks</c>.</summary>
public class WebhookCreateParams
{
    /// <summary>HTTPS endpoint that will receive event deliveries.</summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    /// <summary>
    /// Events to subscribe to: post.scheduled, post.published, post.failed,
    /// post.approved, post.rejected, ad.status_changed (its <c>data</c> is the
    /// campaign, see <see cref="AdStatusChangedEventData"/>), ad.created,
    /// ad.updated, ad.review, ad.delivery_issue, campaign.created,
    /// campaign.updated, campaign.budget, lead.received, ads.sync_failed,
    /// ads.sync_recovered, ads.initial_sync_completed, ad_rule.triggered, and
    /// x_statistics.paused / x_statistics.resumed (the organisation's X
    /// statistics stopped at 0 credits or started again after a top-up; its
    /// <c>data</c> is <see cref="XStatisticsEventData"/>, names in
    /// <see cref="XStatisticsWebhookEvents"/>). Any Ads
    /// event (ad.*, campaign.*, lead.*, ads.*, ad_rule.*) needs the
    /// <c>ads:read</c> scope on the key and <c>access_ads</c> for its person,
    /// else <c>403 insufficient_scope</c> / <c>insufficient_permission</c>.
    /// </summary>
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
