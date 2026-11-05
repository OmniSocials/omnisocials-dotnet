using System.Text.Json.Serialization;

namespace OmniSocials;

/// <summary>Query parameters for <c>GET /limits</c>.</summary>
public sealed class LimitListParams
{
    /// <summary>
    /// Only the entries for this platform (entries for every channel stay in): instagram,
    /// facebook, linkedin, linkedin_page, youtube, tiktok, x, pinterest, bluesky, threads,
    /// mastodon, google_business, telegram, discord, reddit, snapchat.
    /// </summary>
    public string? Platform { get; set; }

    /// <summary>posts, media, inbox, analytics, automations, accounts, ads, team or other.</summary>
    public string? Area { get; set; }
}

/// <summary>Body for <c>POST /feature-requests</c>. Null properties are omitted.</summary>
public class FeatureRequestCreateParams
{
    /// <summary>
    /// What the person wants to do and why, in one or two sentences (10 to 1000 characters).
    /// Write it yourself; never paste a conversation.
    /// </summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = "";

    /// <summary>A short name, at most 120 characters. Made from the summary when left null.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>The platform it is about, when it is about one (same values as <see cref="LimitListParams.Platform"/>).</summary>
    [JsonPropertyName("platform")]
    public string? Platform { get; set; }

    /// <summary>posts, media, inbox, analytics, automations, accounts, ads, team or other.</summary>
    [JsonPropertyName("area")]
    public string? Area { get; set; }

    /// <summary>
    /// The <c>id</c> of the <c>GET /limits</c> entry the request is about. A second person of
    /// the same organisation then adds a vote to the open request instead of a new one.
    /// </summary>
    [JsonPropertyName("limit_id")]
    public string? LimitId { get; set; }
}
