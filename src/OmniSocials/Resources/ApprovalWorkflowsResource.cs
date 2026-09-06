using System.Text.Json;

namespace OmniSocials;

/// <summary>
/// Approval workflows are configured in the OmniSocials dashboard (Approvals).
/// List them here and route a post through one at create time via
/// <see cref="PostCreateParams.ApprovalWorkflowId"/>.
/// </summary>
public sealed class ApprovalWorkflowsResource
{
    private readonly OmniSocialsClient _client;

    internal ApprovalWorkflowsResource(OmniSocialsClient client) => _client = client;

    /// <summary>
    /// <c>GET /approval-workflows</c>: the workflows this workspace can use
    /// (company-wide plus workspace-bound), with steps and named approvers.
    /// </summary>
    public Task<JsonElement?> ListAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync("/approval-workflows", null, cancellationToken);
}
