using System.Text.Json;

namespace OmniSocials;

/// <summary>
/// Post tags: the workspace's own labels for its posts (a hook style, a
/// format), to filter and compare posts. Tags never go to a platform. Set a
/// post's tags with <see cref="PostCreateParams.Tags"/> /
/// <see cref="PostUpdateParams.Tags"/>, filter with
/// <see cref="PostListParams.Tag"/> and <see cref="AnalyticsOverviewParams.Tag"/>,
/// and compare tags with <see cref="AnalyticsResource.TagsAsync"/>. Uses the
/// <c>posts:read</c> / <c>posts:write</c> scopes.
/// </summary>
/// <remarks>
/// Error codes: <c>tag_not_found</c> (404), <c>tag_exists</c> (409, the
/// existing tag is in <c>error.tag</c> of <see cref="ApiException.Body"/>),
/// <c>tag_limit_reached</c> (409, at most 200 tags
/// per workspace), <c>invalid_tag_name</c>, <c>invalid_tag_color</c>,
/// <c>invalid_tags</c>, <c>invalid_post_ids</c>, <c>too_many_posts</c> and
/// <c>too_many_tags</c> (400). Optional typed models: <see cref="TagListResponse"/>,
/// <see cref="TagResponse"/>, <see cref="TagDeleteResponse"/>, <see cref="TagApplyResponse"/>.
/// </remarks>
public sealed class TagsResource
{
    private readonly OmniSocialsClient _client;

    internal TagsResource(OmniSocialsClient client) => _client = client;

    /// <summary><c>GET /tags</c>: every tag of the workspace, by name.</summary>
    public Task<JsonElement?> ListAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync("/tags", null, cancellationToken);

    /// <summary><c>POST /tags</c>: create a tag. Returns <c>{ data: Tag }</c> (201).</summary>
    public Task<JsonElement?> CreateAsync(TagCreateParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/tags", parameters, cancellationToken);

    /// <summary>
    /// <c>PATCH /tags/:id</c>: rename and/or recolour a tag. A property you
    /// leave null keeps its value; posts keep the tag.
    /// </summary>
    public Task<JsonElement?> UpdateAsync(string id, TagUpdateParams parameters, CancellationToken cancellationToken = default)
        => _client.PatchAsync($"/tags/{Uri.EscapeDataString(id)}", parameters, cancellationToken);

    /// <inheritdoc cref="UpdateAsync(string, TagUpdateParams, CancellationToken)"/>
    public Task<JsonElement?> UpdateAsync(long id, TagUpdateParams parameters, CancellationToken cancellationToken = default)
        => UpdateAsync(id.ToString(System.Globalization.CultureInfo.InvariantCulture), parameters, cancellationToken);

    /// <summary>
    /// <c>DELETE /tags/:id</c>: delete the tag and take it off every post of
    /// the workspace (the posts stay). Returns <c>{ data: { tag, posts_untagged } }</c>.
    /// </summary>
    public Task<JsonElement?> DeleteAsync(string id, CancellationToken cancellationToken = default)
        => _client.DeleteAsync($"/tags/{Uri.EscapeDataString(id)}", cancellationToken);

    /// <inheritdoc cref="DeleteAsync(string, CancellationToken)"/>
    public Task<JsonElement?> DeleteAsync(long id, CancellationToken cancellationToken = default)
        => DeleteAsync(id.ToString(System.Globalization.CultureInfo.InvariantCulture), cancellationToken);

    /// <summary>
    /// <c>POST /tags/apply</c>: add, remove or replace the tags of up to 500
    /// posts at once, published ones included. Returns
    /// <c>{ data: { updated, posts: [{ id, tags }], not_found } }</c>.
    /// </summary>
    public Task<JsonElement?> ApplyAsync(TagApplyParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/tags/apply", parameters, cancellationToken);
}
