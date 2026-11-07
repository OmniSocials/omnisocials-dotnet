using System.Globalization;
using System.Text.Json;

namespace OmniSocials;

// The Ads API parity groups (2026-11-05), reached from client.Ads. A thin
// passthrough: query params are a loose dictionary (a list becomes a comma list,
// nulls are left out) and bodies any object System.Text.Json writes (an anonymous
// object, a Dictionary<string, object?> or a params class). The API checks the
// values. A network without a feature answers 400 not_supported_on_network
// (client.Ads.CapabilitiesAsync lists "features"). A missing permission on the
// network side answers 403 network_permission_missing. Money moments (switching
// something on, a budget or spend cap change, a rule that can raise spend, a
// reach and frequency reservation, a study, a planned budget increase) and other
// changes of a campaign on the network need a key with the opt-in ads:spend scope
// ("Start ads"): without it, and on Google always, they answer 403 start_in_app
// with the page where a person does it in the app (rules: 403 rule_raises_spend).
// Pausing, deleting and archiving stay open, except on Google. With ads:spend a
// money moment answers 400 budget_confirmation_required until the person's own
// amount comes back as confirm_budget_amount (rules: confirm_max_daily_budget).
// Calls that can start or raise ad spend are retried on a 429 only, never after a
// 5xx, a connection error or a timeout.

internal static class AdsPaths
{
    internal static string Id(long id) => Uri.EscapeDataString(id.ToString(CultureInfo.InvariantCulture));

    internal static string Seg(string value) => Uri.EscapeDataString(value);

    internal static string Account(long accountId) => $"/ads/accounts/{Id(accountId)}";

    internal static string Campaign(long campaignId) => $"/ads/campaigns/{Id(campaignId)}";

    internal static string Ad(long campaignId, long adId) => $"{Campaign(campaignId)}/ads/{Id(adId)}";

    internal static IReadOnlyList<KeyValuePair<string, string?>> AccountQuery(long adAccountId)
        => new[] { new KeyValuePair<string, string?>("ad_account_id", adAccountId.ToString(CultureInfo.InvariantCulture)) };

    /// <summary><c>{ status, confirm_budget_amount? }</c> of the status calls.</summary>
    internal static Dictionary<string, object?> StatusBody(string status, decimal? confirmBudgetAmount)
    {
        var body = new Dictionary<string, object?> { ["status"] = status };
        if (confirmBudgetAmount.HasValue) body["confirm_budget_amount"] = confirmBudgetAmount.Value;
        return body;
    }
}

/// <summary>
/// The campaigns, ad sets and ads of an ad account as the network has them,
/// also the ones made outside OmniSocials, and the import of those campaigns
/// (<c>client.Ads.Objects</c>). <c>level</c> is "campaign", "ad_set" or "ad";
/// <c>externalId</c> is the network's own id. Objects of OmniSocials campaigns
/// answer <c>409 use_campaign_routes</c>: change those through <c>client.Ads</c>.
/// </summary>
public sealed class AdObjectsResource
{
    private readonly OmniSocialsClient _client;

    internal AdObjectsResource(OmniSocialsClient client) => _client = client;

    private static string Object(long accountId, string level, string externalId)
        => $"{AdsPaths.Account(accountId)}/objects/{AdsPaths.Seg(level)}/{AdsPaths.Seg(externalId)}";

    /// <summary>
    /// <c>GET /ads/accounts/:accountId/tree</c>: campaigns, ad sets and ads read
    /// live from the network with the numbers of the period. Query: <c>from</c>,
    /// <c>to</c> (at most 90 days, default the last 30), <c>status</c> (a list or
    /// a comma list), <c>limit</c> (up to 100), <c>cursor</c>. Each campaign says
    /// <c>source</c> ("omnisocials" or "external"; see <see cref="AdTreeNode"/>).
    /// Requires <c>ads:read</c>.
    /// </summary>
    public Task<JsonElement?> TreeAsync(long accountId, IReadOnlyDictionary<string, object?>? query = null, CancellationToken cancellationToken = default)
        => _client.GetAsync($"{AdsPaths.Account(accountId)}/tree", OmniSocialsClient.ToQuery(query), cancellationToken);

    /// <summary><c>GET /ads/accounts/:accountId/objects/:level/:externalId</c>: live details of one object. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> GetAsync(long accountId, string level, string externalId, CancellationToken cancellationToken = default)
        => _client.GetAsync(Object(accountId, level, externalId), null, cancellationToken);

    /// <summary>
    /// <c>PATCH /ads/accounts/:accountId/objects/:level/:externalId</c>: change
    /// the <c>name</c>, <c>budget</c> or <c>end_at</c> of a campaign or ad set
    /// made outside OmniSocials. A higher budget RAISES REAL AD SPEND: show the
    /// person the new amount and get their yes first, then pass it again as
    /// <c>confirm_budget_amount</c>. It changes the campaign on the network, so
    /// it needs a key with the opt-in <c>ads:spend</c> scope ("Start ads");
    /// without it, and on Google (<c>api_draft_only</c>) always, the call answers
    /// <c>403 start_in_app</c> with the page where a person does it in the app.
    /// Not retried after a 5xx, a connection error or a timeout: read the object
    /// with <see cref="GetAsync"/> to see what happened. Requires
    /// <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> UpdateAsync(long accountId, string level, string externalId, object parameters, CancellationToken cancellationToken = default)
        => _client.PatchAsync(Object(accountId, level, externalId), parameters, RetryPolicy.RateLimitOnly, cancellationToken);

    /// <summary>
    /// <c>PUT /ads/accounts/:accountId/objects/:level/:externalId/status</c>:
    /// pause ("paused") or switch on ("active") one campaign, ad set or ad.
    /// Switching on STARTS REAL AD SPEND: show the person the budget that pays
    /// (the object's own or its parent's) and get their yes first, then pass it
    /// as <paramref name="confirmBudgetAmount"/>. Switching on needs a key with
    /// the opt-in <c>ads:spend</c> scope ("Start ads"); without it, and on Google
    /// (<c>api_draft_only</c>) always, the call answers <c>403 start_in_app</c>
    /// with the page where a person does it in the app. Pausing needs no amount
    /// and stays open (Google: <c>start_in_app</c>). Not retried after a 5xx, a
    /// connection error or a timeout. Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> SetStatusAsync(long accountId, string level, string externalId, string status, decimal? confirmBudgetAmount = null, CancellationToken cancellationToken = default)
        => _client.PutAsync(
            $"{Object(accountId, level, externalId)}/status",
            AdsPaths.StatusBody(status, confirmBudgetAmount),
            RetryPolicy.RateLimitOnly,
            cancellationToken);

