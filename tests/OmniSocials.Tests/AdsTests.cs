using System.Net;
using System.Text.Json;
using Xunit;

namespace OmniSocials.Tests;

/// <summary>
/// Request path, method and body of the ads calls, the optional typed models,
/// and the money-call rule: a call that can start ad spend is never sent twice
/// after a 5xx, a connection error or a timeout.
/// </summary>
public class AdsTests
{
    private const string BadGateway = "{\"error\":{\"code\":\"bad_gateway\",\"message\":\"upstream\"}}";
    private static readonly Dictionary<string, string> RetryNow = new() { ["Retry-After"] = "0" };

    private static OmniSocialsClient CreateClient(StubHttpMessageHandler handler, TimeSpan? timeout = null)
        => new(new OmniSocialsOptions
        {
            ApiKey = "omsk_test_key",
            BaseUrl = "https://api.test.local/v1",
            MaxRetries = 2,
            Timeout = timeout ?? TimeSpan.FromSeconds(5),
            HttpMessageHandler = handler,
        });

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private static string Path(StubHttpMessageHandler handler, int index)
        => handler.Requests[index].RequestUri!.OriginalString["https://api.test.local/v1".Length..];

    private static AdDraftParams Draft() => new()
    {
        Name = "Autumn sale",
        Objective = "traffic",
        Platforms = new object[] { "meta", new AdDraftPlatform { Platform = "linkedin", BudgetAmount = 20 } },
        Budget = new AdDraftBudget { Amount = 10, Currency = "EUR" },
        Schedule = new AdDraftSchedule { DurationDays = 7 },
        Targeting = new AdDraftTargeting { Countries = new[] { "NL" }, AutomaticAudience = true },
        Ads = new[]
        {
            new AdDraftAd
            {
                Media = new[] { new AdMediaFile { Url = "https://example.com/sale.jpg", Type = "image" } },
                PrimaryText = "30% off everything this week",
                Cta = "shop_now",
                Link = "https://example.com/sale",
                PlacementMedia = new AdPlacementMedia
                {
                    StoriesReels = new AdMediaFile { Url = "https://example.com/sale-9x16.jpg", Type = "image" },
                },
            },
        },
    };

    [Fact]
    public async Task Draft_create_update_launch_and_activate_send_the_right_requests()
    {
        var handler = new StubHttpMessageHandler();
        for (var i = 0; i < 4; i++) handler.Enqueue(HttpStatusCode.OK, "{\"data\":{}}");
        using var client = CreateClient(handler);

        await client.Ads.Drafts.CreateAsync(Draft());
        await client.Ads.Drafts.UpdateAsync("g-1", Draft());
        await client.Ads.Drafts.LaunchAsync("g-1", 10m);
        await client.Ads.ActivateAsync(7, 25m);

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("/ads/drafts", Path(handler, 0));
        var created = Parse(handler.RequestBodies[0]!);
        Assert.Equal("traffic", created.GetProperty("objective").GetString());
        Assert.Equal("meta", created.GetProperty("platforms")[0].GetString());
        Assert.Equal(20, created.GetProperty("platforms")[1].GetProperty("budget_amount").GetInt32());
        Assert.Equal(10, created.GetProperty("budget").GetProperty("amount").GetInt32());
        Assert.Equal(7, created.GetProperty("schedule").GetProperty("duration_days").GetInt32());
        var ad = created.GetProperty("ads")[0];
        Assert.Equal("shop_now", ad.GetProperty("cta").GetString());
        Assert.Equal("https://example.com/sale-9x16.jpg",
            ad.GetProperty("placement_media").GetProperty("stories_reels").GetProperty("url").GetString());
        Assert.False(ad.TryGetProperty("headline", out _)); // nulls are left out

        Assert.Equal(HttpMethod.Put, handler.Requests[1].Method);
        Assert.Equal("/ads/drafts/g-1", Path(handler, 1));

        Assert.Equal(HttpMethod.Post, handler.Requests[2].Method);
        Assert.Equal("/ads/drafts/g-1/launch", Path(handler, 2));
        Assert.Equal("{\"confirm_budget_amount\":10}", handler.RequestBodies[2]);

        Assert.Equal(HttpMethod.Post, handler.Requests[3].Method);
        Assert.Equal("/ads/campaigns/7/activate", Path(handler, 3));
        Assert.Equal("{\"confirm_budget_amount\":25}", handler.RequestBodies[3]);
    }

