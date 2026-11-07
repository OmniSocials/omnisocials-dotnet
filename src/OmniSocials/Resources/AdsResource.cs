using System.Globalization;
using System.Text.Json;

namespace OmniSocials;

/// <summary>
/// Ads on Meta, Pinterest, TikTok, LinkedIn, X and Google; "Create an ad"
/// drafts live under <see cref="Drafts"/>.
/// <para>
/// Ads spend real money once a campaign starts. Boosts and drafts are created as
/// drafts (no spend). Starting or raising spend needs a key with the opt-in
/// <c>ads:spend</c> scope ("Start ads", a second switch under "Ads"; its creator
/// needs <c>manage_ads</c>): <see cref="ActivateAsync"/>,
/// <see cref="ActivateMultiAsync"/>, <see cref="ResumeAsync"/>,
/// <see cref="AdDraftsResource.LaunchAsync(string, decimal, CancellationToken)"/>,
/// a budget raise or any change of a campaign that reached the network through
/// <see cref="UpdateAsync"/>, and the parity calls that switch something on or
/// raise a budget: <see cref="BulkStatusAsync"/> (resuming),
/// <see cref="AdCampaignAdsResource.SetStatusAsync"/> (switching on),
/// <see cref="AdCampaignAdsResource.AddAsync"/> on a running campaign,
/// <see cref="AdObjectsResource.UpdateAsync"/>,
/// <see cref="AdObjectsResource.SetStatusAsync"/> (switching on),
/// <see cref="AdRulesResource.CreateAsync"/>,
/// <see cref="AdRulesResource.UpdateAsync"/> and
/// <see cref="AdRulesResource.UndoAsync"/> (a rule or undo that switches on or
/// raises), <see cref="AdAccountsResource.UpdateSettingsAsync"/> (a spend cap
/// change), <see cref="AdReachFrequencyResource.ReserveAsync"/>,
/// <see cref="AdStudiesResource.CreateAsync"/> and
/// <see cref="AdBudgetSchedulesResource.CreateAsync"/>. Each repeats the amount.
/// Without <c>ads:spend</c> these calls answer <c>403 start_in_app</c> with the
/// page in the app where a person does it (rules: <c>403 rule_raises_spend</c>);
/// on Google (<c>api_draft_only</c> of <see cref="CapabilitiesAsync"/>) they
/// always do, also with <c>ads:spend</c>. <see cref="PauseAsync"/> and
/// <see cref="DeleteAsync"/> stay open, except on the networks in
/// <c>api_stop_in_app</c> (Google).
/// </para>
/// <para>
/// Take the amount from the person's explicit yes; never invent or confirm it
/// yourself. The calls that start or raise spend are retried on a 429 only,
/// never after a 5xx, a connection error or a timeout: the first attempt may
/// have started the ad. Pausing never needs an amount. Ads errors may carry
/// <c>param</c> (the field at fault): read it from the exception's <c>Body</c>
/// (<c>error.param</c>).
/// </para>
/// <para>
/// Budgets are in major units (10 = 10 EUR) in the ad account's currency. Ad
/// accounts are connected and picked in the OmniSocials dashboard. The scopes
/// <c>ads:read</c> and <c>ads:write</c> are opt-in on the API key (the "Ads"
/// switch); a key without them gets <c>403 insufficient_scope</c>, and every
/// write also needs <c>manage_ads</c> for the person behind the key. A network
/// without a feature answers <c>400 not_supported_on_network</c>
/// (<see cref="AdCapabilities.Features"/>). Optional typed models:
/// <see cref="AdCampaignResponse"/>, <see cref="AdDraftResponse"/>,
/// <see cref="AdDraftLaunchResponse"/>, <see cref="AdCapabilities"/>,
/// <see cref="AdTreeNode"/> and the rest in <c>AdsModels.cs</c>.
/// </para>
/// </summary>
public sealed class AdsResource
{
    private readonly OmniSocialsClient _client;

    internal AdsResource(OmniSocialsClient client)
    {
        _client = client;
        Drafts = new AdDraftsResource(client);
        Objects = new AdObjectsResource(client);
        Targeting = new AdTargetingResource(client);
        Audiences = new AdAudiencesResource(client);
        CampaignAds = new AdCampaignAdsResource(client);
        LeadForms = new AdLeadFormsResource(client);
        Leads = new AdLeadsResource(client);
        Conversions = new AdConversionsResource(client);
        Insights = new AdInsightsResource(client);
        Reports = new AdReportsResource(client);
        Rules = new AdRulesResource(client);
        Results = new AdResultsResource(client);
        Media = new AdMediaResource(client);
        Previews = new AdPreviewsResource(client);
        Partnership = new AdPartnershipResource(client);
        Comments = new AdCommentsResource(client);
        Accounts = new AdAccountsResource(client);
        ReachFrequency = new AdReachFrequencyResource(client);
        Studies = new AdStudiesResource(client);
        BudgetSchedules = new AdBudgetSchedulesResource(client);
        Google = new AdGoogleResource(client);
    }