    /// <summary>
    /// <c>POST /ads/accounts/:accountId/objects/:level/:externalId/duplicate</c>:
    /// copy an object made outside OmniSocials; the copy is paused. Body:
    /// <c>deep</c> (default true), <c>rename</c>. Meta only for now. Needs a key
    /// with the opt-in <c>ads:spend</c> scope ("Start ads"); without it, and on
    /// Google (<c>api_draft_only</c>) always, the call answers
    /// <c>403 start_in_app</c> with the page where a person does it in the app.
    /// Copy an OmniSocials campaign as a draft with
    /// <see cref="AdsResource.DuplicateAsync"/>. Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> DuplicateAsync(long accountId, string level, string externalId, object? parameters = null, CancellationToken cancellationToken = default)
        => _client.PostAsync($"{Object(accountId, level, externalId)}/duplicate", parameters, cancellationToken);

    /// <summary>
    /// <c>GET /ads/accounts/:accountId/import</c>: the import state ("not_started",
    /// "pending", "done", "failed") and <c>campaigns_imported</c>. Requires <c>ads:read</c>.
    /// </summary>
    public Task<JsonElement?> ImportStatusAsync(long accountId, CancellationToken cancellationToken = default)
        => _client.GetAsync($"{AdsPaths.Account(accountId)}/import", null, cancellationToken);

    /// <summary>
    /// <c>POST /ads/accounts/:accountId/import</c>: read the ad account again at the
    /// next sync (within 15 minutes). The first import reads the last 90 days;
    /// webhook <c>ads.initial_sync_completed</c> when it is done. Answers 202.
    /// Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> StartImportAsync(long accountId, CancellationToken cancellationToken = default)
        => _client.PostAsync($"{AdsPaths.Account(accountId)}/import", null, cancellationToken);

    /// <summary>
    /// <c>GET /ads/external-campaigns</c>: imported campaigns, by <c>ad_account_id</c>,
    /// <c>platform</c> or <c>status</c>; <c>limit</c> up to 200 and <c>offset</c>.
    /// Requires <c>ads:read</c>.
    /// </summary>
    public Task<JsonElement?> ListExternalAsync(IReadOnlyDictionary<string, object?>? query = null, CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/external-campaigns", OmniSocialsClient.ToQuery(query), cancellationToken);

    /// <summary><c>GET /ads/external-campaigns/:id</c>: one imported campaign with its daily numbers (<c>from</c>, <c>to</c>). Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> GetExternalAsync(long id, IReadOnlyDictionary<string, object?>? query = null, CancellationToken cancellationToken = default)
        => _client.GetAsync($"/ads/external-campaigns/{AdsPaths.Id(id)}", OmniSocialsClient.ToQuery(query), cancellationToken);
}

/// <summary>
/// Targeting options of a network (<c>client.Ads.Targeting</c>). The ids go into
/// <see cref="AdDraftTargeting.Networks"/> of a draft, or <c>targeting.network</c>
/// of a campaign. All four calls are reads (<c>ads:read</c>).
/// </summary>
public sealed class AdTargetingResource
{
    private readonly OmniSocialsClient _client;

    internal AdTargetingResource(OmniSocialsClient client) => _client = client;