    [Fact]
    public async Task Other_calls_send_the_right_requests()
    {
        var handler = new StubHttpMessageHandler();
        for (var i = 0; i < 7; i++) handler.Enqueue(HttpStatusCode.OK, "{\"data\":{}}");
        using var client = CreateClient(handler);

        await client.Ads.Drafts.GetAsync("g 1/x");
        await client.Ads.Drafts.AttachAccountAsync("g-1", "meta", 12);
        await client.Ads.Drafts.LaunchAsync("g-1", new AdDraftLaunchParams
        {
            ConfirmBudgetAmounts = new Dictionary<string, decimal> { ["41"] = 10m, ["42"] = 20m },
        });
        await client.Ads.ActivateMultiAsync(new long[] { 7, 8 }, 5m);
        await client.Ads.UpdateAsync(7, new AdCampaignUpdateParams { ConfirmBudgetAmount = 30m });
        await client.Ads.ResumeAsync(7);
        await client.Ads.ListAsync(new AdCampaignListParams { Status = "active", Limit = 10 });

        Assert.Equal("/ads/drafts/g%201%2Fx", Path(handler, 0));
        Assert.Equal("{\"platform\":\"meta\",\"ad_account_id\":12}", handler.RequestBodies[1]);
        Assert.Equal(20, Parse(handler.RequestBodies[2]!).GetProperty("confirm_budget_amounts").GetProperty("42").GetInt32());
        Assert.False(Parse(handler.RequestBodies[2]!).TryGetProperty("confirm_budget_amount", out _));
        Assert.Equal("/ads/campaigns/activate-multi", Path(handler, 3));
        Assert.Equal("{\"campaign_ids\":[7,8],\"confirm_budget_amount\":5}", handler.RequestBodies[3]);
        Assert.Equal(HttpMethod.Patch, handler.Requests[4].Method);
        Assert.Equal("/ads/campaigns/7", Path(handler, 4));
        Assert.Equal("/ads/campaigns/7/resume", Path(handler, 5));
        Assert.Null(handler.RequestBodies[5]);
        Assert.Equal("/ads/campaigns?status=active&limit=10", Path(handler, 6));
    }

    [Fact]
    public async Task Launch_and_activate_are_sent_once_after_a_502()
    {
        var handler = new StubHttpMessageHandler();
        for (var i = 0; i < 5; i++) handler.Enqueue(HttpStatusCode.BadGateway, BadGateway, headers: RetryNow);
        using var client = CreateClient(handler);

        await Assert.ThrowsAsync<ServerException>(() => client.Ads.Drafts.LaunchAsync("g-1", 10m));
        Assert.Single(handler.Requests); // a launch must not be retried after a 502

        await Assert.ThrowsAsync<ServerException>(() => client.Ads.ActivateAsync(7, 25m));
        Assert.Equal(2, handler.Requests.Count); // nor an activation

        await Assert.ThrowsAsync<ServerException>(() => client.Ads.ActivateMultiAsync(new long[] { 7, 8 }, 25m));
        await Assert.ThrowsAsync<ServerException>(() => client.Ads.ResumeAsync(7));
        await Assert.ThrowsAsync<ServerException>(
            () => client.Ads.UpdateAsync(7, new AdCampaignUpdateParams { ConfirmBudgetAmount = 30m }));
        Assert.Equal(5, handler.Requests.Count); // activate multi, resume and update: once each
    }

    [Fact]
    public async Task A_launch_is_sent_once_after_a_connection_error_or_a_timeout()
    {
        var handler = new StubHttpMessageHandler();
        handler.EnqueueConnectionError();
        handler.EnqueueDelayed(TimeSpan.FromSeconds(30), HttpStatusCode.OK, "{\"data\":{}}");
        using var client = CreateClient(handler, timeout: TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAsync<ApiConnectionException>(() => client.Ads.Drafts.LaunchAsync("g-1", 10m));
        Assert.Single(handler.Requests);

        var ex = await Assert.ThrowsAsync<ApiConnectionException>(() => client.Ads.ActivateAsync(7, 25m));
        Assert.Contains("timed out", ex.Message);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task A_read_is_retried_after_a_502()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.BadGateway, BadGateway, headers: RetryNow);
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"create_networks\":[\"meta\"]}}");
        using var client = CreateClient(handler);

