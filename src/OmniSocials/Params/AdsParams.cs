using System.Text.Json.Serialization;

namespace OmniSocials;

// Request bodies for the ads endpoints. Amounts are in MAJOR currency units
// (10 = 10 EUR) in the ad account's currency. Null properties are omitted.

/// <summary>A campaign budget.</summary>
public sealed class AdBudget
{
    /// <summary>"daily" (default) or "lifetime" (the whole schedule).</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>Major units (10 = 10 EUR). Network minimums apply (for example TikTok 20 a day).</summary>
    [JsonPropertyName("amount")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Amount { get; set; }

    /// <summary>3-letter ISO code; must match the ad account's currency.</summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "";
}

/// <summary>When a boost or campaign runs. Both are ISO 8601 and optional.</summary>
public sealed class AdSchedule
{
    [JsonPropertyName("start_at")]
    public string? StartAt { get; set; }

    [JsonPropertyName("end_at")]
    public string? EndAt { get; set; }
}

/// <summary>Query parameters for <c>GET /ads/campaigns</c>. Every property is optional.</summary>
public sealed class AdCampaignListParams
{
    /// <summary>"draft", "pending_review", "active", "rejected", "paused", "completed" or "error".</summary>
    public string? Status { get; set; }

    /// <summary>"meta", "pinterest", "tiktok", "linkedin", "x" or "google".</summary>
    public string? Platform { get; set; }

    /// <summary>"campaign" or "boost".</summary>
    public string? CampaignType { get; set; }

    /// <summary>1 to 100, default 25.</summary>
    public int? Limit { get; set; }

    public int? Offset { get; set; }
}

/// <summary>Shared fields of <see cref="AdBoostParams"/> and <see cref="AdBoostMultiParams"/>.</summary>
public abstract class AdBoostParamsBase
{
    /// <summary>OmniSocials post id of a published post.</summary>
    [JsonPropertyName("post_id")]
    public long PostId { get; set; }

    [JsonPropertyName("budget")]
    public AdBudget Budget { get; set; } = new();

    /// <summary>"engagement", "traffic", "awareness", "video_views", "lead_generation" or "conversions".</summary>
    [JsonPropertyName("objective")]
    public string? Objective { get; set; }

    /// <summary>Unified targeting: <c>["countries"] = new[] { "NL" }</c>, ages, genders. <c>["raw_targeting"]</c> passes a native spec through.</summary>
    [JsonPropertyName("targeting")]
    public Dictionary<string, object?>? Targeting { get; set; }

    [JsonPropertyName("schedule")]
    public AdSchedule? Schedule { get; set; }

