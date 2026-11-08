using System.Text.Json.Serialization;

namespace OmniSocials;

// Optional typed models for X statistics on credits. The resource methods
// return the raw JsonElement?; deserialize into these when you want types:
//   (await client.Credits.GetAsync())?.Deserialize<CreditsResponse>().

/// <summary>
/// <c>x_statistics</c> of an X account (<c>accounts</c> list, get and update):
/// how often its X statistics are read, who pays and whether reads are paused.
/// </summary>
public sealed class AccountXStatistics
{
    /// <summary><c>full</c>, <c>light</c>, <c>off</c> or <c>custom</c>.</summary>
    [JsonPropertyName("posts")]
    public string Posts { get; set; } = "";

    /// <summary><c>daily</c>, <c>weekly</c>, <c>off</c>, <c>hourly</c> or <c>6h</c>.</summary>
    [JsonPropertyName("followers")]
    public string Followers { get; set; } = "";

    /// <summary>The custom schedule when <see cref="Posts"/> is <c>custom</c>, else null.</summary>
    [JsonPropertyName("post_schedule")]
    public XStatisticsPostSchedule? PostSchedule { get; set; }

    /// <summary><c>account</c>, <c>customer</c>, <c>partner_default</c> or <c>default</c>.</summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = "";

    /// <summary><c>credits</c>, or <c>own_x_app</c> when an OmniSocials Connect partner's own X app pays.</summary>
    [JsonPropertyName("billing")]
    public string Billing { get; set; } = "";

    /// <summary><c>active</c>, <c>paused</c> (out of credits) or <c>off</c>.</summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = "";

    /// <summary><c>insufficient_credits</c> while paused, else null.</summary>
    [JsonPropertyName("paused_reason")]
    public string? PausedReason { get; set; }
}

/// <summary>One tweet read by <see cref="AnalyticsResource.RefreshPostAsync(string, CancellationToken)"/>.</summary>
public sealed class PostRefreshTweet
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("metrics")]
    public Dictionary<string, System.Text.Json.JsonElement> Metrics { get; set; } = new();

    [JsonPropertyName("read_at")]
    public string? ReadAt { get; set; }
}

/// <summary><c>data</c> of <c>POST /analytics/posts/:id/refresh</c>.</summary>
public sealed class PostRefreshResult
{
    /// <summary>The post id (sent as a number; a string is read too).</summary>
    [JsonPropertyName("post_id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long PostId { get; set; }

    [JsonPropertyName("tweets")]
    public List<PostRefreshTweet> Tweets { get; set; } = new();

    /// <summary>0.5 per tweet (plus 0.5 per video or GIF tweet); 0 when already read this UTC day.</summary>
    [JsonPropertyName("credits_used")]
    public decimal CreditsUsed { get; set; }

    [JsonPropertyName("read_at")]
    public string? ReadAt { get; set; }
}

/// <summary>The <c>{ data }</c> envelope of a refresh.</summary>
public sealed class PostRefreshResponse
{
    [JsonPropertyName("data")]
    public PostRefreshResult Data { get; set; } = new();
}

/// <summary>Whether X statistics are charged yet, and from when.</summary>
public sealed class CreditsXStatisticsCharging
{
    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("from")]
    public string? From { get; set; }
}

/// <summary>Credits per read: post 0.5, media (video or GIF) 0.5, follower count 1.</summary>
public sealed class CreditsXStatisticsPrices
{
    [JsonPropertyName("post_read")]
    public decimal PostRead { get; set; }

    [JsonPropertyName("media_read")]
    public decimal MediaRead { get; set; }

    [JsonPropertyName("follower_read")]
    public decimal FollowerRead { get; set; }
}

/// <summary>This month's X reads and the credits they used.</summary>
public sealed class CreditsXStatisticsUsage
{
    [JsonPropertyName("post_reads")]
    public int PostReads { get; set; }

    [JsonPropertyName("media_reads")]
    public int MediaReads { get; set; }

    [JsonPropertyName("follower_reads")]
    public int FollowerReads { get; set; }

