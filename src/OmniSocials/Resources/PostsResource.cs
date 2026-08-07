using System.Text.Json;

namespace OmniSocials;

/// <summary>Posts: create, schedule, publish, update, delete.</summary>
public sealed class PostsResource
{
    private readonly OmniSocialsClient _client;

    internal PostsResource(OmniSocialsClient client) => _client = client;

    /// <summary><c>GET /posts</c>: list posts in the workspace (newest first).</summary>
    public Task<JsonElement?> ListAsync(PostListParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var query = new List<KeyValuePair<string, string?>>
        {
            new("status", parameters?.Status),
            new("limit", parameters?.Limit?.ToString()),
            new("offset", parameters?.Offset?.ToString()),
        };
        return _client.GetAsync("/posts", query, cancellationToken);
    }

    /// <summary><c>GET /posts/:id</c>: fetch a single post.</summary>
    public Task<JsonElement?> GetAsync(string id, CancellationToken cancellationToken = default)
        => _client.GetAsync($"/posts/{Uri.EscapeDataString(id)}", null, cancellationToken);

    /// <summary>
    /// <c>GET /posts/recent-platform</c>: recent posts fetched live from the
    /// connected platform APIs (including content published outside OmniSocials).
    /// The fallback for brand-new workspaces where ListAsync is empty. Requires
    /// the analytics:read scope.
    /// </summary>
    public Task<JsonElement?> RecentPlatformAsync(RecentPlatformParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var query = new List<KeyValuePair<string, string?>>
        {
            new("limit", parameters?.Limit?.ToString()),
            new("platforms", parameters?.Platforms is { Count: > 0 } platforms ? string.Join(",", platforms) : null),
        };
        return _client.GetAsync("/posts/recent-platform", query, cancellationToken);
    }

    /// <summary>
    /// <c>POST /posts/create</c>: create a draft or scheduled post. When the
    /// post targets X and its text (or any thread part) contains a URL, the
    /// response includes a top-level <c>warnings</c> array (sibling of
    /// <c>data</c>) with a <c>x_url_post_credits</c> entry carrying
    /// <c>credits_required</c> and <c>credits_balance</c>: X's link-post fee
    /// is passed through as prepaid credits, debited at publish time (from
    /// 2026-08-14). Credits are managed in the dashboard, not the API.
    /// </summary>
    public Task<JsonElement?> CreateAsync(PostCreateParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/posts/create", parameters, cancellationToken);

    /// <summary>
    /// <c>POST /posts/create-and-publish</c>: create a post and publish it immediately.
    /// See <see cref="CreateAsync"/> for the <c>warnings</c> array on X link posts.
    /// </summary>
    public Task<JsonElement?> CreateAndPublishAsync(PostCreateParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/posts/create-and-publish", parameters, cancellationToken);

    /// <summary><c>PATCH /posts/:id</c>: update a draft or scheduled post.</summary>
    public Task<JsonElement?> UpdateAsync(string id, PostUpdateParams parameters, CancellationToken cancellationToken = default)
        => _client.PatchAsync($"/posts/{Uri.EscapeDataString(id)}", parameters, cancellationToken);

    /// <summary><c>DELETE /posts/:id</c>: delete a post. Resolves to null (204).</summary>
    public Task<JsonElement?> DeleteAsync(string id, CancellationToken cancellationToken = default)
        => _client.DeleteAsync($"/posts/{Uri.EscapeDataString(id)}", cancellationToken);

    /// <summary><c>POST /posts/:id/publish</c>: publish a draft or scheduled post now.</summary>
    public Task<JsonElement?> PublishAsync(string id, CancellationToken cancellationToken = default)
        => _client.PostAsync($"/posts/{Uri.EscapeDataString(id)}/publish", null, cancellationToken);

    /// <summary>
    /// <c>POST /posts/:id/retry</c>: retry the failed platforms of a <c>failed</c>
    /// or <c>warning</c> (partially failed) post, on the same post. Only the
    /// platforms that failed are re-published; platforms that already succeeded
    /// are never posted again. Asynchronous: a 200 means the retry is queued -
    /// poll GetAsync for the outcome. Max 3 retries per platform.
    /// </summary>
    public Task<JsonElement?> RetryAsync(string id, CancellationToken cancellationToken = default)
        => _client.PostAsync($"/posts/{Uri.EscapeDataString(id)}/retry", null, cancellationToken);
}