    /// <summary>
    /// Network-specific creative fields. TikTok (Spark Ads) needs
    /// <c>["spark_auth_code"]</c>: the post owner makes it in the TikTok app
    /// (video, "...", Ad settings, Ad authorization). Google needs
    /// <c>["headlines"]</c> (1 to 5 of 40 characters), <c>["descriptions"]</c>
    /// (1 to 5 of 90 characters) and <c>["logo_url"]</c> (a square logo). A TikTok
    /// traffic boost needs <c>["link"]</c>. Meta, Pinterest, X and LinkedIn
    /// boosts need none.
    /// </summary>
    [JsonPropertyName("creative")]
    public Dictionary<string, object?>? Creative { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>Body for <c>POST /ads/boost</c>: a DRAFT boost (no spend until activated).</summary>
public sealed class AdBoostParams : AdBoostParamsBase
{
    /// <summary>OmniSocials ad account id (from <see cref="AdsResource.ListAccountsAsync"/>, picked in the dashboard).</summary>
    [JsonPropertyName("ad_account_id")]
    public long AdAccountId { get; set; }
}

/// <summary>Body for <c>POST /ads/boost-multi</c>: one DRAFT boost per ad account.</summary>
public sealed class AdBoostMultiParams : AdBoostParamsBase
{
    /// <summary>Up to 10 ad accounts, each with the SAME budget: the total is amount x accounts.</summary>
    [JsonPropertyName("ad_account_ids")]
    public IEnumerable<long> AdAccountIds { get; set; } = Array.Empty<long>();
}

/// <summary>
/// Body for <c>PATCH /ads/campaigns/:id</c>. Null properties are omitted. A draft
/// changes with <c>ads:write</c>. A campaign that reached the network changes
/// only with a key that has the opt-in <c>ads:spend</c> scope (never on Google);
/// without it the call answers <c>403 start_in_app</c>.
/// </summary>
public sealed class AdCampaignUpdateParams
{
    /// <summary>
    /// New settings; only on a campaign that never started (after the start
    /// <c>400 settings_locked</c>: make a copy with <see cref="AdsResource.DuplicateAsync"/>).
    /// </summary>
    [JsonPropertyName("settings")]
    public AdNetworkSettings? Settings { get; set; }

    /// <summary>A new goal (also the API goals "lead_generation", "conversions", "messages", "calls").</summary>
    [JsonPropertyName("objective")]
    public string? Objective { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("budget")]
    public AdBudget? Budget { get; set; }

    /// <summary>
    /// Required to RAISE the budget of a running campaign (a key with
    /// <c>ads:spend</c>, outside <c>api_draft_only</c>): the new amount again,
    /// after the person agreed to it.
    /// </summary>
    [JsonPropertyName("confirm_budget_amount")]
    public decimal? ConfirmBudgetAmount { get; set; }

    [JsonPropertyName("targeting")]
    public Dictionary<string, object?>? Targeting { get; set; }

    [JsonPropertyName("schedule")]
    public AdSchedule? Schedule { get; set; }
}

/// <summary>Body for <c>POST /ads/research/keywords</c>. Give <see cref="Keywords"/>, <see cref="Url"/> or both.</summary>
public sealed class AdKeywordResearchParams
{
    /// <summary>Seed keywords, up to 20.</summary>
    [JsonPropertyName("keywords")]
    public IEnumerable<string>? Keywords { get; set; }

    /// <summary>A landing page or homepage to take ideas from.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>ISO 3166-1 alpha-2, default "US".</summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>ISO 639-1, default "en".</summary>
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    /// <summary>1 to 500, default 100.</summary>
    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    /// <summary>Only needed when the workspace has more than one Google Ads account.</summary>
    [JsonPropertyName("ad_account_id")]
    public string? AdAccountId { get; set; }
}

/// <summary>Body for <c>POST /ads/research/keywords/metrics</c>.</summary>
public sealed class AdKeywordMetricsParams
{
    /// <summary>The exact keywords to measure.</summary>
    [JsonPropertyName("keywords")]
    public IEnumerable<string> Keywords { get; set; } = Array.Empty<string>();

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("ad_account_id")]
    public string? AdAccountId { get; set; }
}

// ─── "Create an ad" drafts ──────────────────────────────────────────────────

/// <summary>
/// Body of <c>POST /ads/drafts</c> and <c>PUT /ads/drafts/:group_id</c> (the full
/// draft): one campaign per network in <see cref="Platforms"/>, each with the
/// same <see cref="Ads"/>. Read <see cref="AdsResource.CapabilitiesAsync"/> first
/// for what each network accepts.
/// </summary>
public sealed class AdDraftParams
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// "traffic", "engagement", "awareness" or "video_views", or through the API
    /// only "lead_generation", "conversions", "messages" or "calls" (the
    /// capabilities say per network what each <c>needs</c>: a lead form, a
    /// conversion, messaging or a phone number in <see cref="AdDraftPlatform.Settings"/>).
    /// </summary>
    [JsonPropertyName("objective")]
    public string Objective { get; set; } = "traffic";

    /// <summary>
    /// Network keys (<c>"meta"</c>, <c>"linkedin"</c>, ...) or
    /// <see cref="AdDraftPlatform"/> objects for a budget, ad account or
    /// settings per network.
    /// </summary>
    [JsonPropertyName("platforms")]
    public IEnumerable<object> Platforms { get; set; } = Array.Empty<object>();

    /// <summary>The daily amount per network.</summary>
    [JsonPropertyName("budget")]
    public AdDraftBudget Budget { get; set; } = new();

    [JsonPropertyName("schedule")]
    public AdDraftSchedule? Schedule { get; set; }