    [JsonPropertyName("refreshes")]
    public int Refreshes { get; set; }

    [JsonPropertyName("credits")]
    public decimal Credits { get; set; }
}

/// <summary>One X account with its setting and its estimate for a month.</summary>
public sealed class CreditsXStatisticsAccount
{
    /// <summary>The account id as in the accounts list (<c>&lt;workspace_id&gt;_x</c>).</summary>
    [JsonPropertyName("account_id")]
    public string AccountId { get; set; } = "";

    [JsonPropertyName("workspace_id")]
    public long WorkspaceId { get; set; }

    [JsonPropertyName("handle")]
    public string? Handle { get; set; }

    [JsonPropertyName("posts")]
    public string Posts { get; set; } = "";

    [JsonPropertyName("followers")]
    public string Followers { get; set; } = "";

    /// <summary>Tweets published in the last 30 days.</summary>
    [JsonPropertyName("tweets_30d")]
    public int Tweets30d { get; set; }

    [JsonPropertyName("credits_month")]
    public decimal CreditsMonth { get; set; }
}

/// <summary>What X statistics cost the organisation.</summary>
public sealed class CreditsXStatistics
{
    /// <summary><c>active</c> or <c>paused</c> (the balance reached 0).</summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = "";

    [JsonPropertyName("paused_at")]
    public string? PausedAt { get; set; }

    [JsonPropertyName("charging")]
    public CreditsXStatisticsCharging Charging { get; set; } = new();

    [JsonPropertyName("prices")]
    public CreditsXStatisticsPrices Prices { get; set; } = new();

    /// <summary>The estimate for a month over every X account.</summary>
    [JsonPropertyName("total_credits_month")]
    public decimal TotalCreditsMonth { get; set; }

    [JsonPropertyName("this_month")]
    public CreditsXStatisticsUsage ThisMonth { get; set; } = new();

    [JsonPropertyName("accounts")]
    public List<CreditsXStatisticsAccount> Accounts { get; set; } = new();
}

/// <summary><c>data</c> of <c>GET /credits</c>. 1 credit = EUR 0.01.</summary>
public sealed class CreditsOverview
{
    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }

    [JsonPropertyName("auto_refill")]
    public CreditsAutoRefill AutoRefill { get; set; } = new();

    /// <summary>Credits used this month per reason (for example <c>x_statistics</c>, <c>x_url_post</c>).</summary>
    [JsonPropertyName("this_month")]
    public Dictionary<string, decimal> ThisMonth { get; set; } = new();

    [JsonPropertyName("x_statistics")]
    public CreditsXStatistics XStatistics { get; set; } = new();
}

/// <summary>Whether auto-refill is on.</summary>
public sealed class CreditsAutoRefill
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}

/// <summary>The <c>{ data }</c> envelope of <c>GET /credits</c>.</summary>
public sealed class CreditsResponse
{
    [JsonPropertyName("data")]
    public CreditsOverview Data { get; set; } = new();
}

/// <summary>
/// <c>data</c> of an <c>x_statistics.paused</c> or <c>x_statistics.resumed</c>
/// webhook delivery: the organisation's X statistics stopped at 0 credits
/// (<c>insufficient_credits</c>) or started again after a top-up
/// (<c>credits_added</c>). One delivery per workspace with X.
/// </summary>
public sealed class XStatisticsEventData
{
    /// <summary>Sent as a number; a string is read too.</summary>
    [JsonPropertyName("workspace_id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long? WorkspaceId { get; set; }

    /// <summary>The X account as in the accounts list (<c>&lt;workspace_id&gt;_x</c>).</summary>
    [JsonPropertyName("account_id")]
    public string AccountId { get; set; } = "";

    /// <summary><c>insufficient_credits</c> or <c>credits_added</c>.</summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "";
}

/// <summary>The X statistics webhook event names.</summary>
public static class XStatisticsWebhookEvents
{
    public const string Paused = "x_statistics.paused";
    public const string Resumed = "x_statistics.resumed";

    /// <summary>Both events.</summary>
    public static readonly IReadOnlyList<string> All = new[] { Paused, Resumed };
}
