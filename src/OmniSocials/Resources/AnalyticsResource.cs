using System.Text.Json;

namespace OmniSocials;

/// <summary>Post, account, and workspace analytics.</summary>
public sealed class AnalyticsResource
{
    private readonly OmniSocialsClient _client;

    internal AnalyticsResource(OmniSocialsClient client) => _client = client;

    /// <summary><c>GET /analytics/posts/:id</c>: latest per-platform metrics for one post.</summary>
    public Task<JsonElement?> PostAsync(string postId, CancellationToken cancellationToken = default)
        => _client.GetAsync($"/analytics/posts/{Uri.EscapeDataString(postId)}", null, cancellationToken);

    /// <summary>
    /// <c>GET /analytics/posts?ids=a,b,c</c>: batch metrics for up to 100 posts
    /// in one call instead of one request per post.
    /// </summary>
    public Task<JsonElement?> PostsAsync(IEnumerable<string> postIds, CancellationToken cancellationToken = default)
    {
        var query = new List<KeyValuePair<string, string?>>
        {
            new("ids", string.Join(",", postIds)),
        };
        return _client.GetAsync("/analytics/posts", query, cancellationToken);
    }

    /// <summary><c>GET /analytics/overview</c>: workspace-wide totals for a period.</summary>
    public Task<JsonElement?> OverviewAsync(AnalyticsOverviewParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var query = new List<KeyValuePair<string, string?>>
        {
            new("period", parameters?.Period),
            new("start_date", parameters?.StartDate),
            new("end_date", parameters?.EndDate),
        };
        return _client.GetAsync("/analytics/overview", query, cancellationToken);
    }

    /// <summary><c>GET /analytics/accounts</c>: account-level stats (followers etc).</summary>
    public Task<JsonElement?> AccountsAsync(AccountAnalyticsParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var query = new List<KeyValuePair<string, string?>>
        {
            new("platform", parameters?.Platform),
            new("date", parameters?.Date),
        };
        return _client.GetAsync("/analytics/accounts", query, cancellationToken);
    }

    /// <summary>
    /// <c>GET /analytics/best-times</c>: recommended posting time slots for a
    /// platform, based on when the account's audience engages.
    /// </summary>
    public Task<JsonElement?> BestTimesAsync(BestTimesParams parameters, CancellationToken cancellationToken = default)
    {
        var query = new List<KeyValuePair<string, string?>>
        {
            new("platform", parameters.Platform),
            new("timezone", parameters.Timezone),
        };
        return _client.GetAsync("/analytics/best-times", query, cancellationToken);
    }
}