    [JsonPropertyName("targeting")]
    public AdDraftTargeting Targeting { get; set; } = new();

    [JsonPropertyName("ads")]
    public IEnumerable<AdDraftAd> Ads { get; set; } = Array.Empty<AdDraftAd>();
}

/// <summary>One network of a draft with its own daily amount or ad account.</summary>
public sealed class AdDraftPlatform
{
    [JsonPropertyName("platform")]
    public string Platform { get; set; } = "";

    [JsonPropertyName("budget_amount")]
    public decimal? BudgetAmount { get; set; }

    [JsonPropertyName("ad_account_id")]
    public long? AdAccountId { get; set; }

    /// <summary>The settings of this network. Left out on an update = keep the stored settings.</summary>
    [JsonPropertyName("settings")]
    public AdNetworkSettings? Settings { get; set; }
}

/// <summary>The daily amount per network of a draft, in major units.</summary>
public sealed class AdDraftBudget
{
    [JsonPropertyName("amount")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? Amount { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
}

/// <summary>How long a draft runs once it started.</summary>
public sealed class AdDraftSchedule
{
    /// <summary>1 to 90 days. Null (always sent) = runs until it is stopped.</summary>
    [JsonPropertyName("duration_days")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public int? DurationDays { get; set; }
}

/// <summary>Who sees the ad.</summary>
public sealed class AdDraftTargeting
{
    /// <summary>ISO country codes.</summary>
    [JsonPropertyName("countries")]
    public IEnumerable<string> Countries { get; set; } = Array.Empty<string>();

    /// <summary>True lets the network find the audience (no age or gender limits).</summary>
    [JsonPropertyName("automatic_audience")]
    public bool? AutomaticAudience { get; set; }

    [JsonPropertyName("age_min")]
    public int? AgeMin { get; set; }

    [JsonPropertyName("age_max")]
    public int? AgeMax { get; set; }

    /// <summary>"male" and/or "female".</summary>
    [JsonPropertyName("genders")]
    public IEnumerable<string>? Genders { get; set; }

    /// <summary>"automatic" or "feeds".</summary>
    [JsonPropertyName("placements")]
    public string? Placements { get; set; }

    /// <summary>
    /// The network's own targeting per network key, for example
    /// <c>["meta"] = new AdNetworkTargeting { Include = ... }</c>, with ids from
    /// <see cref="AdTargetingResource.SearchAsync"/>. A save without it keeps
    /// each network's stored targeting.
    /// </summary>
    [JsonPropertyName("networks")]
    public Dictionary<string, AdNetworkTargeting>? Networks { get; set; }

    /// <summary>LinkedIn's EU statement: "NOT_POLITICAL" or "POLITICAL".</summary>
    [JsonPropertyName("political_intent")]
    public string? PoliticalIntent { get; set; }

    /// <summary>Meta's special ad categories (housing, employment, financial products, ...).</summary>
    [JsonPropertyName("special_ad_categories")]
    public IEnumerable<string>? SpecialAdCategories { get; set; }

    /// <summary>The countries of the special ad categories.</summary>
    [JsonPropertyName("special_ad_category_country")]
    public IEnumerable<string>? SpecialAdCategoryCountry { get; set; }

    /// <summary>Meta's verified payer per regional category.</summary>
    [JsonPropertyName("regional_identities")]
    public Dictionary<string, object?>? RegionalIdentities { get; set; }
}

/// <summary>One ad of a draft: written once, mapped onto every network.</summary>
public sealed class AdDraftAd
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("media")]
    public IEnumerable<AdMediaFile> Media { get; set; } = Array.Empty<AdMediaFile>();

    [JsonPropertyName("primary_text")]
    public string PrimaryText { get; set; } = "";

    /// <summary>A second text: the network tests A against B (a second ad).</summary>
    [JsonPropertyName("primary_text_b")]
    public string? PrimaryTextB { get; set; }

    [JsonPropertyName("headline")]
    public string? Headline { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>A button key from the capabilities' <c>ctas</c>, for example "shop_now".</summary>
    [JsonPropertyName("cta")]
    public string? Cta { get; set; }

    [JsonPropertyName("link")]
    public string Link { get; set; } = "";

    /// <summary>Per network text that replaces the shared text: <c>["linkedin"] = new { primary_text = "..." }</c>.</summary>
    [JsonPropertyName("overrides")]
    public Dictionary<string, object?>? Overrides { get; set; }

    /// <summary>
    /// Network extras per network key, for example
    /// <c>["google"] = new { business_name = "...", logo = new { url = "..." } }</c>.
    /// Meta: <c>lead_form_id</c>, <c>primary_texts</c>, <c>headlines</c>,
    /// <c>creative_features</c>, <c>media_ref</c> (<c>image_hash</c> or
    /// <c>video_id</c> from <see cref="AdMediaResource.UploadAsync"/>),
    /// <c>partnership</c> (<c>sponsor_id</c>). TikTok: <c>spark_auth_code</c>,
    /// <c>lead_form_id</c>, <c>media_ref</c>. LinkedIn and Pinterest:
    /// <c>lead_form_id</c>. X: <c>media_ref</c>. Google: <c>business_name</c>,
    /// <c>logo</c>, <c>responsive_search_ad</c>. The capabilities list the keys
    /// per network as <c>ad_fields</c>.
    /// </summary>
    [JsonPropertyName("network")]
    public Dictionary<string, object?>? Network { get; set; }

    /// <summary>
    /// Meta only, optional: a second picture or video for Stories and Reels,
    /// upright 9:16 (1080 x 1920), the same kind as <see cref="Media"/>. Without
    /// it Meta fits the main file. Not for the engagement goal.
    /// </summary>
    [JsonPropertyName("placement_media")]
    public AdPlacementMedia? PlacementMedia { get; set; }
}

/// <summary>A picture or video of an ad.</summary>
public sealed class AdMediaFile
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    /// <summary>"image" or "video".</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "image";

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    /// <summary>Seconds, for a video.</summary>
    [JsonPropertyName("duration")]
    public double? Duration { get; set; }

    /// <summary>Cover picture of a video.</summary>
    [JsonPropertyName("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }
}

/// <summary>Files per placement (Meta).</summary>
public sealed class AdPlacementMedia
{
    /// <summary>Upright 9:16 file for Stories and Reels.</summary>
    [JsonPropertyName("stories_reels")]
    public AdMediaFile? StoriesReels { get; set; }
}

/// <summary>
/// Body for <c>POST /ads/drafts/:group_id/launch</c>: give
/// <see cref="ConfirmBudgetAmount"/> (one daily amount for every network) or
/// <see cref="ConfirmBudgetAmounts"/> (per campaign id). Each must equal that
/// campaign's daily amount, else <c>400 budget_confirmation_required</c> and
/// nothing is written. Needs a key with <c>ads:spend</c>: without it the call
/// answers <c>403 start_in_app</c> with the draft's page. A network in
/// <c>api_draft_only</c> (Google) is skipped with code <c>start_in_app</c>.
/// </summary>
public sealed class AdDraftLaunchParams
{
    [JsonPropertyName("confirm_budget_amount")]
    public decimal? ConfirmBudgetAmount { get; set; }

    [JsonPropertyName("confirm_budget_amounts")]
    public IDictionary<string, decimal>? ConfirmBudgetAmounts { get; set; }
}

// ─── Ads API parity (2026-11-05): network targeting and settings ────────────

/// <summary>
/// The targeting of one network (<see cref="AdDraftTargeting.Networks"/> of a
/// draft, <c>targeting["network"]</c> of a campaign). The keys of
/// <see cref="Include"/> and <see cref="Exclude"/> are the network's targeting
/// types (the capabilities' <c>targeting_types</c>: "interest", "location",
/// "job_title" ...). <c>location</c> entries replace the countries on that network.
/// </summary>
public sealed class AdNetworkTargeting
{
    [JsonPropertyName("include")]
    public Dictionary<string, List<AdTargetingItem>>? Include { get; set; }

    [JsonPropertyName("exclude")]
    public Dictionary<string, List<AdTargetingItem>>? Exclude { get; set; }

    /// <summary>The network's own custom audience ids of the same ad account.</summary>
    [JsonPropertyName("audiences")]
    public AdAudienceSelection? Audiences { get; set; }
}

/// <summary>One targeting entry: the <c>id</c> from <see cref="AdTargetingResource.SearchAsync"/> of that network.</summary>
public sealed class AdTargetingItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>A radius around a place, 1 to 80 km (location only).</summary>
    [JsonPropertyName("radius_km")]
    public double? RadiusKm { get; set; }

    [JsonPropertyName("kind")]
    public string? Kind { get; set; }
}

/// <summary>Custom audiences to include or exclude.</summary>
public sealed class AdAudienceSelection
{
    [JsonPropertyName("include")]
    public List<AdTargetingItem>? Include { get; set; }

    [JsonPropertyName("exclude")]
    public List<AdTargetingItem>? Exclude { get; set; }
}

/// <summary>
/// The settings of one network of a campaign (<c>settings</c>) or a draft
/// (<see cref="AdDraftPlatform.Settings"/>). The capabilities list per network
/// which keys its module reads (<c>settings</c>) and the
/// <c>bid_strategies</c> it maps.
/// </summary>
public sealed class AdNetworkSettings
{
    [JsonPropertyName("optimization")]
    public AdOptimizationSettings? Optimization { get; set; }

    /// <summary>Meta, goal "messages".</summary>
    [JsonPropertyName("messaging")]
    public AdMessagingSettings? Messaging { get; set; }

    /// <summary>Meta, goal "calls".</summary>
    [JsonPropertyName("calls")]
    public AdCallSettings? Calls { get; set; }

    /// <summary>The network's lead form id (<c>external_id</c> from <see cref="AdLeadFormsResource.ListAsync"/>), goal "lead_generation".</summary>
    [JsonPropertyName("lead_form_id")]
    public string? LeadFormId { get; set; }

    /// <summary>Google: <c>kind</c> ("demand_gen", "search", "performance_max") and its <c>search</c> / <c>performance_max</c> settings.</summary>
    [JsonPropertyName("google")]
    public AdGoogleSettings? Google { get; set; }

    /// <summary>Meta: <c>budget_level</c> "ad_set" (default) or "campaign".</summary>
    [JsonPropertyName("meta")]
    public AdMetaSettings? Meta { get; set; }
}

/// <summary>What the network optimises for, how it bids and how it counts.</summary>
public sealed class AdOptimizationSettings
{
    [JsonPropertyName("conversion")]
    public AdConversionTarget? Conversion { get; set; }

    [JsonPropertyName("bidding")]
    public AdBiddingSettings? Bidding { get; set; }

    [JsonPropertyName("attribution")]
    public AdAttributionSettings? Attribution { get; set; }
}

/// <summary>The conversion to optimise for (goal "conversions").</summary>
public sealed class AdConversionTarget
{
    /// <summary>A pixel, dataset or conversion tag from <see cref="AdConversionsResource.DestinationsAsync"/>.</summary>
    [JsonPropertyName("destination_id")]
    public string? DestinationId { get; set; }

    /// <summary>For example "PURCHASE" or "LEAD" (<see cref="AdConversionsResource.GoalsAsync"/>).</summary>
    [JsonPropertyName("event")]
    public string? Event { get; set; }

    [JsonPropertyName("custom_conversion_id")]
    public string? CustomConversionId { get; set; }
}

/// <summary>How the network bids.</summary>
public sealed class AdBiddingSettings
{
    /// <summary>"lowest_cost", "cost_cap", "bid_cap", "min_roas" or "target_cost".</summary>
    [JsonPropertyName("strategy")]
    public string? Strategy { get; set; }

    /// <summary>The cap or target in major units of the account currency.</summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>"min_roas": 1.5 = 150%.</summary>
    [JsonPropertyName("roas_floor")]
    public decimal? RoasFloor { get; set; }
}

/// <summary>The attribution window.</summary>
public sealed class AdAttributionSettings
{
    /// <summary>1, 7 or 28.</summary>
    [JsonPropertyName("click_days")]
    public int? ClickDays { get; set; }

    /// <summary>0 or 1.</summary>
    [JsonPropertyName("view_days")]
    public int? ViewDays { get; set; }
}

/// <summary>Where a "messages" ad opens a chat (Meta).</summary>
public sealed class AdMessagingSettings
{
    /// <summary>"messenger", "instagram_direct" or "whatsapp".</summary>
    [JsonPropertyName("destination")]
    public string? Destination { get; set; }

    [JsonPropertyName("whatsapp_phone_number")]
    public string? WhatsappPhoneNumber { get; set; }

    [JsonPropertyName("welcome_message")]
    public AdWelcomeMessage? WelcomeMessage { get; set; }

    /// <summary>A DM automation that answers the first message from the ad.</summary>
    [JsonPropertyName("automation_id")]
    public long? AutomationId { get; set; }
}

/// <summary>The first message of a chat that an ad opens.</summary>
public sealed class AdWelcomeMessage
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("quick_replies")]
    public IEnumerable<string>? QuickReplies { get; set; }
}

/// <summary>The number a "calls" ad dials (Meta).</summary>
public sealed class AdCallSettings
{
    /// <summary>E.164, from a country the ad targets.</summary>
    [JsonPropertyName("phone_number")]
    public string? PhoneNumber { get; set; }
}

/// <summary>Google campaign kind and its own settings.</summary>
public sealed class AdGoogleSettings
{
    /// <summary>"demand_gen", "search" or "performance_max".</summary>
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("search")]
    public Dictionary<string, object?>? Search { get; set; }

