using System.Text.Json;
using System.Text.Json.Serialization;

namespace OmniSocials;

// Typed response models for the ads endpoints. Like every resource, the
// AdsResource methods return the raw JsonElement? so callers can read the
// payload directly. These classes are optional, strongly-typed deserialization
// targets: e.g. (await client.Ads.Drafts.GetAsync(groupId))
//   ?.Deserialize<AdDraftResponse>(). They mirror the API's snake_case fields
// via [JsonPropertyName]. Ids and amounts are read from a JSON number or a
// numeric string. Amounts are in major units (10 = 10 EUR).

/// <summary>Billing details of an ad account.</summary>
public sealed class AdAccountInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Billing currency; campaign budgets must use it.</summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }

    /// <summary>
    /// False when no payment method is set on the network; the ads do not run
    /// until the person adds one in the network's Ads Manager. Null when the
    /// network does not say.
    /// </summary>
    [JsonPropertyName("billing_ok")]
    public bool? BillingOk { get; set; }
}

/// <summary>A connected ad account (<see cref="AdsResource.ListAccountsAsync"/>).</summary>
[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public sealed class AdAccount
{
    /// <summary>OmniSocials ad account id (use as <c>ad_account_id</c>).</summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>"meta", "pinterest", "tiktok", "linkedin", "x" or "google".</summary>
    [JsonPropertyName("platform")]
    public string Platform { get; set; } = "";

    /// <summary>The network's own ad account id (for example Meta <c>act_...</c>).</summary>
    [JsonPropertyName("external_account_id")]
    public string? ExternalAccountId { get; set; }

    [JsonPropertyName("account_info")]
    public AdAccountInfo? AccountInfo { get; set; }

    /// <summary>Whether a person picked this account for spending in the dashboard.</summary>
    [JsonPropertyName("is_selected")]
    public bool IsSelected { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("needs_reauth")]
    public bool? NeedsReauth { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }
}

/// <summary>Response of <see cref="AdsResource.ListAccountsAsync"/>.</summary>
public sealed class AdAccountsResponse
{
    [JsonPropertyName("data")]
    public List<AdAccount> Data { get; set; } = new();

    /// <summary>The ads networks this server supports.</summary>
    [JsonPropertyName("platforms_available")]
    public List<string>? PlatformsAvailable { get; set; }
}

/// <summary>A campaign or a boost.</summary>
[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public sealed class AdCampaign
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = "";

    /// <summary>"campaign" or "boost".</summary>
    [JsonPropertyName("campaign_type")]
    public string? CampaignType { get; set; }

    [JsonPropertyName("objective")]
    public string? Objective { get; set; }

    /// <summary>
    /// Created as "draft"; spends only once a person starts it in the app.
    /// "pending_review" = in the network's ad review. Also "active", "rejected",
    /// "paused", "completed", "error".
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("budget")]
    public AdBudget? Budget { get; set; }

    /// <summary>Unified targeting (<c>countries</c>, ages, genders...).</summary>
    [JsonPropertyName("targeting")]
    public JsonElement? Targeting { get; set; }

    [JsonPropertyName("schedule")]
    public AdSchedule? Schedule { get; set; }

    /// <summary>Latest synced numbers: spend (major units), impressions, clicks, ctr.</summary>
    [JsonPropertyName("metrics")]
    public Dictionary<string, JsonElement>? Metrics { get; set; }

    /// <summary>The network's campaign id; null while a draft.</summary>
    [JsonPropertyName("external_campaign_id")]
    public string? ExternalCampaignId { get; set; }

    /// <summary>Readable reason when the status is "error" or "rejected".</summary>
    [JsonPropertyName("errors")]
    public string? Errors { get; set; }

    /// <summary>
    /// Set on a campaign of a "Create an ad" draft: a person starts it in the
    /// app, or
    /// <see cref="AdDraftsResource.LaunchAsync(string, decimal, CancellationToken)"/>
    /// with a key that has <c>ads:spend</c>.
    /// </summary>
    [JsonPropertyName("group_id")]
    public string? GroupId { get; set; }

    /// <summary>The network's settings (optimization, messaging, calls, lead form, Google, Meta).</summary>
    [JsonPropertyName("settings")]
    public AdNetworkSettings? Settings { get; set; }

    /// <summary>A campaign row carries <c>createdAt</c>; <c>created_at</c> is read too.</summary>
    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("created_at")]
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private string? CreatedAtSnakeCase
    {
        get => null;
        set => CreatedAt ??= value;
    }
}