    /// <summary>
    /// <c>GET /ads/targeting/search</c>: interests, places, languages, job titles and
    /// more. Query: <c>ad_account_id</c>, <c>type</c>, <c>q</c>, optional <c>limit</c>
    /// (up to 100) and <c>country</c>. Answers <c>items</c> with <c>id</c>,
    /// <c>name</c>, <c>type</c>, <c>path</c> and the audience size range.
    /// </summary>
    public Task<JsonElement?> SearchAsync(IReadOnlyDictionary<string, object?> query, CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/targeting/search", OmniSocialsClient.ToQuery(query), cancellationToken);

    /// <summary><c>GET /ads/targeting/browse</c>: the categories of one <c>type</c> as a tree (<c>ad_account_id</c>, <c>type</c>).</summary>
    public Task<JsonElement?> BrowseAsync(IReadOnlyDictionary<string, object?> query, CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/targeting/browse", OmniSocialsClient.ToQuery(query), cancellationToken);

    /// <summary>
    /// <c>POST /ads/targeting/reach-estimate</c>: the audience size of a targeting
    /// (<c>ad_account_id</c>, <c>targeting</c> with countries, ages, genders and
    /// <c>network</c>, optional <c>objective</c>). Answers <c>lower</c>,
    /// <c>upper</c>, <c>daily</c>.
    /// </summary>
    public Task<JsonElement?> ReachEstimateAsync(object parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/targeting/reach-estimate", parameters, cancellationToken);

    /// <summary><c>POST /ads/targeting/bid-suggestions</c>: a suggested bid and daily budget. Spends nothing.</summary>
    public Task<JsonElement?> BidSuggestionsAsync(object parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/targeting/bid-suggestions", parameters, cancellationToken);
}

/// <summary>
/// Custom audiences of an ad account: customer lists, lookalikes and company
/// lists (<c>client.Ads.Audiences</c>). <c>audienceId</c> is the network's own id.
/// </summary>
public sealed class AdAudiencesResource
{
    private readonly OmniSocialsClient _client;

    internal AdAudiencesResource(OmniSocialsClient client) => _client = client;

    private static string Audience(string audienceId) => $"/ads/audiences/{AdsPaths.Seg(audienceId)}";

    /// <summary><c>GET /ads/audiences</c>: the audiences of <c>ad_account_id</c> (<c>cursor</c> for the next page). Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> ListAsync(IReadOnlyDictionary<string, object?> query, CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/audiences", OmniSocialsClient.ToQuery(query), cancellationToken);

    /// <summary>
    /// <c>POST /ads/audiences</c>: create an audience (<c>ad_account_id</c>,
    /// <c>name</c>, <c>description</c>, <c>kind</c>, <c>lookalike</c>,
    /// <c>customer_file_source</c> and the first members as <c>schema</c> +
    /// <c>rows</c>; see <see cref="AdAudienceCreateParams"/>). Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> CreateAsync(object parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/audiences", parameters, cancellationToken);

    /// <summary><c>GET /ads/audiences/:audienceId?ad_account_id=</c>: one audience. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> GetAsync(string audienceId, long adAccountId, CancellationToken cancellationToken = default)
        => _client.GetAsync(Audience(audienceId), AdsPaths.AccountQuery(adAccountId), cancellationToken);

    /// <summary><c>PATCH /ads/audiences/:audienceId</c>: rename (<c>ad_account_id</c>, <c>name</c>, <c>description</c>). Requires <c>ads:write</c>.</summary>
    public Task<JsonElement?> UpdateAsync(string audienceId, object parameters, CancellationToken cancellationToken = default)
        => _client.PatchAsync(Audience(audienceId), parameters, cancellationToken);

    /// <summary><c>DELETE /ads/audiences/:audienceId?ad_account_id=</c>: delete an audience. Requires <c>ads:write</c>.</summary>
    public Task<JsonElement?> DeleteAsync(string audienceId, long adAccountId, CancellationToken cancellationToken = default)
        => _client.DeleteAsync(Audience(audienceId), AdsPaths.AccountQuery(adAccountId), null, cancellationToken);

    /// <summary>
    /// <c>POST /ads/audiences/:audienceId/members</c>: add people to a customer list
    /// (see <see cref="AdAudienceMembersParams"/>, at most 10,000 rows). Send the
    /// raw values: OmniSocials normalizes and SHA-256 hashes them before they leave
    /// and stores nothing. Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> AddMembersAsync(string audienceId, object parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync($"{Audience(audienceId)}/members", parameters, cancellationToken);

    /// <summary><c>DELETE /ads/audiences/:audienceId/members</c>: remove people (the same body as <see cref="AddMembersAsync"/>). Requires <c>ads:write</c>.</summary>
    public Task<JsonElement?> RemoveMembersAsync(string audienceId, object parameters, CancellationToken cancellationToken = default)
        => _client.DeleteAsync($"{Audience(audienceId)}/members", null, parameters, cancellationToken);

    /// <summary>
    /// <c>POST /ads/audiences/:audienceId/companies</c>: add companies to a company
    /// list (LinkedIn): <c>ad_account_id</c> and <c>companies</c> (each <c>name</c>,
    /// <c>domain</c> or <c>linkedin_url</c>). Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> AddCompaniesAsync(string audienceId, object parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync($"{Audience(audienceId)}/companies", parameters, cancellationToken);
}

/// <summary>Lead forms of an ad account (<c>client.Ads.LeadForms</c>). Their leads are in <c>client.Ads.Leads</c>.</summary>
public sealed class AdLeadFormsResource
{
    private readonly OmniSocialsClient _client;

    internal AdLeadFormsResource(OmniSocialsClient client) => _client = client;

    private static string Form(long formId) => $"/ads/lead-forms/{AdsPaths.Id(formId)}";

    /// <summary>
    /// <c>GET /ads/lead-forms?ad_account_id=</c>: the lead forms. <c>id</c> is the
    /// OmniSocials form id, <c>external_id</c> the network's (use that as
    /// <see cref="AdNetworkSettings.LeadFormId"/>). Requires <c>ads:read</c>.
    /// </summary>
    public Task<JsonElement?> ListAsync(long adAccountId, CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/lead-forms", AdsPaths.AccountQuery(adAccountId), cancellationToken);

    /// <summary><c>POST /ads/lead-forms</c>: create a lead form (see <see cref="AdLeadFormCreateParams"/>). Requires <c>ads:write</c>.</summary>
    public Task<JsonElement?> CreateAsync(object parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/lead-forms", parameters, cancellationToken);

    /// <summary><c>GET /ads/lead-forms/:formId</c>: one lead form. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> GetAsync(long formId, CancellationToken cancellationToken = default)
        => _client.GetAsync(Form(formId), null, cancellationToken);

    /// <summary><c>PATCH /ads/lead-forms/:formId</c>: <c>status</c> "active" or "archived", <c>sync_enabled</c>. Requires <c>ads:write</c>.</summary>
    public Task<JsonElement?> UpdateAsync(long formId, object parameters, CancellationToken cancellationToken = default)
        => _client.PatchAsync(Form(formId), parameters, cancellationToken);

    /// <summary><c>DELETE /ads/lead-forms/:formId</c>: archive a form; the leads already copied stay. Requires <c>ads:write</c>.</summary>
    public Task<JsonElement?> DeleteAsync(long formId, CancellationToken cancellationToken = default)
        => _client.DeleteAsync(Form(formId), cancellationToken);

    /// <summary><c>POST /ads/lead-forms/:formId/test-leads</c>: send a test lead (optional <c>fields</c>), marked <c>is_test</c>. Requires <c>ads:write</c>.</summary>
    public Task<JsonElement?> SendTestLeadAsync(long formId, object? parameters = null, CancellationToken cancellationToken = default)
        => _client.PostAsync($"{Form(formId)}/test-leads", parameters, cancellationToken);
}

/// <summary>
/// The leads of the workspace (<c>client.Ads.Leads</c>). New leads are copied
/// every 15 minutes (webhook <c>lead.received</c>). The answers are personal
/// data; OmniSocials stores them encrypted.
/// </summary>
public sealed class AdLeadsResource
{
    private readonly OmniSocialsClient _client;

    internal AdLeadsResource(OmniSocialsClient client) => _client = client;

    /// <summary>
    /// <c>GET /ads/leads</c>: newest first, by <c>form_id</c>, <c>status</c> ("new",
    /// "contacted", "qualified", "won", "lost", "spam"), <c>since</c>, <c>until</c>;
    /// <c>limit</c> up to 200 and <c>cursor</c>. Requires <c>ads:read</c>.
    /// </summary>
    public Task<JsonElement?> ListAsync(IReadOnlyDictionary<string, object?>? query = null, CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/leads", OmniSocialsClient.ToQuery(query), cancellationToken);

    /// <summary><c>GET /ads/leads/counts</c>: <c>total</c> and <c>by_status</c>, optionally of one <c>form_id</c>. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> CountsAsync(IReadOnlyDictionary<string, object?>? query = null, CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/leads/counts", OmniSocialsClient.ToQuery(query), cancellationToken);

    /// <summary><c>PATCH /ads/leads/:leadId</c>: set a lead's <c>status</c> or <c>note</c>. Requires <c>ads:write</c>.</summary>
    public Task<JsonElement?> UpdateAsync(long leadId, object parameters, CancellationToken cancellationToken = default)
        => _client.PatchAsync($"/ads/leads/{AdsPaths.Id(leadId)}", parameters, cancellationToken);
}

/// <summary>Conversion tracking (<c>client.Ads.Conversions</c>).</summary>
public sealed class AdConversionsResource
{
    private readonly OmniSocialsClient _client;

    internal AdConversionsResource(OmniSocialsClient client) => _client = client;

    /// <summary><c>GET /ads/conversions/destinations?ad_account_id=</c>: pixels, datasets, conversion tags and actions. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> DestinationsAsync(long adAccountId, CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/conversions/destinations", AdsPaths.AccountQuery(adAccountId), cancellationToken);

    /// <summary><c>GET /ads/conversions/goals?ad_account_id=</c>: the events a campaign can optimise for. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> GoalsAsync(long adAccountId, CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/conversions/goals", AdsPaths.AccountQuery(adAccountId), cancellationToken);

    /// <summary>
    /// <c>POST /ads/conversions/custom</c>: <c>ad_account_id</c>, <c>name</c>,
    /// <c>destination_id</c> and an <c>event</c> and/or <c>url_contains</c> rule.
    /// Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> CreateCustomAsync(object parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/conversions/custom", parameters, cancellationToken);

    /// <summary>
    /// <c>POST /ads/conversions/events</c>: <c>ad_account_id</c>, <c>destination_id</c>
    /// and up to 1,000 <c>events</c>. Send the raw email and phone: OmniSocials
    /// normalizes and SHA-256 hashes them before they leave and stores nothing.
    /// Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> SendEventsAsync(object parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/conversions/events", parameters, cancellationToken);
}

/// <summary>
/// Numbers (<c>client.Ads.Insights</c>); long reports are in
/// <c>client.Ads.Reports</c>. Money in major units;
/// <c>ctr</c> is a percent. All calls are reads (<c>ads:read</c>). Rows carry
/// <see cref="AdMetrics"/>.
/// </summary>
public sealed class AdInsightsResource
{
    private readonly OmniSocialsClient _client;

    internal AdInsightsResource(OmniSocialsClient client) => _client = client;

    /// <summary>
    /// <c>POST /ads/insights</c>: live numbers (<c>ad_account_id</c>, <c>level</c>,
    /// <c>ids</c>, <c>metrics</c>, up to 2 <c>breakdowns</c>, <c>from</c>,
    /// <c>to</c>, <c>time_increment</c>). Answers <c>rows</c>.
    /// </summary>
    public Task<JsonElement?> QueryAsync(object parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/insights", parameters, cancellationToken);

    /// <summary>
    /// <c>GET /ads/campaigns/:id/analytics</c>: numbers of an OmniSocials campaign
    /// (<c>from</c>, <c>to</c>, <c>breakdown</c>: "ad" gives one row per ad,
    /// <c>time_increment</c>, <c>metrics</c>).
    /// </summary>
    public Task<JsonElement?> CampaignAsync(long campaignId, IReadOnlyDictionary<string, object?>? query = null, CancellationToken cancellationToken = default)
        => _client.GetAsync($"{AdsPaths.Campaign(campaignId)}/analytics", OmniSocialsClient.ToQuery(query), cancellationToken);

    /// <summary><c>GET /ads/campaigns/:id/ads/:adId/analytics</c>: numbers of one ad (<c>from</c>, <c>to</c>, <c>breakdown</c>).</summary>
    public Task<JsonElement?> AdAsync(long campaignId, long adId, IReadOnlyDictionary<string, object?>? query = null, CancellationToken cancellationToken = default)
        => _client.GetAsync($"{AdsPaths.Ad(campaignId, adId)}/analytics", OmniSocialsClient.ToQuery(query), cancellationToken);

    /// <summary><c>GET /ads/timeline</c>: daily totals (<c>from</c>, <c>to</c>, <c>platform</c>, <c>source</c> "all", "omnisocials" or "external").</summary>
    public Task<JsonElement?> TimelineAsync(IReadOnlyDictionary<string, object?>? query = null, CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/timeline", OmniSocialsClient.ToQuery(query), cancellationToken);

    /// <summary><c>GET /ads/performance</c>: campaigns or ads ranked by one metric (<c>level</c>, <c>metric</c>, <c>order</c>, <c>limit</c>, <c>min_impressions</c>, <c>from</c>, <c>to</c>, <c>platform</c>).</summary>
    public Task<JsonElement?> PerformanceAsync(IReadOnlyDictionary<string, object?>? query = null, CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/performance", OmniSocialsClient.ToQuery(query), cancellationToken);

    /// <summary><c>GET /ads/alerts</c>: rejected ads, held campaigns, sign-in or payment problems, no delivery.</summary>
    public Task<JsonElement?> AlertsAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/alerts", null, cancellationToken);
}

/// <summary>Long reports that run in the background (<c>client.Ads.Reports</c>). All reads (<c>ads:read</c>).</summary>
public sealed class AdReportsResource
{
    private readonly OmniSocialsClient _client;

    internal AdReportsResource(OmniSocialsClient client) => _client = client;

    /// <summary><c>GET /ads/insights/reports</c>: the background reports of the workspace.</summary>
    public Task<JsonElement?> ListAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/insights/reports", null, cancellationToken);

    /// <summary><c>POST /ads/insights/reports</c>: start a long report (up to 730 days, the body of <see cref="AdInsightsResource.QueryAsync"/>). Answers 202.</summary>
    public Task<JsonElement?> StartAsync(object parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/insights/reports", parameters, cancellationToken);

    /// <summary><c>GET /ads/insights/reports/:reportId</c>: "running", "done" (with <c>rows</c>) or "failed".</summary>
    public Task<JsonElement?> GetAsync(string reportId, CancellationToken cancellationToken = default)
        => _client.GetAsync($"/ads/insights/reports/{AdsPaths.Seg(reportId)}", null, cancellationToken);
}

/// <summary>The ads of an OmniSocials campaign (<c>client.Ads.CampaignAds</c>).</summary>
public sealed class AdCampaignAdsResource
{
    private readonly OmniSocialsClient _client;

    internal AdCampaignAdsResource(OmniSocialsClient client) => _client = client;

    /// <summary><c>GET /ads/campaigns/:id/ads</c>: the ads with their status, review and latest numbers. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> ListAsync(long campaignId, CancellationToken cancellationToken = default)
        => _client.GetAsync($"{AdsPaths.Campaign(campaignId)}/ads", null, cancellationToken);

    /// <summary>
    /// <c>POST /ads/campaigns/:id/ads</c>: add up to 6 new ads to a campaign that
    /// runs, in the shape of a draft's ads. The new ads are created PAUSED on the
    /// network; switch each one on with <see cref="SetStatusAsync"/>. Adding ads
    /// to a running campaign needs a key with the opt-in <c>ads:spend</c> scope
    /// ("Start ads"); without it, and on Google (<c>api_draft_only</c>) always,
    /// the call answers <c>403 start_in_app</c> with the page where a person does
    /// it in the app. Webhook <c>ad.created</c>. Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> AddAsync(long campaignId, IEnumerable<AdDraftAd> ads, CancellationToken cancellationToken = default)
        => _client.PostAsync($"{AdsPaths.Campaign(campaignId)}/ads", new { ads = ads.ToArray() }, cancellationToken);

    /// <summary><c>GET /ads/campaigns/:id/ads/:adId</c>: one ad. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> GetAsync(long campaignId, long adId, CancellationToken cancellationToken = default)
        => _client.GetAsync(AdsPaths.Ad(campaignId, adId), null, cancellationToken);

    /// <summary>
    /// <c>PUT /ads/campaigns/:id/ads/:adId/status</c>: pause ("paused") or switch
    /// on ("active") one ad. Switching on STARTS REAL AD SPEND: show the person
    /// the campaign budget and get their yes first, then pass it as
    /// <paramref name="confirmBudgetAmount"/>. Switching on needs a key with the
    /// opt-in <c>ads:spend</c> scope ("Start ads"); without it, and on Google
    /// (<c>api_draft_only</c>) always, the call answers <c>403 start_in_app</c>
    /// with the page where a person does it in the app. Pausing needs no amount
    /// and stays open (Google: <c>start_in_app</c>). Webhook <c>ad.updated</c>.
    /// Not retried after a 5xx, a connection error or a timeout: read the ad with
    /// <see cref="GetAsync"/>. Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> SetStatusAsync(long campaignId, long adId, string status, decimal? confirmBudgetAmount = null, CancellationToken cancellationToken = default)
        => _client.PutAsync(
            $"{AdsPaths.Ad(campaignId, adId)}/status",
            AdsPaths.StatusBody(status, confirmBudgetAmount),
            RetryPolicy.RateLimitOnly,
            cancellationToken);

    /// <summary><c>GET /ads/campaigns/:id/ads/:adId/review</c>: "in_review", "approved" or "rejected" with reasons. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> ReviewAsync(long campaignId, long adId, CancellationToken cancellationToken = default)
        => _client.GetAsync($"{AdsPaths.Ad(campaignId, adId)}/review", null, cancellationToken);
}

/// <summary>Previews drawn by the network (<c>client.Ads.Previews</c>). Spends nothing; both are reads (<c>ads:read</c>).</summary>
public sealed class AdPreviewsResource
{
    private readonly OmniSocialsClient _client;

    internal AdPreviewsResource(OmniSocialsClient client) => _client = client;

    /// <summary>
    /// <c>POST /ads/previews</c>: previews of a new ad (<c>ad_account_id</c>,
    /// <c>ad</c>, optional <c>placements</c> and <c>objective</c>). Answers
    /// <c>previews</c> (<c>placement</c>, <c>html</c>, <c>url</c>).
    /// </summary>
    public Task<JsonElement?> CreateAsync(object parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/previews", parameters, cancellationToken);

    /// <summary><c>GET /ads/campaigns/:id/ads/:adId/previews</c>: previews of an existing ad, optionally for some placements.</summary>
    public Task<JsonElement?> ForAdAsync(long campaignId, long adId, IEnumerable<string>? placements = null, CancellationToken cancellationToken = default)
    {
        var list = placements?.ToArray();
        var query = list is { Length: > 0 }
            ? new[] { new KeyValuePair<string, string?>("placements", string.Join(",", list)) }
            : null;
        return _client.GetAsync($"{AdsPaths.Ad(campaignId, adId)}/previews", query, cancellationToken);
    }
}

/// <summary>
/// Automated rules (<c>client.Ads.Rules</c>): <c>notify</c> (webhook
/// <c>ad_rule.triggered</c>), <c>pause</c>, <c>activate</c> or
/// <c>adjust_budget</c>. A rule that notifies, pauses or lowers a budget works
/// with <c>ads:write</c>. A rule that can raise spend (activate, a positive
/// adjust_budget) needs a key with the opt-in <c>ads:spend</c> scope (else
/// <c>403 rule_raises_spend</c>) and <c>action.max_daily_budget</c>, confirmed by
/// the person as <c>confirm_max_daily_budget</c>; never invent or confirm that
/// amount yourself. At run time a rule never acts on Google.
/// <see cref="CreateAsync"/>, <see cref="UpdateAsync"/> and
/// <see cref="UndoAsync"/> are retried on a 429 only.
/// </summary>
public sealed class AdRulesResource
{
    private readonly OmniSocialsClient _client;

    internal AdRulesResource(OmniSocialsClient client) => _client = client;

    private static string Rule(long ruleId) => $"/ads/rules/{AdsPaths.Id(ruleId)}";

    /// <summary><c>GET /ads/rules</c>. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> ListAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/rules", null, cancellationToken);

    /// <summary>
    /// <c>POST /ads/rules</c>: create a rule (<c>name</c>, <c>conditions</c>,
    /// <c>action</c>, optional <c>enabled</c>, <c>scope</c>, <c>every_minutes</c>
    /// ...). A rule that can RAISE AD SPEND needs <c>confirm_max_daily_budget</c>
    /// = <c>action.max_daily_budget</c>, after the person agreed to it, and a key
    /// with <c>ads:spend</c> (else <c>403 rule_raises_spend</c>). Preview it
    /// first with <see cref="PreviewAsync"/>. Not retried after a 5xx, a
    /// connection error or a timeout. Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> CreateAsync(object parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/rules", parameters, RetryPolicy.RateLimitOnly, cancellationToken);

    /// <summary><c>GET /ads/rules/templates</c>: ready-made rules. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> TemplatesAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/rules/templates", null, cancellationToken);

    /// <summary><c>POST /ads/rules/preview</c>: what an unsaved rule would match and do now. Changes nothing. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> PreviewAsync(object parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/rules/preview", parameters, cancellationToken);

    /// <summary><c>GET /ads/rules/activity</c>: every change the rules made (<c>rule_id</c>, <c>limit</c>). Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> ActivityAsync(IReadOnlyDictionary<string, object?>? query = null, CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/rules/activity", OmniSocialsClient.ToQuery(query), cancellationToken);

    /// <summary>
    /// <c>POST /ads/rules/activity/:activityId/undo</c>: undo a change a rule
    /// made. Undoing a pause or a budget cut STARTS OR RAISES REAL AD SPEND and
    /// needs <paramref name="confirmBudgetAmount"/> = the budget, after the
    /// person agreed to it, and a key with <c>ads:spend</c> (else, and on Google
    /// always, <c>403 start_in_app</c>); undoing a budget raise needs none. Not
    /// retried after a 5xx, a connection error or a timeout. Requires
    /// <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> UndoAsync(long activityId, decimal? confirmBudgetAmount = null, CancellationToken cancellationToken = default)
        => _client.PostAsync(
            $"/ads/rules/activity/{AdsPaths.Id(activityId)}/undo",
            confirmBudgetAmount.HasValue ? new { confirm_budget_amount = confirmBudgetAmount.Value } : null,
            RetryPolicy.RateLimitOnly,
            cancellationToken);

    /// <summary><c>GET /ads/rules/:ruleId</c>. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> GetAsync(long ruleId, CancellationToken cancellationToken = default)
        => _client.GetAsync(Rule(ruleId), null, cancellationToken);

    /// <summary>
    /// <c>PATCH /ads/rules/:ruleId</c>: change a rule, also <c>enabled</c>. A
    /// changed action that can RAISE AD SPEND needs
    /// <c>confirm_max_daily_budget</c> again and a key with <c>ads:spend</c>
    /// (else <c>403 rule_raises_spend</c>). Not retried after a 5xx, a connection
    /// error or a timeout. Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> UpdateAsync(long ruleId, object parameters, CancellationToken cancellationToken = default)
        => _client.PatchAsync(Rule(ruleId), parameters, RetryPolicy.RateLimitOnly, cancellationToken);

    /// <summary><c>DELETE /ads/rules/:ruleId</c>. Requires <c>ads:write</c>.</summary>
    public Task<JsonElement?> DeleteAsync(long ruleId, CancellationToken cancellationToken = default)
        => _client.DeleteAsync(Rule(ruleId), cancellationToken);

    /// <summary><c>POST /ads/rules/:ruleId/preview</c>: what a saved rule would match and do now. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> PreviewSavedAsync(long ruleId, CancellationToken cancellationToken = default)
        => _client.PostAsync($"{Rule(ruleId)}/preview", null, cancellationToken);

    /// <summary><c>GET /ads/rules/:ruleId/runs</c>. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> RunsAsync(long ruleId, IReadOnlyDictionary<string, object?>? query = null, CancellationToken cancellationToken = default)
        => _client.GetAsync($"{Rule(ruleId)}/runs", OmniSocialsClient.ToQuery(query), cancellationToken);
}

/// <summary>Your own numbers per campaign and day (<c>client.Ads.Results</c>). Rules read them as <c>results.&lt;metric&gt;</c>.</summary>
public sealed class AdResultsResource
{
    private readonly OmniSocialsClient _client;

    internal AdResultsResource(OmniSocialsClient client) => _client = client;

    /// <summary><c>GET /ads/campaign-results</c> (<c>from</c>, <c>to</c>, <c>campaign_id</c>, <c>metric</c>). Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> ListAsync(IReadOnlyDictionary<string, object?>? query = null, CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/campaign-results", OmniSocialsClient.ToQuery(query), cancellationToken);

    /// <summary>
    /// <c>POST /ads/campaign-results</c>: upload <c>rows</c> (<c>day</c>, <c>metric</c>,
    /// <c>value</c> and the campaign; at most 5,000). Sending a day again replaces
    /// it. Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> UploadAsync(object parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/campaign-results", parameters, cancellationToken);

    /// <summary><c>GET /ads/campaign-results/totals</c> with the formulas. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> TotalsAsync(IReadOnlyDictionary<string, object?>? query = null, CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/campaign-results/totals", OmniSocialsClient.ToQuery(query), cancellationToken);

    /// <summary><c>GET /ads/campaign-results/metrics</c>: your metric names and formulas. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> MetricsAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/campaign-results/metrics", null, cancellationToken);

    /// <summary><c>PUT /ads/campaign-results/metrics/:key</c>: <c>label</c>, <c>unit</c>, <c>formula</c>. Requires <c>ads:write</c>.</summary>
    public Task<JsonElement?> PutMetricAsync(string key, object parameters, CancellationToken cancellationToken = default)
        => _client.PutAsync($"/ads/campaign-results/metrics/{AdsPaths.Seg(key)}", parameters, cancellationToken);
}

/// <summary>
/// The network's own image and video library (<c>client.Ads.Media</c>). Use an
/// uploaded file in an ad as <c>network.&lt;network&gt;.media_ref</c>.
/// </summary>
public sealed class AdMediaResource
{
    private readonly OmniSocialsClient _client;

    internal AdMediaResource(OmniSocialsClient client) => _client = client;

    /// <summary><c>GET /ads/media</c> (<c>ad_account_id</c>, <c>type</c>, <c>cursor</c>). Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> ListAsync(IReadOnlyDictionary<string, object?> query, CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/media", OmniSocialsClient.ToQuery(query), cancellationToken);

    /// <summary><c>POST /ads/media</c>: <c>ad_account_id</c>, <c>type</c>, <c>url</c>, optional <c>name</c>. Requires <c>ads:write</c>.</summary>
    public Task<JsonElement?> UploadAsync(object parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/media", parameters, cancellationToken);

    /// <summary><c>GET /ads/media/:mediaId</c>: processing status (<c>ad_account_id</c>, <c>type</c>). Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> GetAsync(string mediaId, IReadOnlyDictionary<string, object?> query, CancellationToken cancellationToken = default)
        => _client.GetAsync($"/ads/media/{AdsPaths.Seg(mediaId)}", OmniSocialsClient.ToQuery(query), cancellationToken);

    /// <summary><c>DELETE /ads/media/:mediaId</c> (<c>ad_account_id</c> and <c>type</c>). Requires <c>ads:write</c>.</summary>
    public Task<JsonElement?> DeleteAsync(string mediaId, IReadOnlyDictionary<string, object?> query, CancellationToken cancellationToken = default)
        => _client.DeleteAsync($"/ads/media/{AdsPaths.Seg(mediaId)}", OmniSocialsClient.ToQuery(query), null, cancellationToken);
}

/// <summary>Partnership ads with creators on Meta (<c>client.Ads.Partnership</c>).</summary>
public sealed class AdPartnershipResource
{
    private readonly OmniSocialsClient _client;

    internal AdPartnershipResource(OmniSocialsClient client) => _client = client;

    /// <summary><c>GET /ads/partnership/content?ad_account_id=</c>: creator posts you may run. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> ListContentAsync(long adAccountId, CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/partnership/content", AdsPaths.AccountQuery(adAccountId), cancellationToken);

    /// <summary><c>GET /ads/partnership/permissions?ad_account_id=</c>. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> ListPermissionsAsync(long adAccountId, CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/partnership/permissions", AdsPaths.AccountQuery(adAccountId), cancellationToken);

    /// <summary><c>POST /ads/partnership/permissions</c>: ask for or set a permission. Requires <c>ads:write</c>.</summary>
    public Task<JsonElement?> SetPermissionAsync(object parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/partnership/permissions", parameters, cancellationToken);
}

/// <summary>Comments on the ads of an OmniSocials campaign (<c>client.Ads.Comments</c>).</summary>
public sealed class AdCommentsResource
{
    private readonly OmniSocialsClient _client;

    internal AdCommentsResource(OmniSocialsClient client) => _client = client;

    private static string Comments(long campaignId, long adId) => $"{AdsPaths.Ad(campaignId, adId)}/comments";

    /// <summary><c>GET /ads/campaigns/:id/ads/:adId/comments</c> (<c>cursor</c>). Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> ListAsync(long campaignId, long adId, IReadOnlyDictionary<string, object?>? query = null, CancellationToken cancellationToken = default)
        => _client.GetAsync(Comments(campaignId, adId), OmniSocialsClient.ToQuery(query), cancellationToken);

    /// <summary><c>POST .../comments/:commentId/reply</c>: answer a comment. Requires <c>ads:write</c>.</summary>
    public Task<JsonElement?> ReplyAsync(long campaignId, long adId, string commentId, string text, CancellationToken cancellationToken = default)
        => _client.PostAsync($"{Comments(campaignId, adId)}/{AdsPaths.Seg(commentId)}/reply", new { text }, cancellationToken);

    /// <summary><c>POST .../comments/:commentId/hide</c>: hide a comment, or show it again with <paramref name="hidden"/> false. Requires <c>ads:write</c>.</summary>
    public Task<JsonElement?> HideAsync(long campaignId, long adId, string commentId, bool hidden = true, CancellationToken cancellationToken = default)
        => _client.PostAsync($"{Comments(campaignId, adId)}/{AdsPaths.Seg(commentId)}/hide", new { hidden }, cancellationToken);

    /// <summary><c>DELETE .../comments/:commentId</c>. Requires <c>ads:write</c>.</summary>
    public Task<JsonElement?> DeleteAsync(long campaignId, long adId, string commentId, CancellationToken cancellationToken = default)
        => _client.DeleteAsync($"{Comments(campaignId, adId)}/{AdsPaths.Seg(commentId)}", cancellationToken);
}

/// <summary>
/// One connected ad account (<c>client.Ads.Accounts</c>): health, money,
/// settings, people, the change log, businesses and identities.
/// </summary>
public sealed class AdAccountsResource
{
    private readonly OmniSocialsClient _client;

    internal AdAccountsResource(OmniSocialsClient client) => _client = client;

    /// <summary><c>GET /ads/accounts/:accountId/health</c>: status, payment, spend limit, Page, pixel; failed checks carry a <c>fix_url</c>. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> HealthAsync(long accountId, CancellationToken cancellationToken = default)
        => _client.GetAsync($"{AdsPaths.Account(accountId)}/health", null, cancellationToken);

    /// <summary><c>GET /ads/accounts/:accountId/finance</c>: spend limit, amount spent, balance and payment method. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> FinanceAsync(long accountId, CancellationToken cancellationToken = default)
        => _client.GetAsync($"{AdsPaths.Account(accountId)}/finance", null, cancellationToken);

    /// <summary>
    /// <c>PATCH /ads/accounts/:accountId/settings</c>: <c>spend_cap</c>,
    /// <c>dsa_beneficiary</c>, <c>dsa_payor</c>, <c>name</c>. A HIGHER SPEND CAP
    /// lets the account spend more: show the person the new cap and get their yes
    /// first, then pass it as <c>confirm_budget_amount</c>; removing the cap needs
    /// <c>new Dictionary&lt;string, object?&gt; { ["spend_cap"] = null, ["confirm_budget_amount"] = "remove" }</c>
    /// (a dictionary keeps the null). A spend cap change needs a key with the
    /// opt-in <c>ads:spend</c> scope ("Start ads"); without it, and on Google
    /// (<c>api_draft_only</c>) always, the call answers <c>403 start_in_app</c>
    /// with the page where a person does it in the app. The name and the DSA
    /// names stay open. Not retried after a 5xx, a connection error or a timeout:
    /// read <see cref="FinanceAsync"/> to see what happened. Requires
    /// <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> UpdateSettingsAsync(long accountId, object parameters, CancellationToken cancellationToken = default)
        => _client.PatchAsync($"{AdsPaths.Account(accountId)}/settings", parameters, RetryPolicy.RateLimitOnly, cancellationToken);

    /// <summary><c>GET /ads/accounts/:accountId/users</c>: people with access. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> ListUsersAsync(long accountId, CancellationToken cancellationToken = default)
        => _client.GetAsync($"{AdsPaths.Account(accountId)}/users", null, cancellationToken);

    /// <summary><c>POST /ads/accounts/:accountId/users</c>: give access (LinkedIn; <c>role</c> and <c>email</c> or <c>member_id</c>). Requires <c>ads:write</c>.</summary>
    public Task<JsonElement?> AddUserAsync(long accountId, object parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync($"{AdsPaths.Account(accountId)}/users", parameters, cancellationToken);

    /// <summary><c>DELETE /ads/accounts/:accountId/users/:userId</c>. Requires <c>ads:write</c>.</summary>
    public Task<JsonElement?> RemoveUserAsync(long accountId, string userId, CancellationToken cancellationToken = default)
        => _client.DeleteAsync($"{AdsPaths.Account(accountId)}/users/{AdsPaths.Seg(userId)}", cancellationToken);

    /// <summary><c>GET /ads/accounts/:accountId/activity</c>: the change log (<c>since</c>, <c>until</c>, <c>cursor</c>). Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> ActivityAsync(long accountId, IReadOnlyDictionary<string, object?>? query = null, CancellationToken cancellationToken = default)
        => _client.GetAsync($"{AdsPaths.Account(accountId)}/activity", OmniSocialsClient.ToQuery(query), cancellationToken);

    /// <summary><c>GET /ads/accounts/:accountId/businesses</c>: business portfolios (Meta), Business Centers (TikTok). Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> BusinessesAsync(long accountId, CancellationToken cancellationToken = default)
        => _client.GetAsync($"{AdsPaths.Account(accountId)}/businesses", null, cancellationToken);

    /// <summary><c>GET /ads/accounts/:accountId/dsa</c>: the EU DSA payer and beneficiary (Meta). Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> DsaAsync(long accountId, CancellationToken cancellationToken = default)
        => _client.GetAsync($"{AdsPaths.Account(accountId)}/dsa", null, cancellationToken);

    /// <summary><c>GET /ads/accounts/:accountId/hierarchy</c>: the manager account hierarchy (Google). Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> HierarchyAsync(long accountId, CancellationToken cancellationToken = default)
        => _client.GetAsync($"{AdsPaths.Account(accountId)}/hierarchy", null, cancellationToken);

    /// <summary><c>GET /ads/accounts/:accountId/identities</c>: Pages, profiles and identities an ad can run as. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> IdentitiesAsync(long accountId, CancellationToken cancellationToken = default)
        => _client.GetAsync($"{AdsPaths.Account(accountId)}/identities", null, cancellationToken);
}

/// <summary>Reach and frequency buying on Meta (<c>client.Ads.ReachFrequency</c>). A prediction spends nothing; a reservation does.</summary>
public sealed class AdReachFrequencyResource
{
    private readonly OmniSocialsClient _client;

    internal AdReachFrequencyResource(OmniSocialsClient client) => _client = client;

    private static string Prediction(string predictionId) => $"/ads/reach-frequency/{AdsPaths.Seg(predictionId)}";

    /// <summary>
    /// <c>POST /ads/reach-frequency</c>: create a prediction (see
    /// <see cref="AdReachFrequencyParams"/>: exactly one country, a budget in
    /// major units or a reach). Meta needs the reach and frequency capability on
    /// the ad account. Spends nothing. Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> CreateAsync(object parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/reach-frequency", parameters, cancellationToken);

    /// <summary><c>GET /ads/reach-frequency/:predictionId?ad_account_id=</c>. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> GetAsync(string predictionId, long adAccountId, CancellationToken cancellationToken = default)
        => _client.GetAsync(Prediction(predictionId), AdsPaths.AccountQuery(adAccountId), cancellationToken);

    /// <summary><c>DELETE /ads/reach-frequency/:predictionId?ad_account_id=</c>: cancel a prediction or reservation. Requires <c>ads:write</c>.</summary>
    public Task<JsonElement?> CancelAsync(string predictionId, long adAccountId, CancellationToken cancellationToken = default)
        => _client.DeleteAsync(Prediction(predictionId), AdsPaths.AccountQuery(adAccountId), null, cancellationToken);

    /// <summary>
    /// <c>POST /ads/reach-frequency/:predictionId/reserve</c>: COMMITS REAL AD
    /// SPEND. Show the person the prediction's budget and get their yes first;
    /// <paramref name="confirmBudgetAmount"/> must equal it. Needs a key with the
    /// opt-in <c>ads:spend</c> scope ("Start ads"); without it, and on Google
    /// (<c>api_draft_only</c>) always, the call answers <c>403 start_in_app</c>
    /// with the page where a person does it in the app. Not retried after a 5xx,
    /// a connection error or a timeout. Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> ReserveAsync(string predictionId, long adAccountId, decimal confirmBudgetAmount, CancellationToken cancellationToken = default)
        => _client.PostAsync(
            $"{Prediction(predictionId)}/reserve",
            new { ad_account_id = adAccountId, confirm_budget_amount = confirmBudgetAmount },
            RetryPolicy.RateLimitOnly,
            cancellationToken);
}

/// <summary>A/B tests and lift studies on Meta (<c>client.Ads.Studies</c>).</summary>
public sealed class AdStudiesResource
{
    private readonly OmniSocialsClient _client;

    internal AdStudiesResource(OmniSocialsClient client) => _client = client;

    /// <summary><c>GET /ads/studies?ad_account_id=</c>. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> ListAsync(long adAccountId, CancellationToken cancellationToken = default)
        => _client.GetAsync("/ads/studies", AdsPaths.AccountQuery(adAccountId), cancellationToken);

    /// <summary>
    /// <c>POST /ads/studies</c>: an A/B test between existing campaigns or ad
    /// sets. Needs a key with the opt-in <c>ads:spend</c> scope ("Start ads");
    /// without it, and on Google (<c>api_draft_only</c>) always, the call answers
    /// <c>403 start_in_app</c> with the page where a person does it in the app.
    /// Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> CreateAsync(object parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/ads/studies", parameters, cancellationToken);
}

/// <summary>Planned budget increases for high-demand periods on Meta (<c>client.Ads.BudgetSchedules</c>).</summary>
public sealed class AdBudgetSchedulesResource
{
    private readonly OmniSocialsClient _client;

    internal AdBudgetSchedulesResource(OmniSocialsClient client) => _client = client;

    private static string Schedules(long campaignId) => $"{AdsPaths.Campaign(campaignId)}/budget-schedules";

    /// <summary><c>GET /ads/campaigns/:id/budget-schedules</c>. Requires <c>ads:read</c>.</summary>
    public Task<JsonElement?> ListAsync(long campaignId, CancellationToken cancellationToken = default)
        => _client.GetAsync(Schedules(campaignId), null, cancellationToken);

    /// <summary>
    /// <c>POST /ads/campaigns/:id/budget-schedules</c>: NEW AD SPEND in that
    /// period (see <see cref="AdBudgetScheduleParams"/>). Show the person the
    /// amount and get their yes first; <c>confirm_budget_amount</c> = the daily
    /// total during the period (the daily budget plus the increase), under the
    /// OmniSocials daily cap. Needs a key with the opt-in <c>ads:spend</c> scope
    /// ("Start ads"); without it, and on Google (<c>api_draft_only</c>) always,
    /// the call answers <c>403 start_in_app</c> with the page where a person does
    /// it in the app. Not retried after a 5xx, a connection error or a timeout.
    /// Requires <c>ads:write</c>.
    /// </summary>
    public Task<JsonElement?> CreateAsync(long campaignId, object parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync(Schedules(campaignId), parameters, RetryPolicy.RateLimitOnly, cancellationToken);

    /// <summary><c>DELETE /ads/campaigns/:id/budget-schedules/:scheduleId</c>. Requires <c>ads:write</c>.</summary>
    public Task<JsonElement?> DeleteAsync(long campaignId, string scheduleId, CancellationToken cancellationToken = default)
        => _client.DeleteAsync($"{Schedules(campaignId)}/{AdsPaths.Seg(scheduleId)}", cancellationToken);
}

/// <summary>
/// Google Ads reports (<c>client.Ads.Google</c>). Every call takes
/// <c>ad_account_id</c> and is a read (<c>ads:read</c>). Every API caller stops
/// at the draft on Google, also a key with <c>ads:spend</c>
/// (<c>403 start_in_app</c>).
/// </summary>
public sealed class AdGoogleResource
{
    private readonly OmniSocialsClient _client;

    internal AdGoogleResource(OmniSocialsClient client) => _client = client;

    private Task<JsonElement?> Read(string report, IReadOnlyDictionary<string, object?> query, CancellationToken cancellationToken)
        => _client.GetAsync($"/ads/google/{report}", OmniSocialsClient.ToQuery(query), cancellationToken);

    /// <summary><c>GET /ads/google/search-terms</c> (<c>campaign_id</c> or <c>external_campaign_id</c>, <c>from</c>, <c>to</c>, <c>limit</c>).</summary>
    public Task<JsonElement?> SearchTermsAsync(IReadOnlyDictionary<string, object?> query, CancellationToken cancellationToken = default)
        => Read("search-terms", query, cancellationToken);

    /// <summary><c>GET /ads/google/recommendations</c> (<c>types</c>).</summary>
    public Task<JsonElement?> RecommendationsAsync(IReadOnlyDictionary<string, object?> query, CancellationToken cancellationToken = default)
        => Read("recommendations", query, cancellationToken);

    /// <summary><c>GET /ads/google/keywords</c> (<c>external_campaign_id</c>, <c>external_ad_set_id</c>).</summary>
    public Task<JsonElement?> KeywordsAsync(IReadOnlyDictionary<string, object?> query, CancellationToken cancellationToken = default)
        => Read("keywords", query, cancellationToken);

    /// <summary><c>GET /ads/google/assets</c>: sitelinks, callouts and structured snippets (<c>type</c>).</summary>
    public Task<JsonElement?> AssetsAsync(IReadOnlyDictionary<string, object?> query, CancellationToken cancellationToken = default)
        => Read("assets", query, cancellationToken);

    /// <summary><c>GET /ads/google/shared-budgets</c>.</summary>
    public Task<JsonElement?> SharedBudgetsAsync(IReadOnlyDictionary<string, object?> query, CancellationToken cancellationToken = default)
        => Read("shared-budgets", query, cancellationToken);

    /// <summary><c>GET /ads/google/bid-strategies</c>: portfolio bid strategies.</summary>
    public Task<JsonElement?> BidStrategiesAsync(IReadOnlyDictionary<string, object?> query, CancellationToken cancellationToken = default)
        => Read("bid-strategies", query, cancellationToken);

    /// <summary><c>GET /ads/google/ad-schedule</c>: days and hours of a campaign (<c>external_campaign_id</c>).</summary>
    public Task<JsonElement?> AdScheduleAsync(IReadOnlyDictionary<string, object?> query, CancellationToken cancellationToken = default)
        => Read("ad-schedule", query, cancellationToken);

    /// <summary><c>GET /ads/google/labels</c>.</summary>
    public Task<JsonElement?> LabelsAsync(IReadOnlyDictionary<string, object?> query, CancellationToken cancellationToken = default)
        => Read("labels", query, cancellationToken);

    /// <summary><c>GET /ads/google/local-services-leads</c> (<c>since</c>).</summary>
    public Task<JsonElement?> LocalServicesLeadsAsync(IReadOnlyDictionary<string, object?> query, CancellationToken cancellationToken = default)
        => Read("local-services-leads", query, cancellationToken);
}