    [JsonPropertyName("performance_max")]
    public Dictionary<string, object?>? PerformanceMax { get; set; }
}

/// <summary>Meta campaign settings.</summary>
public sealed class AdMetaSettings
{
    /// <summary>"ad_set" (default) or "campaign" (Advantage campaign budget).</summary>
    [JsonPropertyName("budget_level")]
    public string? BudgetLevel { get; set; }
}

// ─── Ads API parity: request bodies (optional; any object works) ───────────

/// <summary>Body for <see cref="AdAudiencesResource.CreateAsync"/>.</summary>
public sealed class AdAudienceCreateParams
{
    [JsonPropertyName("ad_account_id")]
    public long AdAccountId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>"customer_list", "lookalike" or "company_list".</summary>
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("lookalike")]
    public AdLookalikeParams? Lookalike { get; set; }

    /// <summary>
    /// Meta customer lists: where the people come from, "user_provided",
    /// "partner_provided" or "both", as the person says. Without it Meta reads
    /// "user_provided".
    /// </summary>
    [JsonPropertyName("customer_file_source")]
    public string? CustomerFileSource { get; set; }

    /// <summary>The first members of a customer list: the column names (see <see cref="AdAudienceMembersParams"/>). Pinterest makes a list only together with its first people.</summary>
    [JsonPropertyName("schema")]
    public IEnumerable<string>? Schema { get; set; }

    /// <summary>The first members: one list of raw values per person, in the order of <see cref="Schema"/>.</summary>
    [JsonPropertyName("rows")]
    public IEnumerable<IEnumerable<string>>? Rows { get; set; }
}