    /// <summary>"Create an ad" drafts: one campaign per network, each with the same ads.</summary>
    public AdDraftsResource Drafts { get; }

    /// <summary>Campaigns, ad sets and ads as the network has them, and the import of campaigns made outside OmniSocials.</summary>
    public AdObjectsResource Objects { get; }

    /// <summary>Targeting search, browse, reach estimate and bid suggestions.</summary>
    public AdTargetingResource Targeting { get; }

    /// <summary>Custom audiences: customer lists, lookalikes, company lists.</summary>
    public AdAudiencesResource Audiences { get; }

    /// <summary>The ads of an OmniSocials campaign: list, add, read, status, review.</summary>
    public AdCampaignAdsResource CampaignAds { get; }

    /// <summary>Lead forms.</summary>
    public AdLeadFormsResource LeadForms { get; }

    /// <summary>The leads the lead forms bring.</summary>
    public AdLeadsResource Leads { get; }

    /// <summary>Pixels, conversion goals, custom conversions and server events.</summary>
    public AdConversionsResource Conversions { get; }

    /// <summary>Live numbers, campaign and ad numbers, timeline, rankings and alerts.</summary>
    public AdInsightsResource Insights { get; }

    /// <summary>Long reports that run in the background.</summary>
    public AdReportsResource Reports { get; }

    /// <summary>Automated rules.</summary>
    public AdRulesResource Rules { get; }

    /// <summary>Your own numbers per campaign and day.</summary>
    public AdResultsResource Results { get; }

    /// <summary>The network's own image and video library.</summary>
    public AdMediaResource Media { get; }

    /// <summary>Previews drawn by the network.</summary>
    public AdPreviewsResource Previews { get; }

    /// <summary>Partnership ads with creators (Meta).</summary>
    public AdPartnershipResource Partnership { get; }

    /// <summary>Comments on ads.</summary>
    public AdCommentsResource Comments { get; }

    /// <summary>One ad account: health, finance, settings, people, activity, identities.</summary>
    public AdAccountsResource Accounts { get; }

    /// <summary>Reach and frequency buying (Meta).</summary>
    public AdReachFrequencyResource ReachFrequency { get; }

    /// <summary>A/B tests and lift studies (Meta).</summary>
    public AdStudiesResource Studies { get; }

    /// <summary>Planned budget increases (Meta).</summary>
    public AdBudgetSchedulesResource BudgetSchedules { get; }

    /// <summary>Google Ads reports.</summary>
    public AdGoogleResource Google { get; }

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
    /// (goals, media, text limits, buttons, minimum daily budget, and per network
    /// <c>bid_strategies</c>, <c>settings</c> and <c>ad_fields</c>),
    /// <c>create_networks</c>, <c>network_states</c> ("live", "preview" or
    /// "coming_soon"), <c>features</c> with <c>feature_labels</c>,
    /// <c>targeting_types</c>, <c>api_goals</c>, <c>api_draft_only</c> (networks
    /// where every API caller stops at the draft, also a key with
    /// <c>ads:spend</c>: Google) and <c>api_stop_in_app</c> (networks where the
    /// API may not even pause or delete a campaign that reached the network:
    /// Google) (see <see cref="AdCapabilities"/>). Requires <c>ads:read</c>.
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
    /// <c>POST /ads/boost</c>: boost a published post. Creates a DRAFT boost that
    /// spends nothing; the person starts it in the app. TikTok needs
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
    /// <c>PATCH /ads/campaigns/:id</c>: change the name, budget, targeting or
    /// schedule of a draft. A campaign that reached the network changes only with
    /// a key that has the opt-in <c>ads:spend</c> scope ("Start ads"); without
    /// it, and on Google (<c>api_draft_only</c>) always, the call answers
    /// <c>403 start_in_app</c> with the page where a person does it in the app.
    /// <see cref="AdCampaignUpdateParams.ConfirmBudgetAmount"/> (the new amount
    /// again, after the person agreed) is for a budget raise of a running
    /// campaign. Not retried after a 5xx, a connection error or a timeout: read
    /// the campaign with <see cref="GetAsync"/> to see what happened. Requires
    /// <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> UpdateAsync(long id, AdCampaignUpdateParams parameters, CancellationToken cancellationToken = default)
        => _client.PatchAsync(Campaign(id), parameters, RetryPolicy.RateLimitOnly, cancellationToken);

