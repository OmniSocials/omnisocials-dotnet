using System.Text.Json;

namespace OmniSocials;

/// <summary>Connected social accounts.</summary>
public sealed class AccountsResource
{
    private readonly OmniSocialsClient _client;

    internal AccountsResource(OmniSocialsClient client) => _client = client;

    /// <summary><c>GET /accounts</c>: the workspace's connected social accounts.</summary>
    public Task<JsonElement?> ListAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync("/accounts", null, cancellationToken);

    /// <summary>
    /// <c>GET /accounts/:id</c>: a single connected account. An X account also
    /// carries <c>x_statistics</c> (typed model <see cref="AccountXStatistics"/>).
    /// </summary>
    public Task<JsonElement?> GetAsync(string id, CancellationToken cancellationToken = default)
        => _client.GetAsync($"/accounts/{Uri.EscapeDataString(id)}", null, cancellationToken);

    /// <summary>
    /// <c>PATCH /accounts/:id</c>: change how often the X statistics of an X
    /// account are read. Only the properties you set on
    /// <see cref="AccountUpdateParams.XStatistics"/> are sent; a null
    /// <c>XStatistics</c> is sent as <c>null</c> (OmniSocials Connect keys: follow
    /// the customer's setting again). X bills every read and OmniSocials passes
    /// X's price on as credits (half a credit per post check, plus half for a
    /// video or GIF, 1 credit per follower count). Needs the opt-in scope
    /// <c>accounts:write</c> and billing rights (manage_billing) for the key's
    /// creator. Returns the account with its new <c>x_statistics</c>. Errors:
    /// <c>400 validation_error</c> / <c>schedule_invalid</c> /
    /// <c>not_supported</c> (not an X account), <c>403 forbidden</c>,
    /// <c>404 not_found</c>.
    /// </summary>
    public Task<JsonElement?> UpdateAsync(string id, AccountUpdateParams parameters, CancellationToken cancellationToken = default)
        => _client.PatchAsync($"/accounts/{Uri.EscapeDataString(id)}", parameters ?? new AccountUpdateParams(), cancellationToken);
}