/// <summary>Offset pagination of <c>GET /ads/campaigns</c>.</summary>
public sealed class AdPagination
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }
}

/// <summary>Response of <see cref="AdsResource.ListAsync"/>.</summary>
public sealed class AdCampaignsResponse
{
    [JsonPropertyName("data")]
    public List<AdCampaign> Data { get; set; } = new();

    [JsonPropertyName("pagination")]
    public AdPagination? Pagination { get; set; }
}

/// <summary><c>{ "data": campaign }</c>: get, boost, update, activate, pause and resume.</summary>
public sealed class AdCampaignResponse
{
    [JsonPropertyName("data")]
    public AdCampaign Data { get; set; } = new();
}

/// <summary>
/// One target of <see cref="AdsResource.BoostMultiAsync"/> (with
/// <see cref="AdAccountId"/>) or <see cref="AdsResource.ActivateMultiAsync"/>
/// (with <see cref="CampaignId"/>). One failed target never stops the others.
/// <see cref="Code"/> "start_in_app" = the person starts that campaign in the
/// app (<see cref="Error"/> names the page).
/// </summary>
[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public sealed class AdMultiResult
{
    [JsonPropertyName("ad_account_id")]
    public long? AdAccountId { get; set; }

    [JsonPropertyName("campaign_id")]
    public long? CampaignId { get; set; }

    [JsonPropertyName("platform")]
    public string? Platform { get; set; }

    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("campaign")]
    public AdCampaign? Campaign { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

/// <summary><c>{ "results": [...] }</c> of boost multi and activate multi (no <c>data</c> envelope).</summary>
public sealed class AdMultiResponse
{
    [JsonPropertyName("results")]
    public List<AdMultiResult> Results { get; set; } = new();
}

/// <summary>The totals of <see cref="AdsOverview"/>, spend in major units.</summary>
[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public sealed class AdsTotals
{
    /// <summary>
    /// Null when the campaigns use more than one currency: read
    /// <see cref="AdsOverview.TotalsByCurrency"/> then. Spend is never added
    /// across currencies.
    /// </summary>
    [JsonPropertyName("spend")]
    public decimal? Spend { get; set; }

    /// <summary>The one currency of <see cref="Spend"/>; null when there are several (or none).</summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("impressions")]
    public decimal Impressions { get; set; }

    [JsonPropertyName("clicks")]
    public decimal Clicks { get; set; }
}

/// <summary>Spend, impressions and clicks of the campaigns in one currency.</summary>
[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public sealed class AdsCurrencyTotals
{
    /// <summary>3-letter ISO code; null for campaigns without a currency.</summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("spend")]
    public decimal Spend { get; set; }

    [JsonPropertyName("impressions")]
    public decimal Impressions { get; set; }

    [JsonPropertyName("clicks")]
    public decimal Clicks { get; set; }
}

/// <summary>Response of <see cref="AdsResource.OverviewAsync"/> (inside <c>data</c>).</summary>
public sealed class AdsOverview
{
    [JsonPropertyName("connected_accounts")]
    public int ConnectedAccounts { get; set; }

    [JsonPropertyName("campaigns_total")]
    public int CampaignsTotal { get; set; }

    [JsonPropertyName("campaigns_by_status")]
    public Dictionary<string, int> CampaignsByStatus { get; set; } = new();

    /// <summary>Spend (null with several currencies), currency, impressions, clicks.</summary>
    [JsonPropertyName("totals")]
    public AdsTotals Totals { get; set; } = new();

    /// <summary>Spend, impressions and clicks per currency.</summary>
    [JsonPropertyName("totals_by_currency")]
    public List<AdsCurrencyTotals> TotalsByCurrency { get; set; } = new();
}

/// <summary>An organic post that does better than the workspace median: a boost candidate.</summary>
[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public sealed class AdBoostSuggestion
{
    [JsonPropertyName("post_id")]
    public long PostId { get; set; }

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = "";

    [JsonPropertyName("published_at")]
    public string? PublishedAt { get; set; }

    [JsonPropertyName("metric_value")]
    public double MetricValue { get; set; }

    [JsonPropertyName("workspace_median")]
    public double WorkspaceMedian { get; set; }

    /// <summary>How many times the median.</summary>
    [JsonPropertyName("multiplier")]
    public double Multiplier { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("description_snippet")]
    public string? DescriptionSnippet { get; set; }

    [JsonPropertyName("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }
}

/// <summary>One keyword of a keyword research or metrics answer (Google Ads).</summary>
public sealed class AdKeywordIdea
{
    [JsonPropertyName("keyword")]
    public string Keyword { get; set; } = "";

    /// <summary>Average monthly Google searches over the last 12 months.</summary>
    [JsonPropertyName("avg_monthly_searches")]
    public long AvgMonthlySearches { get; set; }

    /// <summary>"low", "medium", "high" or "unspecified".</summary>
    [JsonPropertyName("competition")]
    public string? Competition { get; set; }

    /// <summary>0 to 100.</summary>
    [JsonPropertyName("competition_index")]
    public int? CompetitionIndex { get; set; }

    /// <summary>Top-of-page bid range in the ad account's currency (major units).</summary>
    [JsonPropertyName("low_top_of_page_bid")]
    public decimal? LowTopOfPageBid { get; set; }

    [JsonPropertyName("high_top_of_page_bid")]
    public decimal? HighTopOfPageBid { get; set; }

    /// <summary>Only on <see cref="AdsResource.KeywordMetricsAsync"/>.</summary>
    [JsonPropertyName("close_variants")]
    public List<string>? CloseVariants { get; set; }
}

/// <summary><c>data</c> of <see cref="AdsResource.ResearchKeywordsAsync"/> (<see cref="Ideas"/>) and <see cref="AdsResource.KeywordMetricsAsync"/> (<see cref="Keywords"/>).</summary>
public sealed class AdKeywordResult
{
    [JsonPropertyName("ad_account_id")]
    public string? AdAccountId { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("cached")]
    public bool Cached { get; set; }

    [JsonPropertyName("ideas")]
    public List<AdKeywordIdea>? Ideas { get; set; }

    [JsonPropertyName("keywords")]
    public List<AdKeywordIdea>? Keywords { get; set; }
}

// ─── "Create an ad" drafts ──────────────────────────────────────────────────

/// <summary>A problem that stops a network or an ad of a draft from starting.</summary>
public sealed class AdDraftIssue
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = "";

    [JsonPropertyName("code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";
}

/// <summary>Readiness of one ad of a draft.</summary>
public sealed class AdDraftAdReadiness
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("ready")]
    public bool Ready { get; set; }

    [JsonPropertyName("issues")]
    public List<AdDraftIssue> Issues { get; set; } = new();
}

/// <summary>What still stops a network of a draft.</summary>
public sealed class AdDraftReadiness
{
    [JsonPropertyName("ready")]
    public bool Ready { get; set; }

    [JsonPropertyName("issues")]
    public List<AdDraftIssue> Issues { get; set; } = new();

    [JsonPropertyName("ads")]
    public List<AdDraftAdReadiness> Ads { get; set; } = new();
}

/// <summary>The ad account of one network of a draft.</summary>
[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public sealed class AdDraftAccount
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("billing_ok")]
    public bool? BillingOk { get; set; }

    [JsonPropertyName("needs_reauth")]
    public bool NeedsReauth { get; set; }
}

/// <summary>One network's campaign in a draft group.</summary>
[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public sealed class AdDraftCampaign
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    /// <summary>The network never started: a save changes it, a launch starts it.</summary>
    [JsonPropertyName("editable")]
    public bool? Editable { get; set; }

    [JsonPropertyName("external_campaign_id")]
    public string? ExternalCampaignId { get; set; }

    [JsonPropertyName("budget")]
    public AdBudget? Budget { get; set; }

    [JsonPropertyName("ad_account")]
    public AdDraftAccount? AdAccount { get; set; }

    [JsonPropertyName("errors")]
    public string? Errors { get; set; }

    [JsonPropertyName("readiness")]
    public AdDraftReadiness? Readiness { get; set; }

    /// <summary>The settings of this network (the draft's <c>platforms[].settings</c>).</summary>
    [JsonPropertyName("settings")]
    public AdNetworkSettings? Settings { get; set; }
}

/// <summary>A "Create an ad" draft group.</summary>
public sealed class AdDraft
{
    [JsonPropertyName("group_id")]
    public string GroupId { get; set; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("objective")]
    public string? Objective { get; set; }

    /// <summary>"draft", or "launched" once at least one network started.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    /// <summary>At least one network never started: a launch can start it.</summary>
    [JsonPropertyName("can_launch")]
    public bool? CanLaunch { get; set; }

    /// <summary>The daily amount per network.</summary>
    [JsonPropertyName("budget")]
    public AdDraftBudget? Budget { get; set; }

    [JsonPropertyName("schedule")]
    public AdDraftSchedule? Schedule { get; set; }

    [JsonPropertyName("targeting")]
    public AdDraftTargeting? Targeting { get; set; }

    [JsonPropertyName("ads")]
    public List<AdDraftAd> Ads { get; set; } = new();

    [JsonPropertyName("campaigns")]
    public List<AdDraftCampaign> Campaigns { get; set; } = new();

    /// <summary>The builder in the dashboard, where the person checks and starts the ad: hand it over.</summary>
    [JsonPropertyName("review_url")]
    public string? ReviewUrl { get; set; }
}

/// <summary><c>{ "data": draft }</c>: create, get, update and attach account.</summary>
public sealed class AdDraftResponse
{
    [JsonPropertyName("data")]
    public AdDraft Data { get; set; } = new();
}

/// <summary>
/// The outcome of one network of a launch. The API sends <c>campaign_id</c> as
/// a string (64-bit ids); it is read into a number here. <c>code</c>
/// "start_in_app" = the person starts that network in the app.
/// </summary>
[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public sealed class AdDraftLaunchNetworkResult
{
    [JsonPropertyName("campaign_id")]
    public long CampaignId { get; set; }

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = "";

    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary><c>data</c> of a launch: the draft after it, and one result per network.</summary>
public sealed class AdDraftLaunchResult
{
    [JsonPropertyName("group")]
    public AdDraft Group { get; set; } = new();

    [JsonPropertyName("results")]
    public List<AdDraftLaunchNetworkResult> Results { get; set; } = new();
}

/// <summary><c>{ "data": { group, results } }</c> of <see cref="AdDraftsResource.LaunchAsync(string, decimal, CancellationToken)"/>.</summary>
public sealed class AdDraftLaunchResponse
{
    [JsonPropertyName("data")]
    public AdDraftLaunchResult Data { get; set; } = new();
}

// ─── Ads API parity (2026-11-05) ────────────────────────────────────────────

/// <summary>
/// <c>data</c> of <see cref="AdsResource.CapabilitiesAsync"/>. <see cref="Networks"/>
/// stays raw JSON (goals, media rules, text limits, buttons, minimum budgets,
/// <c>bid_strategies</c>, <c>settings</c> and <c>ad_fields</c> per network).
/// </summary>
public sealed class AdCapabilities
{
    [JsonPropertyName("networks")]
    public List<JsonElement> Networks { get; set; } = new();

    [JsonPropertyName("goals")]
    public List<string>? Goals { get; set; }

    /// <summary>Goals through the API and MCP only: "lead_generation", "conversions", "messages", "calls".</summary>
    [JsonPropertyName("api_goals")]
    public List<string>? ApiGoals { get; set; }

    [JsonPropertyName("ctas")]
    public List<JsonElement>? Ctas { get; set; }

    /// <summary>The networks open to your organisation for new ads.</summary>
    [JsonPropertyName("create_networks")]
    public List<string>? CreateNetworks { get; set; }

    /// <summary>Per network: "live", "preview" or "coming_soon".</summary>
    [JsonPropertyName("network_states")]
    public Dictionary<string, string>? NetworkStates { get; set; }

    /// <summary>
    /// Networks where every API caller stops at the draft, also a key with
    /// <c>ads:spend</c> (Google): a start answers <c>403 start_in_app</c> and a
    /// person starts and changes those ads in the app.
    /// </summary>
    [JsonPropertyName("api_draft_only")]
    public List<string>? ApiDraftOnly { get; set; }

    /// <summary>
    /// Networks where the API may not even pause or delete a campaign that
    /// reached the network (Google): those calls answer <c>403 start_in_app</c>.
    /// </summary>
    [JsonPropertyName("api_stop_in_app")]
    public List<string>? ApiStopInApp { get; set; }

    /// <summary>
    /// Per network, the features its API offers here ("tree", "object_status",
    /// "audiences", "lead_forms", "insights", "reach_frequency", "search_terms"
    /// ...). A call for a feature a network lacks answers
    /// <c>400 not_supported_on_network</c>.
    /// </summary>
    [JsonPropertyName("features")]
    public Dictionary<string, List<string>>? Features { get; set; }

    /// <summary>A readable label per feature key.</summary>
    [JsonPropertyName("feature_labels")]
    public Dictionary<string, string>? FeatureLabels { get; set; }

    /// <summary>Per network, the types <see cref="AdTargetingResource.SearchAsync"/> takes and the keys of <see cref="AdNetworkTargeting.Include"/>.</summary>
    [JsonPropertyName("targeting_types")]
    public Dictionary<string, List<string>>? TargetingTypes { get; set; }
}

/// <summary>Numbers in major currency units; null = the network does not report it. <c>Ctr</c> is a percent.</summary>
public sealed class AdMetrics
{
    [JsonPropertyName("spend")] public decimal? Spend { get; set; }
    [JsonPropertyName("impressions")] public double? Impressions { get; set; }
    [JsonPropertyName("clicks")] public double? Clicks { get; set; }
    [JsonPropertyName("ctr")] public double? Ctr { get; set; }
    [JsonPropertyName("cpc")] public decimal? Cpc { get; set; }
    [JsonPropertyName("cpm")] public decimal? Cpm { get; set; }
    [JsonPropertyName("reach")] public double? Reach { get; set; }
    [JsonPropertyName("frequency")] public double? Frequency { get; set; }
    [JsonPropertyName("conversions")] public double? Conversions { get; set; }
    [JsonPropertyName("conversion_value")] public decimal? ConversionValue { get; set; }
    [JsonPropertyName("video_views")] public double? VideoViews { get; set; }
    [JsonPropertyName("leads")] public double? Leads { get; set; }
    [JsonPropertyName("engagements")] public double? Engagements { get; set; }
}

/// <summary>One campaign, ad set or ad as the network has it (<see cref="AdObjectsResource.TreeAsync"/>).</summary>
public sealed class AdTreeNode
{
    /// <summary>"campaign", "ad_set" or "ad".</summary>
    [JsonPropertyName("level")]
    public string Level { get; set; } = "";

    /// <summary>The network's own id.</summary>
    [JsonPropertyName("external_id")]
    public string ExternalId { get; set; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>"active", "paused", "pending_review", "rejected", "error", "completed", "archived", "deleted" or "draft".</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("budget")]
    public AdBudget? Budget { get; set; }

    [JsonPropertyName("objective")]
    public string? Objective { get; set; }

    [JsonPropertyName("start_at")]
    public string? StartAt { get; set; }

    [JsonPropertyName("end_at")]
    public string? EndAt { get; set; }

    [JsonPropertyName("metrics")]
    public AdMetrics? Metrics { get; set; }

    /// <summary>The network's review of an ad: <c>status</c> and <c>reasons</c>.</summary>
    [JsonPropertyName("review")]
    public JsonElement? Review { get; set; }

    /// <summary>"omnisocials" or "external" (made outside OmniSocials).</summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>The OmniSocials campaign id when <see cref="Source"/> is "omnisocials".</summary>
    [JsonPropertyName("campaign_id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long? CampaignId { get; set; }

    [JsonPropertyName("children")]
    public List<AdTreeNode>? Children { get; set; }
}

// ─── Webhook event ──────────────────────────────────────────────────────────

/// <summary>
/// <c>data</c> of an <c>ad.status_changed</c> webhook delivery: the campaign, not
/// a post. It fires when a campaign changes status, for example "pending_review"
/// to "active" (the network's review passed and spend started) or to "rejected".
/// After <see cref="WebhookSignature.Verify(string, string, string, int)"/>,
/// check <c>type</c>, then
/// <c>webhookEvent.GetProperty("data").Deserialize&lt;AdStatusChangedEventData&gt;()</c>.
/// Subscribing to it needs the <c>ads:read</c> scope on the key and
/// <c>access_ads</c> for its person (<c>403 insufficient_scope</c> /
/// <c>insufficient_permission</c>).
/// </summary>
[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public sealed class AdStatusChangedEventData
{
    [JsonPropertyName("campaign_id")]
    public long CampaignId { get; set; }

    [JsonPropertyName("workspace_id")]
    public long? WorkspaceId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("platform")]
    public string? Platform { get; set; }

    /// <summary>"campaign" or "boost".</summary>
    [JsonPropertyName("campaign_type")]
    public string? CampaignType { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("previous_status")]
    public string? PreviousStatus { get; set; }

    [JsonPropertyName("external_campaign_id")]
    public string? ExternalCampaignId { get; set; }

    [JsonPropertyName("budget")]
    public AdBudget? Budget { get; set; }
}
