using System.Text.Json;

namespace OmniSocials;

/// <summary>
/// "Create an ad" drafts (<c>client.Ads.Drafts</c>). A draft group holds one
/// campaign per network in <see cref="AdDraftParams.Platforms"/>, each with the
/// same ads. A draft never spends; the person checks it and starts it in the app
/// (<c>review_url</c>), or
/// <see cref="LaunchAsync(string, decimal, CancellationToken)"/> starts it with a
/// key that has the opt-in <c>ads:spend</c> scope.
/// </summary>
public sealed class AdDraftsResource
{
    private readonly OmniSocialsClient _client;

    internal AdDraftsResource(OmniSocialsClient client) => _client = client;

    private static string Draft(string groupId) => $"/ads/drafts/{Uri.EscapeDataString(groupId)}";

    /// <summary>
    /// <c>GET /ads/drafts/defaults</c>: suggested countries (where the Instagram
    /// followers are), the currency and the workspace's ad account per network.
    /// Requires <c>ads:read</c>.
    /// </summary>
    public Task<JsonElement?> DefaultsAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/drafts/defaults", null, cancellationToken);

    /// <summary>
    /// <c>POST /ads/drafts</c>: save a new ad as a draft group. It needs no ad
    /// account and spends nothing. Read <see cref="AdsResource.CapabilitiesAsync"/>
    /// first for what each network accepts. The answer carries <c>review_url</c>
    /// (the builder in the dashboard, where the person checks and starts the ad:
    /// hand it over) and per network <c>readiness</c> (see
    /// <see cref="AdDraftResponse"/>). A <c>budget.currency</c> other than the
    /// attached ad account's currency answers <c>400 currency_mismatch</c> with
    /// <c>param: "budget.currency"</c>. Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> CreateAsync(AdDraftParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/drafts", parameters, cancellationToken);

    /// <summary><c>GET /ads/drafts/:group_id</c>: one draft group. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> GetAsync(string groupId, CancellationToken cancellationToken = default)
        => _client.GetAsync(Draft(groupId), null, cancellationToken);

    /// <summary>
    /// <c>PUT /ads/drafts/:group_id</c>: replace the draft with the full body.
    /// Networks that already started are left alone (<c>campaigns[].editable</c>).
    /// A <c>budget.currency</c> other than the attached ad account's currency
    /// answers <c>400 currency_mismatch</c> with <c>param: "budget.currency"</c>.
    /// Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> UpdateAsync(string groupId, AdDraftParams parameters, CancellationToken cancellationToken = default)
        => _client.PutAsync(Draft(groupId), parameters, cancellationToken);

    /// <summary>
    /// <c>DELETE /ads/drafts/:group_id</c>: delete a draft group that never
    /// started. Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> DeleteAsync(string groupId, CancellationToken cancellationToken = default)
        => _client.DeleteAsync(Draft(groupId), cancellationToken);

    /// <summary>
    /// <c>POST /ads/drafts/:group_id/account</c>: set the ad account of one
    /// network in the draft (an account from <see cref="AdsResource.ListAccountsAsync"/>,
    /// same network). Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> AttachAccountAsync(string groupId, string platform, long adAccountId, CancellationToken cancellationToken = default)
        => _client.PostAsync(
            $"{Draft(groupId)}/account",
            new { platform, ad_account_id = adAccountId },
            cancellationToken);

    /// <summary>
    /// <c>POST /ads/drafts/:group_id/launch</c>: START REAL AD SPEND for every
    /// network of the draft that never started. Needs a key with the opt-in
    /// <c>ads:spend</c> scope ("Start ads"): without it the call answers
    /// <c>403 start_in_app</c> with the draft's page (its <c>review_url</c>); a
    /// network in <c>api_draft_only</c> (Google) is skipped with code
    /// <c>start_in_app</c>. Show the person the daily amount per network and get
    /// their yes first; <paramref name="confirmBudgetAmount"/> must equal the
    /// daily amount of every network. A wrong amount answers
    /// <c>400 budget_confirmation_required</c> and writes nothing. Per network
    /// results (see <see cref="AdDraftLaunchResponse"/>); one failed network
    /// never stops the others. Not retried after a 5xx, a connection error or a
    /// timeout: read the draft with <see cref="GetAsync"/> to see what started.
    /// Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> LaunchAsync(string groupId, decimal confirmBudgetAmount, CancellationToken cancellationToken = default)
        => LaunchAsync(groupId, new AdDraftLaunchParams { ConfirmBudgetAmount = confirmBudgetAmount }, cancellationToken);

    /// <summary>
    /// <c>POST /ads/drafts/:group_id/launch</c>: START REAL AD SPEND, with the
    /// confirmation as <see cref="AdDraftLaunchParams.ConfirmBudgetAmount"/> (one
    /// daily amount for every network) or
    /// <see cref="AdDraftLaunchParams.ConfirmBudgetAmounts"/> (per campaign id).
    /// Needs a key with <c>ads:spend</c>: without it the call answers
    /// <c>403 start_in_app</c> with the draft's page; a network in
    /// <c>api_draft_only</c> (Google) is skipped with code <c>start_in_app</c>.
    /// Show the person the amounts and get their yes first. Not retried after a
    /// 5xx, a connection error or a timeout: read the draft with
    /// <see cref="GetAsync"/> to see what started. Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> LaunchAsync(string groupId, AdDraftLaunchParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync($"{Draft(groupId)}/launch", parameters, RetryPolicy.RateLimitOnly, cancellationToken);
}
