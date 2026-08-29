using System.Globalization;
using System.Text.Json;

namespace OmniSocials;

/// <summary>
/// Location search for post tagging: Instagram (Facebook Places) and Threads
/// (Meta's Threads location catalog). The two sources use different ids: a
/// Facebook Place id is not a Threads location id.
/// </summary>
public sealed class LocationsResource
{
    private readonly OmniSocialsClient _client;

    internal LocationsResource(OmniSocialsClient client) => _client = client;

    /// <summary>
    /// <c>GET /locations/search?q=</c>: search Facebook Places for Instagram
    /// location tagging (the default source). Use a result's id as location_id
    /// on a post. For Threads locations, use the
    /// <see cref="SearchAsync(LocationSearchParams, CancellationToken)"/> overload.
    /// </summary>
    public Task<JsonElement?> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<KeyValuePair<string, string?>> { new("q", query) };
        return _client.GetAsync("/locations/search", queryParams, cancellationToken);
    }

    /// <summary>
    /// <c>GET /locations/search</c> with the full parameter set. Set
    /// <see cref="LocationSearchParams.Platform"/> to "threads" to search Meta's
    /// Threads location catalog instead of Facebook Places; pass either
    /// <see cref="LocationSearchParams.Q"/> or the
    /// <see cref="LocationSearchParams.Latitude"/> +
    /// <see cref="LocationSearchParams.Longitude"/> pair (coordinates are
    /// Threads only). The Threads response shape differs from Instagram's:
    /// <c>{ "locations": [ { id, name, address, city, country, latitude, longitude } ] }</c>
    /// (all fields but id nullable) or <c>{ "error": { code, message } }</c>
    /// where code is one of <c>not_available</c>, <c>threads_not_connected</c>,
    /// <c>threads_reauth_required</c> (the connection lacks the
    /// <c>threads_location_tagging</c> permission; reconnect Threads), or
    /// <c>platform_error</c>. Pass a Threads result's id as
    /// <c>threads.location_id</c> on post create/update. Threads location
    /// tagging is currently rolling out; until Meta approves the permissions it
    /// is disabled on production and calls return a clear error.
    /// </summary>
    public Task<JsonElement?> SearchAsync(LocationSearchParams parameters, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<KeyValuePair<string, string?>>
        {
            new("q", parameters.Q),
            new("platform", parameters.Platform),
            new("latitude", parameters.Latitude?.ToString(CultureInfo.InvariantCulture)),
            new("longitude", parameters.Longitude?.ToString(CultureInfo.InvariantCulture)),
        };
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