    /// <summary>
    /// <c>POST /ads/campaigns/:id/activate</c>: START REAL AD SPEND. Needs a key
    /// with the opt-in <c>ads:spend</c> scope ("Start ads"); without it, and on
    /// Google (<c>api_draft_only</c>) always, the call answers
    /// <c>403 start_in_app</c> with the page where a person does it in the app.
    /// Show the person the budget (amount, currency, daily or lifetime) and get
    /// their yes first; <paramref name="confirmBudgetAmount"/> must equal the
    /// campaign's budget amount. A campaign of a draft group answers
    /// <c>409 use_draft_launch</c>: use
    /// <see cref="AdDraftsResource.LaunchAsync(string, decimal, CancellationToken)"/>.
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
    /// campaigns. Needs a key with the opt-in <c>ads:spend</c> scope ("Start
    /// ads"); without it, and on Google (<c>api_draft_only</c>) always, a result
    /// has <c>ok: false</c> and code <c>start_in_app</c> with the page where a
    /// person starts it in the app. Every campaign's budget must equal
    /// <paramref name="confirmBudgetAmount"/>; show the person the total (amount
    /// x campaigns) and get their yes first. Answers <c>{ results }</c> (see
    /// <see cref="AdMultiResponse"/>). Not retried after a 5xx, a connection
    /// error or a timeout: read the campaigns with <see cref="GetAsync"/> to see
    /// what happened. Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> ActivateMultiAsync(IEnumerable<long> campaignIds, decimal confirmBudgetAmount, CancellationToken cancellationToken = default)
        => _client.PostAsync(
            "/ads/campaigns/activate-multi",
            new { campaign_ids = campaignIds.ToArray(), confirm_budget_amount = confirmBudgetAmount },
            RetryPolicy.RateLimitOnly,
            cancellationToken);

    /// <summary>
    /// <c>POST /ads/campaigns/:id/pause</c>: pause a campaign (also while in
    /// review). Open through the API, except on the networks in
    /// <c>api_stop_in_app</c> (Google): there a campaign that reached the network
    /// answers <c>403 start_in_app</c>. Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> PauseAsync(long id, CancellationToken cancellationToken = default)
        => _client.PostAsync($"{Campaign(id)}/pause", null, cancellationToken);

    /// <summary>
    /// <c>POST /ads/campaigns/:id/resume</c>: resume a paused campaign. It STARTS
    /// REAL AD SPEND again. Needs a key with the opt-in <c>ads:spend</c> scope
    /// ("Start ads"); without it, and on Google (<c>api_draft_only</c>) always,
    /// the call answers <c>403 start_in_app</c> with the page where a person does
    /// it in the app. Only a paused campaign resumes (<c>409 not_paused</c>). Not
    /// retried after a 5xx, a connection error or a timeout: read the campaign
    /// with <see cref="GetAsync"/> to see what happened. Requires
    /// <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> ResumeAsync(long id, CancellationToken cancellationToken = default)
        => _client.PostAsync($"{Campaign(id)}/resume", null, RetryPolicy.RateLimitOnly, cancellationToken);

    /// <summary>
    /// <c>DELETE /ads/campaigns/:id</c>: delete a campaign on the network too. A
    /// campaign that spent is archived: its spend stays in the totals. Open
    /// through the API, except a campaign that reached the network on a network
    /// in <c>api_stop_in_app</c> (Google): <c>403 start_in_app</c>. Requires
    /// <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> DeleteAsync(long id, CancellationToken cancellationToken = default)
        => _client.DeleteAsync(Campaign(id), cancellationToken);

    /// <summary>
    /// <c>GET /ads/overview</c>: connected accounts, campaigns per status and the
    /// totals (see <see cref="AdsOverview"/>). <c>totals.spend</c> is null when
    /// the campaigns use more than one currency: read <c>totals_by_currency</c>
    /// then. Requires <c>ads:read</c>.
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

    // ─── Ads API parity: campaigns and their ads ─────────────────────────────

    /// <summary>
    /// <c>POST /ads/campaigns/bulk-status</c>: pause ("paused") or resume
    /// ("active") up to 50 OmniSocials campaigns. Resuming STARTS REAL AD SPEND
    /// again: every campaign's budget must equal
    /// <paramref name="confirmBudgetAmount"/> (one amount for all, checked before
    /// any change); show the person the total and get their yes first. Resuming
    /// needs a key with <c>ads:spend</c>: without it, and on Google always, a
    /// result answers <c>start_in_app</c>. Pausing needs no amount (Google:
    /// <c>start_in_app</c>). Answers <c>results</c> per campaign. Not retried
    /// after a 5xx, a connection error or a timeout: read the campaigns with
    /// <see cref="GetAsync"/> to see what happened. Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> BulkStatusAsync(IEnumerable<long> campaignIds, string status, decimal? confirmBudgetAmount = null, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object?> { ["campaign_ids"] = campaignIds.ToArray(), ["status"] = status };
        if (confirmBudgetAmount.HasValue) body["confirm_budget_amount"] = confirmBudgetAmount.Value;
        return _client.PostAsync("/ads/campaigns/bulk-status", body, RetryPolicy.RateLimitOnly, cancellationToken);
    }

    /// <summary>
    /// <c>POST /ads/campaigns/:id/duplicate</c>: copy an OmniSocials campaign as a
    /// new DRAFT (same ad account, goal, budget, targeting, settings and ads;
    /// optional <paramref name="name"/>). A draft never spends. Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> DuplicateAsync(long id, string? name = null, CancellationToken cancellationToken = default)
        => _client.PostAsync($"{Campaign(id)}/duplicate", name is null ? null : new { name }, cancellationToken);
}
