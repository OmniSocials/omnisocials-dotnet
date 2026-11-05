using System.Text.Json.Serialization;

namespace OmniSocials;

// Typed response models for GET /limits and /feature-requests. Like every
// resource method, LimitsResource and FeatureRequestsResource return the raw
// JsonElement? so callers can read the payload directly. These classes are
// optional, strongly-typed deserialization targets: e.g.
//   (await client.FeatureRequests.CreateAsync(p))?.Deserialize<FeatureRequestCreateResponse>().
// They mirror the API's snake_case fields via [JsonPropertyName].

/// <summary>Something OmniSocials cannot do, why, and what to do instead.</summary>
public sealed class Limit
{
    /// <summary>For example "tiktok.delete". Pass it as <see cref="FeatureRequestCreateParams.LimitId"/>.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    /// <summary>
    /// platform, by_design, not_built, beta, dashboard_only or needs_setup. "platform" and
    /// "by_design" cannot change through a request.
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "";

    /// <summary>The kind in words, e.g. "The platform does not allow it".</summary>
    [JsonPropertyName("kind_label")]
    public string KindLabel { get; set; } = "";

    [JsonPropertyName("area")]
    public string Area { get; set; } = "";

    /// <summary>The platforms it is about; empty = every channel.</summary>
    [JsonPropertyName("platforms")]
    public List<string> Platforms { get; set; } = new();

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    /// <summary>The reason, to tell the person.</summary>
    [JsonPropertyName("says")]
    public string Says { get; set; } = "";

    /// <summary>What to do instead, null when there is nothing.</summary>
    [JsonPropertyName("instead")]
    public string? Instead { get; set; }

    /// <summary>Whether a feature request can name it. False for "platform" and "by_design".</summary>
    [JsonPropertyName("requestable")]
    public bool Requestable { get; set; }
}

/// <summary>Response of <c>GET /limits</c>.</summary>
public sealed class LimitListResponse
{
    [JsonPropertyName("data")]
    public List<Limit> Data { get; set; } = new();

    /// <summary>A label per kind ("platform": "The platform does not allow it", ...).</summary>
    [JsonPropertyName("kinds")]
    public Dictionary<string, string> Kinds { get; set; } = new();
}

/// <summary>A reply of the OmniSocials team on a request.</summary>
public sealed class FeatureRequestTeamReply
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    /// <summary>ISO 8601.</summary>
    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = "";
}

/// <summary>A feature request of the person who owns the key.</summary>
public sealed class FeatureRequest
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = "";

    /// <summary>open, in_progress, completed or declined.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    /// <summary>The OmniSocials team's answer, when there is one.</summary>
    [JsonPropertyName("team_answer")]
    public string? TeamAnswer { get; set; }

    /// <summary>Replies of the OmniSocials team, oldest first.</summary>
    [JsonPropertyName("team_replies")]
    public List<FeatureRequestTeamReply> TeamReplies { get; set; } = new();

    /// <summary>People of the organisation who asked for the same thing.</summary>
    [JsonPropertyName("votes_count")]
    public int VotesCount { get; set; }

    [JsonPropertyName("limit_id")]
    public string? LimitId { get; set; }

    [JsonPropertyName("platform")]
    public string? Platform { get; set; }

    [JsonPropertyName("area")]
    public string? Area { get; set; }

    /// <summary>Where the request was sent from: app, mcp, api or cli.</summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = "";

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = "";

    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; } = "";

    [JsonPropertyName("completed_at")]
    public string? CompletedAt { get; set; }
}

/// <summary>Response of <c>GET /feature-requests</c>.</summary>
public sealed class FeatureRequestListResponse
{
    [JsonPropertyName("data")]
    public List<FeatureRequest> Data { get; set; } = new();
}

/// <summary>Response of <c>GET /feature-requests/:id</c>.</summary>
public sealed class FeatureRequestResponse
{
    [JsonPropertyName("data")]
    public FeatureRequest Data { get; set; } = new();
}

/// <summary>Response of <c>POST /feature-requests</c>.</summary>
public sealed class FeatureRequestCreateResponse
{
    /// <summary>
    /// "created" (a new request), "voted" (a vote added to the organisation's open request for
    /// the same gap) or "already_requested" (this person already has an open request for it).
    /// </summary>
    [JsonPropertyName("outcome")]
    public string Outcome { get; set; } = "";

    /// <summary>The request the outcome is about.</summary>
    [JsonPropertyName("data")]
    public FeatureRequest Data { get; set; } = new();
}