/// <summary>A lookalike of an existing audience.</summary>
public sealed class AdLookalikeParams
{
    [JsonPropertyName("source_audience_id")]
    public string SourceAudienceId { get; set; } = "";

    [JsonPropertyName("country")]
    public string Country { get; set; } = "";

    /// <summary>0.01 to 0.2.</summary>
    [JsonPropertyName("ratio")]
    public decimal Ratio { get; set; }
}

/// <summary>
/// Body for <see cref="AdAudiencesResource.AddMembersAsync"/> and
/// <see cref="AdAudiencesResource.RemoveMembersAsync"/>. Send the raw values:
/// OmniSocials normalizes and SHA-256 hashes them before they leave.
/// </summary>
public sealed class AdAudienceMembersParams
{
    [JsonPropertyName("ad_account_id")]
    public long AdAccountId { get; set; }

    /// <summary>
    /// "email", "phone" (with the country code), "first_name", "last_name",
    /// "city", "state", "zip", "country", "external_id", "dob_year", "gender",
    /// "maid".
    /// </summary>
    [JsonPropertyName("schema")]
    public IEnumerable<string> Schema { get; set; } = Array.Empty<string>();

    /// <summary>At most 10,000 rows per call.</summary>
    [JsonPropertyName("rows")]
    public IEnumerable<IEnumerable<string>> Rows { get; set; } = Array.Empty<IEnumerable<string>>();
}

