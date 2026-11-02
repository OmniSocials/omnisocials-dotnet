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

    /// <summary>
    /// <c>GET /analytics/overview</c>: workspace-wide totals for a period.
    /// <see cref="AnalyticsOverviewParams.Tag"/> limits the post numbers to the
    /// posts with that tag; account numbers stay those of the whole account.
    /// </summary>
    public Task<JsonElement?> OverviewAsync(AnalyticsOverviewParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var query = new List<KeyValuePair<string, string?>>
        {
            new("period", parameters?.Period),
            new("start_date", parameters?.StartDate),
            new("end_date", parameters?.EndDate),
            new("tag", parameters?.Tag),
        };
        return _client.GetAsync("/analytics/overview", query, cancellationToken);
    }

    /// <summary>
    /// <c>GET /analytics/tags</c>: how each post tag did against your normal
    /// (default: the last 90 days). Returns <c>{ data: { period, platform,
    /// min_posts, tags, untagged, normals } }</c>; <c>tags</c> lists the
    /// comparable tags first, best first. <c>vs_normal</c> is a fraction (0.38
    /// = 38% above normal), <c>engagement_rate</c> a percent. Optional typed
    /// model: <see cref="TagAnalyticsResponse"/>. Requires <c>analytics:read</c>.
    /// </summary>
    public Task<JsonElement?> TagsAsync(TagAnalyticsParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var query = new List<KeyValuePair<string, string?>>
        {
            new("start_date", parameters?.StartDate),
            new("end_date", parameters?.EndDate),
            new("platform", parameters?.Platform),
            new("min_posts", parameters?.MinPosts?.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        };
        return _client.GetAsync("/analytics/tags", query, cancellationToken);
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
