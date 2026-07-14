namespace OmniSocials;

/// <summary>Query parameters for <c>GET /analytics/overview</c>.</summary>
public sealed class AnalyticsOverviewParams
{
    /// <summary>Named period, e.g. "7d", "30d", "90d".</summary>
    public string? Period { get; set; }

    /// <summary>ISO date (YYYY-MM-DD); use with <see cref="EndDate"/> instead of <see cref="Period"/>.</summary>
    public string? StartDate { get; set; }

    /// <summary>ISO date (YYYY-MM-DD); use with <see cref="StartDate"/> instead of <see cref="Period"/>.</summary>
    public string? EndDate { get; set; }
}

/// <summary>Query parameters for <c>GET /analytics/accounts</c>.</summary>
public sealed class AccountAnalyticsParams
{
    /// <summary>Restrict to one platform, e.g. "instagram".</summary>
    public string? Platform { get; set; }

    /// <summary>ISO date (YYYY-MM-DD) to fetch a specific day's snapshot.</summary>
    public string? Date { get; set; }
}

/// <summary>Query parameters for <c>GET /analytics/best-times</c>.</summary>
public sealed class BestTimesParams
{
    /// <summary>Platform identifier, e.g. "instagram".</summary>
    public string Platform { get; set; } = "";

    /// <summary>IANA timezone for the returned slots, e.g. "Europe/Amsterdam".</summary>
    public string? Timezone { get; set; }
}
