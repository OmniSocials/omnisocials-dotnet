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

/// <summary>Body for <c>POST /ads/campaigns</c>: a DRAFT campaign without ads.</summary>
public sealed class AdCampaignCreateParams
{
    [JsonPropertyName("ad_account_id")]
    public long AdAccountId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("budget")]
    public AdBudget Budget { get; set; } = new();

    /// <summary>"traffic" (default), "engagement", "awareness", "video_views", "lead_generation" or "conversions".</summary>
    [JsonPropertyName("objective")]
    public string? Objective { get; set; }

    [JsonPropertyName("targeting")]
    public Dictionary<string, object?>? Targeting { get; set; }

    [JsonPropertyName("schedule")]
    public AdSchedule? Schedule { get; set; }
}

/// <summary>Body for <c>PATCH /ads/campaigns/:id</c>. Null properties are omitted.</summary>
public sealed class AdCampaignUpdateParams
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("budget")]
    public AdBudget? Budget { get; set; }

    /// <summary>
    /// Required to RAISE the budget of a running campaign: the new amount again,
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

    /// <summary>"traffic", "engagement", "awareness" or "video_views".</summary>
    [JsonPropertyName("objective")]
    public string Objective { get; set; } = "traffic";

    /// <summary>
    /// Network keys (<c>"meta"</c>, <c>"linkedin"</c>, ...) or
    /// <see cref="AdDraftPlatform"/> objects for a budget or ad account per network.
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

    /// <summary>Network extras, for example <c>["google"] = new { business_name = "...", logo = new { url = "..." } }</c>.</summary>
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
/// nothing is written.
/// </summary>
public sealed class AdDraftLaunchParams
{
    [JsonPropertyName("confirm_budget_amount")]
    public decimal? ConfirmBudgetAmount { get; set; }

    [JsonPropertyName("confirm_budget_amounts")]
    public IDictionary<string, decimal>? ConfirmBudgetAmounts { get; set; }
}
