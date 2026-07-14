using System.Text.Json;

namespace OmniSocials;

/// <summary>Instagram location tagging (Facebook Places).</summary>
public sealed class LocationsResource
{
    private readonly OmniSocialsClient _client;

    internal LocationsResource(OmniSocialsClient client) => _client = client;

    /// <summary>
    /// <c>GET /locations/search?q=</c>: search Facebook Places for Instagram
    /// location tagging. Use a result's id as location_id on a post.
    /// </summary>
    public Task<JsonElement?> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<KeyValuePair<string, string?>> { new("q", query) };
        return _client.GetAsync("/locations/search", queryParams, cancellationToken);
    }

    /// <summary>
    /// <c>GET /locations/validate?id=</c>: check whether a Facebook Place id is a
    /// valid Instagram location before using it as location_id.
    /// </summary>
    public Task<JsonElement?> ValidateAsync(string id, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<KeyValuePair<string, string?>> { new("id", id) };
        return _client.GetAsync("/locations/validate", queryParams, cancellationToken);
    }
}
