using System.Reflection;
using System.Text.Json;
using Xunit;

namespace OmniSocials.Tests;

/// <summary>
/// Guards the full v1 endpoint surface (77 endpoints + GET /health): every
/// resource method must exist, return Task&lt;JsonElement?&gt;, and accept a
/// CancellationToken.
/// </summary>
public class ResourceSurfaceTests
{
    [Theory]
    [InlineData(typeof(PostsResource),
        "ListAsync,GetAsync,RecentPlatformAsync,CreateAsync,CreateAndPublishAsync,UpdateAsync,DeleteAsync,PublishAsync,ApproveAsync,RejectAsync,GetApprovalAsync")]
    [InlineData(typeof(MediaResource),
        "ListAsync,GetAsync,UploadAsync,UploadFromUrlAsync,UploadFromBase64Async,CreateUploadUrlAsync,CheckAsync,UpdateAsync,DeleteAsync")]
    [InlineData(typeof(FoldersResource), "ListAsync,CreateAsync,UpdateAsync,DeleteAsync")]
    [InlineData(typeof(HashtagSetsResource), "ListAsync,GetAsync,CreateAsync,UpdateAsync,DeleteAsync")]
    [InlineData(typeof(TagsResource), "ListAsync,CreateAsync,UpdateAsync,DeleteAsync,ApplyAsync")]
    [InlineData(typeof(AccountsResource), "ListAsync,GetAsync")]
    [InlineData(typeof(AnalyticsResource), "PostAsync,PostsAsync,OverviewAsync,AccountsAsync,BestTimesAsync,TagsAsync")]
    [InlineData(typeof(LocationsResource), "SearchAsync,ValidateAsync")]
    [InlineData(typeof(PinterestResource), "ListProductsAsync,ValidateProductAsync")]
    [InlineData(typeof(AutomationsResource), "ListAsync,GetAsync,CreateAsync,UpdateAsync,DeleteAsync,ListEventsAsync")]
    [InlineData(typeof(AdsResource),
        "ListAccountsAsync,CapabilitiesAsync,ListAsync,GetAsync,BoostAsync,BoostMultiAsync,UpdateAsync,ActivateAsync,ActivateMultiAsync,PauseAsync,ResumeAsync,DeleteAsync,OverviewAsync,SuggestionsAsync,ResearchKeywordsAsync,KeywordMetricsAsync")]
    [InlineData(typeof(AdDraftsResource), "DefaultsAsync,CreateAsync,GetAsync,UpdateAsync,DeleteAsync,AttachAccountAsync,LaunchAsync")]
    [InlineData(typeof(AdsResource),
        "BulkStatusAsync,DuplicateAsync")]
    [InlineData(typeof(AdCampaignAdsResource), "ListAsync,AddAsync,GetAsync,SetStatusAsync,ReviewAsync")]
    [InlineData(typeof(AdObjectsResource), "TreeAsync,GetAsync,UpdateAsync,SetStatusAsync,DuplicateAsync,ImportStatusAsync,StartImportAsync,ListExternalAsync,GetExternalAsync")]
    [InlineData(typeof(AdTargetingResource), "SearchAsync,BrowseAsync,ReachEstimateAsync,BidSuggestionsAsync")]
    [InlineData(typeof(AdAudiencesResource), "ListAsync,CreateAsync,GetAsync,UpdateAsync,DeleteAsync,AddMembersAsync,RemoveMembersAsync,AddCompaniesAsync")]
    [InlineData(typeof(AdLeadFormsResource), "ListAsync,CreateAsync,GetAsync,UpdateAsync,DeleteAsync,SendTestLeadAsync")]
    [InlineData(typeof(AdLeadsResource), "ListAsync,CountsAsync,UpdateAsync")]
    [InlineData(typeof(AdConversionsResource), "DestinationsAsync,GoalsAsync,CreateCustomAsync,SendEventsAsync")]
    [InlineData(typeof(AdInsightsResource), "QueryAsync,CampaignAsync,AdAsync,TimelineAsync,PerformanceAsync,AlertsAsync")]
    [InlineData(typeof(AdReportsResource), "ListAsync,StartAsync,GetAsync")]
    [InlineData(typeof(AdRulesResource), "ListAsync,CreateAsync,TemplatesAsync,PreviewAsync,ActivityAsync,UndoAsync,GetAsync,UpdateAsync,DeleteAsync,PreviewSavedAsync,RunsAsync")]
    [InlineData(typeof(AdResultsResource), "ListAsync,UploadAsync,TotalsAsync,MetricsAsync,PutMetricAsync")]
    [InlineData(typeof(AdMediaResource), "ListAsync,UploadAsync,GetAsync,DeleteAsync")]
    [InlineData(typeof(AdPreviewsResource), "CreateAsync,ForAdAsync")]
    [InlineData(typeof(AdPartnershipResource), "ListContentAsync,ListPermissionsAsync,SetPermissionAsync")]
    [InlineData(typeof(AdCommentsResource), "ListAsync,ReplyAsync,HideAsync,DeleteAsync")]
    [InlineData(typeof(AdAccountsResource), "HealthAsync,FinanceAsync,UpdateSettingsAsync,ListUsersAsync,AddUserAsync,RemoveUserAsync,ActivityAsync,BusinessesAsync,DsaAsync,HierarchyAsync,IdentitiesAsync")]
    [InlineData(typeof(AdReachFrequencyResource), "CreateAsync,GetAsync,CancelAsync,ReserveAsync")]
    [InlineData(typeof(AdStudiesResource), "ListAsync,CreateAsync")]
    [InlineData(typeof(AdBudgetSchedulesResource), "ListAsync,CreateAsync,DeleteAsync")]
    [InlineData(typeof(AdGoogleResource), "SearchTermsAsync,RecommendationsAsync,KeywordsAsync,AssetsAsync,SharedBudgetsAsync,BidStrategiesAsync,AdScheduleAsync,LabelsAsync,LocalServicesLeadsAsync")]
    [InlineData(typeof(WebhooksResource), "ListAsync,GetAsync,CreateAsync,UpdateAsync,DeleteAsync,RotateSecretAsync")]
    [InlineData(typeof(InboxResource), "ListConversationsAsync,GetMessagesAsync,MarkReadAsync,ReplyAsync,HideAsync,LikeAsync,DeleteMessageAsync,NextAsync")]
    [InlineData(typeof(LimitsResource), "ListAsync")]
    [InlineData(typeof(FeatureRequestsResource), "CreateAsync,ListAsync,GetAsync")]
    public void Resource_exposes_expected_methods(Type resourceType, string expectedMethods)
    {
        foreach (var name in expectedMethods.Split(','))
        {
            var overloads = resourceType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.Name == name)
                .ToArray();

            Assert.True(overloads.Length > 0, $"{resourceType.Name}.{name} is missing.");

            foreach (var method in overloads)
            {
                Assert.True(typeof(Task<JsonElement?>) == method.ReturnType,
                    $"{resourceType.Name}.{name} must return Task<JsonElement?>.");
                Assert.True(method.GetParameters().Any(p => p.ParameterType == typeof(CancellationToken)),
                    $"{resourceType.Name}.{name} must accept a CancellationToken.");
            }
        }
    }

    [Fact]
    public void Client_exposes_all_resources_and_health()
    {
        var client = typeof(OmniSocialsClient);

        Assert.Equal(typeof(PostsResource), client.GetProperty("Posts")!.PropertyType);
        Assert.Equal(typeof(MediaResource), client.GetProperty("Media")!.PropertyType);
        Assert.Equal(typeof(FoldersResource), client.GetProperty("Folders")!.PropertyType);
        Assert.Equal(typeof(HashtagSetsResource), client.GetProperty("HashtagSets")!.PropertyType);
        Assert.Equal(typeof(TagsResource), client.GetProperty("Tags")!.PropertyType);
        Assert.Equal(typeof(AccountsResource), client.GetProperty("Accounts")!.PropertyType);
        Assert.Equal(typeof(AnalyticsResource), client.GetProperty("Analytics")!.PropertyType);
        Assert.Equal(typeof(LocationsResource), client.GetProperty("Locations")!.PropertyType);
        Assert.Equal(typeof(PinterestResource), client.GetProperty("Pinterest")!.PropertyType);
        Assert.Equal(typeof(AutomationsResource), client.GetProperty("Automations")!.PropertyType);
        Assert.Equal(typeof(AdsResource), client.GetProperty("Ads")!.PropertyType);
        Assert.Equal(typeof(AdDraftsResource), typeof(AdsResource).GetProperty("Drafts")!.PropertyType);
        Assert.Equal(typeof(WebhooksResource), client.GetProperty("Webhooks")!.PropertyType);
        Assert.Equal(typeof(InboxResource), client.GetProperty("Inbox")!.PropertyType);
        Assert.Equal(typeof(LimitsResource), client.GetProperty("Limits")!.PropertyType);
        Assert.Equal(typeof(FeatureRequestsResource), client.GetProperty("FeatureRequests")!.PropertyType);

        var health = client.GetMethod("HealthAsync");
        Assert.NotNull(health);
        Assert.Equal(typeof(Task<JsonElement?>), health!.ReturnType);
    }

    [Fact]
    public void Exception_hierarchy_matches_the_spec()
    {
        Assert.True(typeof(ApiException).IsSubclassOf(typeof(OmniSocialsException)));
        Assert.True(typeof(AuthenticationException).IsSubclassOf(typeof(ApiException)));
        Assert.True(typeof(PermissionDeniedException).IsSubclassOf(typeof(ApiException)));
        Assert.True(typeof(NotFoundException).IsSubclassOf(typeof(ApiException)));
        Assert.True(typeof(ValidationException).IsSubclassOf(typeof(ApiException)));
        Assert.True(typeof(RateLimitException).IsSubclassOf(typeof(ApiException)));
        Assert.True(typeof(ServerException).IsSubclassOf(typeof(ApiException)));
        Assert.True(typeof(ApiConnectionException).IsSubclassOf(typeof(OmniSocialsException)));
        Assert.True(typeof(WebhookVerificationException).IsSubclassOf(typeof(OmniSocialsException)));
        Assert.NotNull(typeof(RateLimitException).GetProperty("RetryAfter"));
    }
}