        var caps = await client.Ads.CapabilitiesAsync();

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("meta", caps!.Value.GetProperty("data").GetProperty("create_networks")[0].GetString());
    }

    [Fact]
    public async Task A_launch_is_retried_after_a_429()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.TooManyRequests,
            "{\"error\":{\"code\":\"rate_limit_exceeded\",\"message\":\"slow down\"}}",
            headers: RetryNow);
        handler.Enqueue(HttpStatusCode.OK,
            "{\"data\":{\"group\":{\"group_id\":\"g-1\",\"status\":\"launched\",\"campaigns\":[]}," +
            "\"results\":[{\"campaign_id\":\"41\",\"platform\":\"meta\",\"ok\":true}," +
            "{\"campaign_id\":42,\"platform\":\"linkedin\",\"ok\":false,\"code\":\"billing_missing\",\"error\":\"Add a payment method\"}]}}");
        using var client = CreateClient(handler);

        var launched = await client.Ads.Drafts.LaunchAsync("g-1", 10m);

        Assert.Equal(2, handler.Requests.Count); // a 429 means nothing started
        var typed = launched!.Value.Deserialize<AdDraftLaunchResponse>()!;
        Assert.Equal("launched", typed.Data.Group.Status);
        Assert.Equal(41, typed.Data.Results[0].CampaignId); // the API may send ids as strings
        Assert.False(typed.Data.Results[1].Ok);
        Assert.Equal("billing_missing", typed.Data.Results[1].Code);
    }

    [Fact]
    public void Typed_models_read_a_draft_and_a_campaign()
    {
        var draft = Parse(
            "{\"data\":{\"group_id\":\"g-1\",\"name\":\"Autumn sale\",\"objective\":\"traffic\",\"status\":\"draft\"," +
            "\"can_launch\":true,\"budget\":{\"type\":\"daily\",\"amount\":10,\"currency\":\"EUR\"}," +
            "\"schedule\":{\"duration_days\":null},\"targeting\":{\"countries\":[\"NL\"],\"automatic_audience\":true}," +
            "\"ads\":[{\"key\":\"a1\",\"media\":[{\"url\":\"https://example.com/a.jpg\",\"type\":\"image\"}]," +
            "\"primary_text\":\"Hi\",\"link\":\"https://example.com\"}]," +
            "\"campaigns\":[{\"id\":\"41\",\"platform\":\"meta\",\"status\":\"draft\",\"editable\":true," +
            "\"budget\":{\"type\":\"daily\",\"amount\":10,\"currency\":\"EUR\"}," +
            "\"ad_account\":{\"id\":\"12\",\"name\":\"Shop\",\"currency\":\"EUR\",\"billing_ok\":true,\"needs_reauth\":false}," +
            "\"errors\":null,\"readiness\":{\"ready\":false,\"issues\":[{\"field\":\"ads\",\"code\":\"media_missing\",\"message\":\"Add a picture\"}],\"ads\":[]}}]," +
            "\"review_url\":\"https://app.omnisocials.com/ads/create/g-1\"}}");

        var typed = draft.Deserialize<AdDraftResponse>()!.Data;

        Assert.Equal("g-1", typed.GroupId);
        Assert.True(typed.CanLaunch);
        Assert.Equal(10m, typed.Budget!.Amount);
        Assert.Null(typed.Schedule!.DurationDays);
        Assert.Equal("NL", typed.Targeting!.Countries.First());
        Assert.Equal("https://example.com/a.jpg", typed.Ads[0].Media.First().Url);
        Assert.Equal(41, typed.Campaigns[0].Id);
        Assert.Equal(12, typed.Campaigns[0].AdAccount!.Id);
        Assert.Equal("media_missing", typed.Campaigns[0].Readiness!.Issues[0].Code);

        var campaign = Parse(
            "{\"data\":{\"id\":7,\"name\":\"Boost\",\"platform\":\"meta\",\"campaign_type\":\"boost\",\"status\":\"active\"," +
            "\"budget\":{\"type\":\"daily\",\"amount\":\"5.50\",\"currency\":\"EUR\"}," +
            "\"metrics\":{\"spend\":12.4,\"impressions\":3100},\"external_campaign_id\":\"238\",\"group_id\":null," +
            "\"createdAt\":\"2026-10-20T09:00:00.000Z\"}}");

        var c = campaign.Deserialize<AdCampaignResponse>()!.Data;

        Assert.Equal(7, c.Id);
        Assert.Equal(5.50m, c.Budget!.Amount);
        Assert.Equal(3100, c.Metrics!["impressions"].GetInt32());
        Assert.Equal("2026-10-20T09:00:00.000Z", c.CreatedAt);

        var snakeCase = Parse("{\"id\":\"8\",\"platform\":\"x\",\"status\":\"draft\",\"created_at\":\"2026-10-21T10:00:00Z\"}");
        Assert.Equal("2026-10-21T10:00:00Z", snakeCase.Deserialize<AdCampaign>()!.CreatedAt);
        Assert.DoesNotContain("created_at", JsonSerializer.Serialize(snakeCase.Deserialize<AdCampaign>()));
    }

    [Fact]
    public async Task A_draft_without_an_end_sends_duration_days_null()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{}}");
        using var client = CreateClient(handler);

        await client.Ads.Drafts.CreateAsync(new AdDraftParams { Schedule = new AdDraftSchedule() });

        var schedule = Parse(handler.RequestBodies[0]!).GetProperty("schedule");
        Assert.Equal(JsonValueKind.Null, schedule.GetProperty("duration_days").ValueKind);
    }

    // ─── Ads API parity (2026-11-05) ─────────────────────────────────────────

    [Fact]
    public async Task Parity_calls_send_the_right_requests()
    {
        var handler = new StubHttpMessageHandler();
        for (var i = 0; i < 24; i++) handler.Enqueue(HttpStatusCode.OK, "{\"data\":{}}");
        using var client = CreateClient(handler);
        var ads = client.Ads;

        await ads.Objects.TreeAsync(12, new Dictionary<string, object?> { ["from"] = "2026-10-01", ["status"] = new[] { "active", "paused" }, ["limit"] = 50 });
        await ads.Objects.GetAsync(12, "ad_set", "23851/1");
        await ads.Objects.SetStatusAsync(12, "ad", "9", "paused");
        await ads.Objects.SetStatusAsync(12, "campaign", "238", "active", 25m);
        await ads.Objects.StartImportAsync(12);
        await ads.BulkStatusAsync(new long[] { 7, 8 }, "active", 10m);
        await ads.CampaignAds.AddAsync(7, new[] { new AdDraftAd { PrimaryText = "Hi", Link = "https://example.com" } });
        await ads.CampaignAds.SetStatusAsync(7, 41, "active", 10m);
        await ads.Previews.ForAdAsync(7, 41, new[] { "feed", "story" });
        await ads.Targeting.SearchAsync(new Dictionary<string, object?> { ["ad_account_id"] = 12, ["type"] = "interest", ["q"] = "yoga" });
        await ads.Audiences.GetAsync("aud 1", 12);
        await ads.Audiences.RemoveMembersAsync("aud-1", new AdAudienceMembersParams
        {
            AdAccountId = 12,
            Schema = new[] { "email" },
            Rows = new[] { new[] { "a@example.com" } },
        });
        await ads.LeadForms.ListAsync(12);
        await ads.Insights.CampaignAsync(7, new Dictionary<string, object?> { ["breakdown"] = "ad", ["metrics"] = new List<string> { "spend", "clicks" } });
        await ads.Rules.UndoAsync(3, 20m);
        await ads.Rules.PreviewSavedAsync(4);
        await ads.Results.PutMetricAsync("revenue", new { label = "Revenue" });
        await ads.Media.DeleteAsync("abc", new Dictionary<string, object?> { ["ad_account_id"] = 12, ["type"] = "image" });
        await ads.Comments.HideAsync(7, 41, "c:1", hidden: false);
        await ads.Accounts.UpdateSettingsAsync(12, new Dictionary<string, object?> { ["spend_cap"] = null, ["confirm_budget_amount"] = "remove" });
        await ads.ReachFrequency.ReserveAsync("rf1", 12, 500m);
        await ads.ReachFrequency.CancelAsync("rf1", 12);
        await ads.BudgetSchedules.CreateAsync(7, new AdBudgetScheduleParams
        {
            StartAt = "2026-11-27T00:00:00Z",
            EndAt = "2026-11-30T00:00:00Z",
            BudgetIncrease = new AdBudgetIncrease { Type = "absolute", Value = 20m },
            ConfirmBudgetAmount = 20m,
        });
        await ads.Google.SearchTermsAsync(new Dictionary<string, object?> { ["ad_account_id"] = 12L, ["limit"] = 20, ["active"] = true });

        var expected = new (string Method, string Path, string? Body)[]
        {
            ("GET", "/ads/accounts/12/tree?from=2026-10-01&status=active%2Cpaused&limit=50", null),
            ("GET", "/ads/accounts/12/objects/ad_set/23851%2F1", null),
            ("PUT", "/ads/accounts/12/objects/ad/9/status", "{\"status\":\"paused\"}"),
            ("PUT", "/ads/accounts/12/objects/campaign/238/status", "{\"status\":\"active\",\"confirm_budget_amount\":25}"),
            ("POST", "/ads/accounts/12/import", null),
            ("POST", "/ads/campaigns/bulk-status", "{\"campaign_ids\":[7,8],\"status\":\"active\",\"confirm_budget_amount\":10}"),
            ("POST", "/ads/campaigns/7/ads", "{\"ads\":[{\"media\":[],\"primary_text\":\"Hi\",\"link\":\"https://example.com\"}]}"),
            ("PUT", "/ads/campaigns/7/ads/41/status", "{\"status\":\"active\",\"confirm_budget_amount\":10}"),
            ("GET", "/ads/campaigns/7/ads/41/previews?placements=feed%2Cstory", null),
            ("GET", "/ads/targeting/search?ad_account_id=12&type=interest&q=yoga", null),
            ("GET", "/ads/audiences/aud%201?ad_account_id=12", null),
            ("DELETE", "/ads/audiences/aud-1/members", "{\"ad_account_id\":12,\"schema\":[\"email\"],\"rows\":[[\"a@example.com\"]]}"),
            ("GET", "/ads/lead-forms?ad_account_id=12", null),
            ("GET", "/ads/campaigns/7/analytics?breakdown=ad&metrics=spend%2Cclicks", null),
            ("POST", "/ads/rules/activity/3/undo", "{\"confirm_budget_amount\":20}"),
            ("POST", "/ads/rules/4/preview", null),
            ("PUT", "/ads/campaign-results/metrics/revenue", "{\"label\":\"Revenue\"}"),
            ("DELETE", "/ads/media/abc?ad_account_id=12&type=image", null),
            ("POST", "/ads/campaigns/7/ads/41/comments/c%3A1/hide", "{\"hidden\":false}"),
            ("PATCH", "/ads/accounts/12/settings", "{\"spend_cap\":null,\"confirm_budget_amount\":\"remove\"}"),
            ("POST", "/ads/reach-frequency/rf1/reserve", "{\"ad_account_id\":12,\"confirm_budget_amount\":500}"),
            ("DELETE", "/ads/reach-frequency/rf1?ad_account_id=12", null),
            ("POST", "/ads/campaigns/7/budget-schedules",
                "{\"start_at\":\"2026-11-27T00:00:00Z\",\"end_at\":\"2026-11-30T00:00:00Z\",\"budget_increase\":{\"type\":\"absolute\",\"value\":20},\"confirm_budget_amount\":20}"),
            ("GET", "/ads/google/search-terms?ad_account_id=12&limit=20&active=true", null),
        };
        Assert.Equal(expected.Length, handler.Requests.Count);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Method, handler.Requests[i].Method.Method);
            Assert.Equal(expected[i].Path, Path(handler, i));
            Assert.Equal(expected[i].Body, handler.RequestBodies[i]);
        }
    }

    [Fact]
    public async Task Parity_money_calls_are_sent_once_after_a_502()
    {
        var handler = new StubHttpMessageHandler();
        for (var i = 0; i < 10; i++) handler.Enqueue(HttpStatusCode.BadGateway, BadGateway, headers: RetryNow);
        using var client = CreateClient(handler);
        var ads = client.Ads;

        var moneyCalls = new Func<Task>[]
        {
            () => ads.Objects.UpdateAsync(12, "campaign", "238", new { confirm_budget_amount = 30 }),
            () => ads.Objects.SetStatusAsync(12, "campaign", "238", "active", 25m),
            () => ads.BulkStatusAsync(new long[] { 7, 8 }, "active", 10m),
            () => ads.CampaignAds.SetStatusAsync(7, 41, "active", 10m),
            () => ads.Rules.CreateAsync(new { name = "Raise", confirm_max_daily_budget = 50 }),
            () => ads.Rules.UpdateAsync(4, new { enabled = true }),
            () => ads.Rules.UndoAsync(3, 20m),
            () => ads.Accounts.UpdateSettingsAsync(12, new { spend_cap = 900, confirm_budget_amount = 900 }),
            () => ads.ReachFrequency.ReserveAsync("rf1", 12, 500m),
            () => ads.BudgetSchedules.CreateAsync(7, new { confirm_budget_amount = 20 }),
        };
        for (var i = 0; i < moneyCalls.Length; i++)
        {
            await Assert.ThrowsAsync<ServerException>(moneyCalls[i]);
            Assert.Equal(i + 1, handler.Requests.Count); // each money call is sent once
        }
    }

    [Fact]
    public async Task A_parity_read_is_retried_after_a_502()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.BadGateway, BadGateway, headers: RetryNow);
        handler.Enqueue(HttpStatusCode.OK,
            "{\"data\":{\"campaigns\":[{\"level\":\"campaign\",\"external_id\":\"238\",\"name\":\"Spring\",\"status\":\"active\"," +
            "\"source\":\"external\",\"campaign_id\":null,\"metrics\":{\"spend\":12.5,\"ctr\":1.4,\"leads\":null}," +
            "\"children\":[{\"level\":\"ad_set\",\"external_id\":\"239\",\"status\":\"paused\"}]}],\"next_cursor\":null,\"currency\":\"EUR\"}}");
        using var client = CreateClient(handler);

        var tree = await client.Ads.Objects.TreeAsync(12);

        Assert.Equal(2, handler.Requests.Count);
        var node = tree!.Value.GetProperty("data").GetProperty("campaigns")[0].Deserialize<AdTreeNode>()!;
        Assert.Equal("external", node.Source);
        Assert.Equal(12.5m, node.Metrics!.Spend);
        Assert.Null(node.Metrics.Leads);
        Assert.Equal("ad_set", node.Children![0].Level);
    }

    [Fact]
    public async Task Drafts_and_campaigns_carry_network_targeting_and_settings()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{}}");
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{}}");
        using var client = CreateClient(handler);

        var draft = Draft();
        draft.Objective = "lead_generation";
        draft.Platforms = new object[]
        {
            new AdDraftPlatform { Platform = "meta", Settings = new AdNetworkSettings { LeadFormId = "lf-9", Meta = new AdMetaSettings { BudgetLevel = "campaign" } } },
        };
        draft.Targeting.Networks = new Dictionary<string, AdNetworkTargeting>
        {
            ["meta"] = new()
            {
                Include = new() { ["interest"] = new() { new AdTargetingItem { Id = "6003", Name = "Yoga" } } },
                Audiences = new AdAudienceSelection { Exclude = new() { new AdTargetingItem { Id = "aud-1" } } },
            },
        };
        await client.Ads.Drafts.CreateAsync(draft);
        await client.Ads.UpdateAsync(7, new AdCampaignUpdateParams
        {
            Objective = "conversions",
            Targeting = new Dictionary<string, object?> { ["countries"] = new[] { "NL" }, ["network"] = new AdNetworkTargeting { Include = new() { ["location"] = new() { new AdTargetingItem { Id = "2759794", RadiusKm = 25 } } } } },
            Settings = new AdNetworkSettings
            {
                Optimization = new AdOptimizationSettings
                {
                    Conversion = new AdConversionTarget { DestinationId = "px1", Event = "PURCHASE" },
                    Bidding = new AdBiddingSettings { Strategy = "cost_cap", Amount = 4.5m },
                },
            },
        });

        var sentDraft = Parse(handler.RequestBodies[0]!);
        Assert.Equal("lead_generation", sentDraft.GetProperty("objective").GetString());
        var settings = sentDraft.GetProperty("platforms")[0].GetProperty("settings");
        Assert.Equal("lf-9", settings.GetProperty("lead_form_id").GetString());
        Assert.Equal("campaign", settings.GetProperty("meta").GetProperty("budget_level").GetString());
        Assert.False(settings.TryGetProperty("messaging", out _)); // nulls are left out
        var meta = sentDraft.GetProperty("targeting").GetProperty("networks").GetProperty("meta");
        Assert.Equal("6003", meta.GetProperty("include").GetProperty("interest")[0].GetProperty("id").GetString());
        Assert.Equal("aud-1", meta.GetProperty("audiences").GetProperty("exclude")[0].GetProperty("id").GetString());

        var sentCampaign = Parse(handler.RequestBodies[1]!);
        Assert.Equal(25, sentCampaign.GetProperty("targeting").GetProperty("network").GetProperty("include").GetProperty("location")[0].GetProperty("radius_km").GetInt32());
        Assert.Equal("cost_cap", sentCampaign.GetProperty("settings").GetProperty("optimization").GetProperty("bidding").GetProperty("strategy").GetString());
        Assert.Equal(4.5m, sentCampaign.GetProperty("settings").GetProperty("optimization").GetProperty("bidding").GetProperty("amount").GetDecimal());
    }

    [Fact]
    public void Typed_capabilities_read_the_parity_fields()
    {
        var caps = Parse(
            "{\"networks\":[{\"key\":\"meta\",\"bid_strategies\":[\"lowest_cost\"]}],\"goals\":[\"traffic\"]," +
            "\"api_goals\":[\"lead_generation\",\"conversions\",\"messages\",\"calls\"],\"create_networks\":[\"meta\"]," +
            "\"network_states\":{\"meta\":\"live\"},\"api_draft_only\":[\"google\"],\"api_stop_in_app\":[\"google\"],\"features\":{\"meta\":[\"tree\",\"audiences\"]}," +
            "\"feature_labels\":{\"tree\":\"Campaigns, ad sets and ads\"},\"targeting_types\":{\"meta\":[\"interest\",\"location\"]}}")
            .Deserialize<AdCapabilities>()!;

        Assert.Equal(4, caps.ApiGoals!.Count);
        Assert.Contains("audiences", caps.Features!["meta"]);
        Assert.Equal("location", caps.TargetingTypes!["meta"][1]);
        Assert.Equal("live", caps.NetworkStates!["meta"]);
        Assert.Equal("google", caps.ApiDraftOnly![0]);
        Assert.Equal("google", caps.ApiStopInApp![0]);
        Assert.Equal("lowest_cost", caps.Networks[0].GetProperty("bid_strategies")[0].GetString());
    }

    [Fact]
    public void Ads_has_no_create_campaign_method()
    {
        // POST /ads/campaigns is gone: campaigns without ads could never start.
        Assert.Null(typeof(AdsResource).GetMethod("CreateAsync"));
    }

    [Fact]
    public void Overview_reads_a_null_spend_and_the_totals_per_currency()
    {
        var overview = Parse(
            "{\"connected_accounts\":2,\"campaigns_total\":3,\"campaigns_by_status\":{\"active\":2}," +
            "\"totals\":{\"spend\":null,\"currency\":null,\"impressions\":3000,\"clicks\":40}," +
            "\"totals_by_currency\":[{\"currency\":\"EUR\",\"spend\":10.5,\"impressions\":1000,\"clicks\":15}," +
            "{\"currency\":\"JPY\",\"spend\":\"1500\",\"impressions\":2000,\"clicks\":25}]}");

        var typed = overview.Deserialize<AdsOverview>()!;

        Assert.Null(typed.Totals.Spend);
        Assert.Null(typed.Totals.Currency);
        Assert.Equal(3000m, typed.Totals.Impressions);
        Assert.Equal(2, typed.TotalsByCurrency.Count);
        Assert.Equal("JPY", typed.TotalsByCurrency[1].Currency);
        Assert.Equal(1500m, typed.TotalsByCurrency[1].Spend);

        var single = Parse("{\"totals\":{\"spend\":12.5,\"currency\":\"EUR\",\"impressions\":10,\"clicks\":1},\"totals_by_currency\":[]}");
        Assert.Equal(12.5m, single.Deserialize<AdsOverview>()!.Totals.Spend);
    }
}
