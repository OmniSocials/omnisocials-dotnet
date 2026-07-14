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

    /// <summary><c>GET /accounts/:id</c>: a single connected account.</summary>
    public Task<JsonElement?> GetAsync(string id, CancellationToken cancellationToken = default)
        => _client.GetAsync($"/accounts/{Uri.EscapeDataString(id)}", null, cancellationToken);
}
