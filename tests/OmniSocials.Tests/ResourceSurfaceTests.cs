using System.Reflection;
using System.Text.Json;
using Xunit;

namespace OmniSocials.Tests;

/// <summary>
/// Guards the full v1 endpoint surface (45 endpoints + GET /health): every
/// resource method must exist, return Task&lt;JsonElement?&gt;, and accept a
/// CancellationToken.
/// </summary>
public class ResourceSurfaceTests
{
    [Theory]
    [InlineData(typeof(PostsResource),
        "ListAsync,GetAsync,RecentPlatformAsync,CreateAsync,CreateAndPublishAsync,UpdateAsync,DeleteAsync,PublishAsync")]
    [InlineData(typeof(MediaResource),
        "ListAsync,GetAsync,UploadAsync,UploadFromUrlAsync,UploadFromBase64Async,CreateUploadUrlAsync,CheckAsync,UpdateAsync,DeleteAsync")]
    [InlineData(typeof(FoldersResource), "ListAsync,CreateAsync,UpdateAsync,DeleteAsync")]
    [InlineData(typeof(HashtagSetsResource), "ListAsync,GetAsync,CreateAsync,UpdateAsync,DeleteAsync")]
    [InlineData(typeof(AccountsResource), "ListAsync,GetAsync")]
    [InlineData(typeof(AnalyticsResource), "PostAsync,PostsAsync,OverviewAsync,AccountsAsync,BestTimesAsync")]
    [InlineData(typeof(LocationsResource), "SearchAsync,ValidateAsync")]
    [InlineData(typeof(WebhooksResource), "ListAsync,GetAsync,CreateAsync,UpdateAsync,DeleteAsync,RotateSecretAsync")]
    [InlineData(typeof(InboxResource), "ListConversationsAsync,GetMessagesAsync,MarkReadAsync,ReplyAsync,HideAsync")]
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
        Assert.Equal(typeof(AccountsResource), client.GetProperty("Accounts")!.PropertyType);
        Assert.Equal(typeof(AnalyticsResource), client.GetProperty("Analytics")!.PropertyType);
        Assert.Equal(typeof(LocationsResource), client.GetProperty("Locations")!.PropertyType);
        Assert.Equal(typeof(WebhooksResource), client.GetProperty("Webhooks")!.PropertyType);
        Assert.Equal(typeof(InboxResource), client.GetProperty("Inbox")!.PropertyType);

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
