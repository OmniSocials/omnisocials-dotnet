using System.Text.Json;

namespace OmniSocials;

/// <summary>
/// Feature requests to the OmniSocials team, for the person who owns the key (their name and
/// email are stored with it so the team can answer). The person sees them in OmniSocials
/// (Settings, Request a feature) and gets an email when the status changes. No scope needed.
/// </summary>
/// <remarks>
/// Error codes: <c>invalid_summary</c>, <c>invalid_title</c>, <c>invalid_platform</c>,
/// <c>invalid_area</c>, <c>unknown_limit</c>, <c>invalid_id</c> (400); <c>not_found</c> (404);
/// <c>not_requestable</c> (409: the <c>limit_id</c> is a platform rule or a choice of
/// OmniSocials, the entry is in <c>error.limit</c> of <see cref="ApiException.Body"/> and
/// nothing was stored); <c>too_many_requests</c> (429, 10 an hour per person). Optional typed
/// models: <see cref="FeatureRequestCreateResponse"/>, <see cref="FeatureRequestListResponse"/>,
/// <see cref="FeatureRequestResponse"/>.
/// </remarks>
public sealed class FeatureRequestsResource
{
    private readonly OmniSocialsClient _client;

    internal FeatureRequestsResource(OmniSocialsClient client) => _client = client;

    /// <summary>
    /// <c>POST /feature-requests</c>: send a request. Send it only when the person asked for it.
    /// Returns <c>{ outcome, data }</c>: <c>outcome</c> is <c>created</c> (a new request),
    /// <c>voted</c> (a vote added to the organisation's open request for the same gap) or
    /// <c>already_requested</c> (this person already has an open request for it; <c>data</c> is
    /// that request).
    /// </summary>
    public Task<JsonElement?> CreateAsync(FeatureRequestCreateParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/feature-requests", parameters, cancellationToken);

    /// <summary><c>GET /feature-requests</c>: the person's own requests, newest first, at most 50.</summary>
    public Task<JsonElement?> ListAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync("/feature-requests", null, cancellationToken);

    /// <summary><c>GET /feature-requests/:id</c>: one request with its status and the team's replies.</summary>
    public Task<JsonElement?> GetAsync(long id, CancellationToken cancellationToken = default)
        => GetAsync(id.ToString(System.Globalization.CultureInfo.InvariantCulture), cancellationToken);

    /// <inheritdoc cref="GetAsync(long, CancellationToken)"/>
    public Task<JsonElement?> GetAsync(string id, CancellationToken cancellationToken = default)
        => _client.GetAsync($"/feature-requests/{Uri.EscapeDataString(id)}", null, cancellationToken);
}
