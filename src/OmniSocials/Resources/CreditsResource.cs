using System.Text.Json;

namespace OmniSocials;

/// <summary>
/// The organisation's credit balance and what X statistics cost. Top-ups are
/// made in the OmniSocials app (Settings -> Organisation -> Billing -> Credits).
/// </summary>
public sealed class CreditsResource
{
    private readonly OmniSocialsClient _client;

    internal CreditsResource(OmniSocialsClient client) => _client = client;

    /// <summary>
    /// <c>GET /credits</c>: the balance (1 credit = EUR 0.01), <c>auto_refill</c>,
    /// <c>this_month</c> (credits used per reason) and <c>x_statistics</c> (state,
    /// the date charging starts, the prices per read, this month's reads, and each
    /// X account with its setting and its estimate for a month). Needs the opt-in
    /// scope <c>credits:read</c> and billing rights (manage_billing) for the key's
    /// creator; OmniSocials Connect keys get <c>403 not_available_for_connect</c>.
    /// Typed model: <see cref="CreditsResponse"/>.
    /// </summary>
    public Task<JsonElement?> GetAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync("/credits", null, cancellationToken);
}
