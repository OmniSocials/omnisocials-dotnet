using System.Text.Json;

namespace OmniSocials;

/// <summary>
/// Saved, reusable hashtag groups. Apply a set to a post at create time via
/// <see cref="PostCreateParams.HashtagSet"/> (name, case-insensitive) or
/// <see cref="PostCreateParams.HashtagSetId"/>.
/// </summary>
public sealed class HashtagSetsResource
{
    private readonly OmniSocialsClient _client;

    internal HashtagSetsResource(OmniSocialsClient client) => _client = client;

    /// <summary><c>GET /hashtag-sets</c>: list the workspace's saved hashtag sets.</summary>
    public Task<JsonElement?> ListAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync("/hashtag-sets", null, cancellationToken);

    /// <summary><c>GET /hashtag-sets/:id</c>: fetch a single hashtag set.</summary>
    public Task<JsonElement?> GetAsync(string id, CancellationToken cancellationToken = default)
        => _client.GetAsync($"/hashtag-sets/{Uri.EscapeDataString(id)}", null, cancellationToken);

    /// <summary>
    /// <c>POST /hashtag-sets</c>: create a hashtag set.
    /// <see cref="HashtagSetCreateParams.Hashtags"/> is a <c>string[]</c>, or a
    /// single string of tags.
    /// </summary>
    public Task<JsonElement?> CreateAsync(HashtagSetCreateParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/hashtag-sets", parameters, cancellationToken);

    /// <summary>
    /// <c>PATCH /hashtag-sets/:id</c>: rename (<see cref="HashtagSetUpdateParams.Name"/>)
    /// and/or replace the tags (<see cref="HashtagSetUpdateParams.Hashtags"/>
    /// replaces the FULL list).
    /// </summary>
    public Task<JsonElement?> UpdateAsync(string id, HashtagSetUpdateParams parameters, CancellationToken cancellationToken = default)
        => _client.PatchAsync($"/hashtag-sets/{Uri.EscapeDataString(id)}", parameters, cancellationToken);

    /// <summary><c>DELETE /hashtag-sets/:id</c>: delete a hashtag set. Resolves to null (204).</summary>
    public Task<JsonElement?> DeleteAsync(string id, CancellationToken cancellationToken = default)
        => _client.DeleteAsync($"/hashtag-sets/{Uri.EscapeDataString(id)}", cancellationToken);
}
