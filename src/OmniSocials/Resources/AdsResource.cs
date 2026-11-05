using System.Globalization;
using System.Text.Json;

namespace OmniSocials;

/// <summary>
/// Ads on Meta, Pinterest, TikTok, LinkedIn, X and Google; "Create an ad"
/// drafts live under <see cref="Drafts"/>.
/// <para>
/// Ads spend real money once a campaign starts. Boosts, campaigns and drafts
/// are created as drafts (no spend). <see cref="ActivateAsync"/>,
/// <see cref="ActivateMultiAsync"/>, <see cref="ResumeAsync"/>,
/// <see cref="AdDraftsResource.LaunchAsync(string, decimal, CancellationToken)"/>
/// and a budget raise through <see cref="UpdateAsync"/> start or raise spend;
/// the activations and the launch take the amount again as a confirmation.
/// These calls are retried on a 429 only, never after a 5xx, a connection
/// error or a timeout: the first attempt may have started the ad.
/// </para>
/// <para>
/// Budgets are in major units (10 = 10 EUR) in the ad account's currency. Ad
/// accounts are connected and picked in the OmniSocials dashboard. The scopes
/// <c>ads:read</c> and <c>ads:write</c> are opt-in on the API key (the "Ads"
/// switch); a key without them gets <c>403 insufficient_scope</c>. Optional
/// typed models: <see cref="AdCampaignResponse"/>, <see cref="AdDraftResponse"/>,
/// <see cref="AdDraftLaunchResponse"/> and the rest in <c>AdsModels.cs</c>.
/// </para>
/// </summary>
public sealed class AdsResource
{
    private readonly OmniSocialsClient _client;

    internal AdsResource(OmniSocialsClient client)
    {
        _client = client;
        Drafts = new AdDraftsResource(client);
    }

    /// <summary>"Create an ad" drafts: one campaign per network, each with the same ads.</summary>
    public AdDraftsResource Drafts { get; }

    private static string Campaign(long id)
        => $"/ads/campaigns/{Uri.EscapeDataString(id.ToString(CultureInfo.InvariantCulture))}";

    /// <summary>
    /// <c>GET /ads/accounts</c>: the connected ad accounts, plus
    /// <c>platforms_available</c> (see <see cref="AdAccountsResponse"/>).
    /// Accounts are connected and picked in the dashboard; only
    /// <c>is_selected</c> accounts can run campaigns. Requires <c>ads:read</c>
    /// (opt-in on the API key).
    /// </summary>
    public Task<JsonElement?> ListAccountsAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/accounts", null, cancellationToken);

    /// <summary>
    /// <c>GET /ads/capabilities</c>: what each network accepts for a new ad
    /// (goals, media, text limits, buttons, minimum daily budget),
    /// <c>create_networks</c> and <c>network_states</c> ("live", "preview" or
    /// "coming_soon"). Requires <c>ads:read</c>.
    /// </summary>
    public Task<JsonElement?> CapabilitiesAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/capabilities", null, cancellationToken);

    /// <summary>
    /// <c>GET /ads/campaigns</c>: list campaigns and boosts, optionally by
    /// status, platform or campaign type (see <see cref="AdCampaignsResponse"/>).
    /// Requires <c>ads:read</c>.
    /// </summary>
    public Task<JsonElement?> ListAsync(AdCampaignListParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var query = new List<KeyValuePair<string, string?>>
        {
            new("status", parameters?.Status),
            new("platform", parameters?.Platform),
            new("campaign_type", parameters?.CampaignType),
            new("limit", parameters?.Limit?.ToString(CultureInfo.InvariantCulture)),
            new("offset", parameters?.Offset?.ToString(CultureInfo.InvariantCulture)),
        };
        return _client.GetAsync("/ads/campaigns", query, cancellationToken);
    }

    /// <summary><c>GET /ads/campaigns/:id</c>: one campaign with its latest numbers. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> GetAsync(long id, CancellationToken cancellationToken = default)
        => _client.GetAsync(Campaign(id), null, cancellationToken);

    /// <summary>
    /// <c>POST /ads/boost</c>: boost a published post. Creates a DRAFT boost: no
    /// spend until <see cref="ActivateAsync"/>. TikTok needs
    /// <c>Creative["spark_auth_code"]</c>, Google needs <c>headlines</c>,
    /// <c>descriptions</c> and <c>logo_url</c>. Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> BoostAsync(AdBoostParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/boost", parameters, cancellationToken);

    /// <summary>
    /// <c>POST /ads/boost-multi</c>: one DRAFT boost per ad account (up to 10),
    /// each with the same budget, so the total is amount x accounts. Answers
    /// <c>{ results }</c> (see <see cref="AdMultiResponse"/>); one failed account
    /// never stops the others. Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> BoostMultiAsync(AdBoostMultiParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/boost-multi", parameters, cancellationToken);

    /// <summary>
    /// <c>POST /ads/campaigns</c>: a DRAFT campaign without ads. It cannot start
    /// (<c>campaign_has_no_ads</c>): use <see cref="AdDraftsResource.CreateAsync"/>
    /// for a new ad. Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> CreateAsync(AdCampaignCreateParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/campaigns", parameters, cancellationToken);