/// <summary>Body for <see cref="AdLeadFormsResource.CreateAsync"/>.</summary>
public sealed class AdLeadFormCreateParams
{
    [JsonPropertyName("ad_account_id")]
    public long AdAccountId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("privacy_policy_url")]
    public string PrivacyPolicyUrl { get; set; } = "";

    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    [JsonPropertyName("headline")]
    public string? Headline { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("questions")]
    public IEnumerable<AdLeadFormQuestion> Questions { get; set; } = Array.Empty<AdLeadFormQuestion>();

    [JsonPropertyName("thank_you")]
    public AdLeadFormThankYou ThankYou { get; set; } = new();

    /// <summary>
    /// The person accepts the network's lead ad terms (Pinterest refuses a form
    /// without it). Only send true after the person said yes.
    /// </summary>
    [JsonPropertyName("accept_terms")]
    public bool? AcceptTerms { get; set; }
}

/// <summary>One question of a lead form.</summary>
public sealed class AdLeadFormQuestion
{
    /// <summary>"email", "full_name", "first_name", "last_name", "phone", "company", "job_title", "city", "country", "zip" or "custom".</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("options")]
    public IEnumerable<string>? Options { get; set; }
}

/// <summary>The screen after a lead form is sent.</summary>
public sealed class AdLeadFormThankYou
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("button_text")]
    public string? ButtonText { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// Body for <see cref="AdBudgetSchedulesResource.CreateAsync"/>: NEW AD SPEND in
/// the period, so <see cref="ConfirmBudgetAmount"/> must come from the person's
/// yes.
/// </summary>
public sealed class AdBudgetScheduleParams
{
    [JsonPropertyName("start_at")]
    public string StartAt { get; set; } = "";

    [JsonPropertyName("end_at")]
    public string EndAt { get; set; } = "";

    [JsonPropertyName("budget_increase")]
    public AdBudgetIncrease BudgetIncrease { get; set; } = new();

    /// <summary>The daily total during the period (the daily budget plus the increase), under the OmniSocials daily cap.</summary>
    [JsonPropertyName("confirm_budget_amount")]
    public decimal ConfirmBudgetAmount { get; set; }
}

/// <summary>
/// A planned increase: "absolute" (<see cref="Value"/> = the extra per day, major
/// units) or "multiplier" (<see cref="Value"/> = a whole percentage on top, 50 =
/// 50% more).
/// </summary>
public sealed class AdBudgetIncrease
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "absolute";

    [JsonPropertyName("value")]
    public decimal Value { get; set; }
}

