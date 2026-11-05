using System.Text.Json;

namespace OmniSocials;

/// <summary>
/// What OmniSocials cannot do, why, and what to do instead: platform rules, features not built
/// yet, features that work in the OmniSocials app only. Read it before you tell a person that
/// something is not possible, so you can give the reason (<c>says</c>) and the way around it
/// (<c>instead</c>). When the person still wants a <c>requestable</c> entry, send it with
/// <see cref="FeatureRequestsResource.CreateAsync"/> and its <c>limit_id</c>. No scope needed:
/// every key may call it.
/// </summary>
/// <remarks>
/// Error codes: <c>invalid_platform</c>, <c>invalid_area</c> (400). Optional typed model:
/// <see cref="LimitListResponse"/>.
/// </remarks>
public sealed class LimitsResource
{
    private readonly OmniSocialsClient _client;

    internal LimitsResource(OmniSocialsClient client) => _client = client;

    /// <summary>
    /// <c>GET /limits</c>: the entries, optionally for one platform or area. Returns
    /// <c>{ data: Limit[], kinds: { kind: label } }</c>.
    /// </summary>
    public Task<JsonElement?> ListAsync(LimitListParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var query = new List<KeyValuePair<string, string?>>
        {
            new("platform", parameters?.Platform),
            new("area", parameters?.Area),
        };
        return _client.GetAsync("/limits", query, cancellationToken);
    }
}