    /// <summary>
    /// <c>PATCH /ads/campaigns/:id</c>: change the name, budget, targeting or
    /// schedule. A budget raise can START REAL AD SPEND: to raise the budget of a
    /// running campaign, show the person the new amount, get their yes first,
    /// and pass it again as <see cref="AdCampaignUpdateParams.ConfirmBudgetAmount"/>.
    /// Not retried after a 5xx, a connection error or a timeout: read the
    /// campaign with <see cref="GetAsync"/> to see what happened. Requires
    /// <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> UpdateAsync(long id, AdCampaignUpdateParams parameters, CancellationToken cancellationToken = default)
        => _client.PatchAsync(Campaign(id), parameters, RetryPolicy.RateLimitOnly, cancellationToken);

    /// <summary>
    /// <c>POST /ads/campaigns/:id/activate</c>: START REAL AD SPEND. Show the
    /// person the budget (amount, currency, daily or lifetime) and get their yes
    /// first; <paramref name="confirmBudgetAmount"/> must equal the campaign's
    /// budget amount. A campaign of a draft group answers
    /// <c>409 use_draft_launch</c>: use <see cref="AdDraftsResource.LaunchAsync(string, decimal, CancellationToken)"/>.
    /// Not retried after a 5xx, a connection error or a timeout: read the
    /// campaign with <see cref="GetAsync"/> to see what happened. Requires
    /// <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> ActivateAsync(long id, decimal confirmBudgetAmount, CancellationToken cancellationToken = default)
        => _client.PostAsync(
            $"{Campaign(id)}/activate",
            new { confirm_budget_amount = confirmBudgetAmount },
            RetryPolicy.RateLimitOnly,
            cancellationToken);

    /// <summary>
    /// <c>POST /ads/campaigns/activate-multi</c>: START REAL AD SPEND on up to 10
    /// campaigns. Every campaign's budget must equal
    /// <paramref name="confirmBudgetAmount"/>; show the person the total
    /// (amount x campaigns) and get their yes first. Answers <c>{ results }</c>
    /// (see <see cref="AdMultiResponse"/>). Not retried after a 5xx, a
    /// connection error or a timeout: read the campaigns with
    /// <see cref="GetAsync"/> to see what happened. Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> ActivateMultiAsync(IEnumerable<long> campaignIds, decimal confirmBudgetAmount, CancellationToken cancellationToken = default)
        => _client.PostAsync(
            "/ads/campaigns/activate-multi",
            new { campaign_ids = campaignIds.ToArray(), confirm_budget_amount = confirmBudgetAmount },
            RetryPolicy.RateLimitOnly,
            cancellationToken);

    /// <summary><c>POST /ads/campaigns/:id/pause</c>: pause a campaign (also while in review). Requires <c>ads:write</c>.</summary>
    public Task<JsonElement?> PauseAsync(long id, CancellationToken cancellationToken = default)
        => _client.PostAsync($"{Campaign(id)}/pause", null, cancellationToken);

    /// <summary>
    /// <c>POST /ads/campaigns/:id/resume</c>: resume a paused campaign. It STARTS
    /// REAL AD SPEND again: show the person the budget and get their yes first.
    /// Not retried after a 5xx, a connection error or a timeout: read the
    /// campaign with <see cref="GetAsync"/> to see what happened. Requires
    /// <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> ResumeAsync(long id, CancellationToken cancellationToken = default)
        => _client.PostAsync($"{Campaign(id)}/resume", null, RetryPolicy.RateLimitOnly, cancellationToken);

    /// <summary>
    /// <c>DELETE /ads/campaigns/:id</c>: delete a campaign on the network too. A
    /// campaign that spent is archived: its spend stays in the totals. Requires
    /// <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> DeleteAsync(long id, CancellationToken cancellationToken = default)
        => _client.DeleteAsync(Campaign(id), cancellationToken);

    /// <summary>
    /// <c>GET /ads/overview</c>: connected accounts, campaigns per status and
    /// total spend (see <see cref="AdsOverview"/>). Requires <c>ads:read</c>.
    /// </summary>
    public Task<JsonElement?> OverviewAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/overview", null, cancellationToken);

    /// <summary>
    /// <c>GET /ads/suggestions</c>: organic posts that do better than the
    /// workspace median, boost candidates (see <see cref="AdBoostSuggestion"/>).
    /// Read-only. Requires <c>ads:read</c>.
    /// </summary>
    public Task<JsonElement?> SuggestionsAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/suggestions", null, cancellationToken);

    /// <summary>
    /// <c>POST /ads/research/keywords</c>: keyword ideas from seed keywords or a
    /// URL, through the connected Google Ads account (see
    /// <see cref="AdKeywordResult"/>). Read-only and free. Requires <c>ads:read</c>.
    /// </summary>
    public Task<JsonElement?> ResearchKeywordsAsync(AdKeywordResearchParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/research/keywords", parameters, cancellationToken);

    /// <summary>
    /// <c>POST /ads/research/keywords/metrics</c>: search numbers for an exact
    /// keyword list. Read-only. Requires <c>ads:read</c>.
    /// </summary>
    public Task<JsonElement?> KeywordMetricsAsync(AdKeywordMetricsParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/research/keywords/metrics", parameters, cancellationToken);
}