/// <summary>Body for <see cref="AdReachFrequencyResource.CreateAsync"/> (Meta). A prediction spends nothing.</summary>
public sealed class AdReachFrequencyParams
{
    [JsonPropertyName("ad_account_id")]
    public long AdAccountId { get; set; }

    /// <summary>"awareness", "engagement" or "video_views".</summary>
    [JsonPropertyName("objective")]
    public string Objective { get; set; } = "awareness";

    /// <summary>Exactly one country in <c>countries</c>; optional <c>age_min</c>, <c>age_max</c>, <c>genders</c>.</summary>
    [JsonPropertyName("targeting")]
    public Dictionary<string, object?> Targeting { get; set; } = new();

    [JsonPropertyName("start_at")]
    public string StartAt { get; set; } = "";

    [JsonPropertyName("end_at")]
    public string EndAt { get; set; } = "";

    /// <summary>The budget in major units; give this or <see cref="Reach"/>.</summary>
    [JsonPropertyName("budget")]
    public AdBudget? Budget { get; set; }

    /// <summary>People to reach, instead of a budget.</summary>
    [JsonPropertyName("reach")]
    public long? Reach { get; set; }

    [JsonPropertyName("frequency_cap")]
    public int? FrequencyCap { get; set; }

    [JsonPropertyName("frequency_interval_days")]
    public int? FrequencyIntervalDays { get; set; }
}
