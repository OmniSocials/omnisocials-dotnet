using System.Text.Json.Serialization;

namespace OmniSocials;

// Typed response models for the post tag endpoints (/tags, GET /analytics/tags)
// and the tag fields on posts. Like every resource method, TagsResource and
// AnalyticsResource.TagsAsync return the raw JsonElement? so callers can read
// the payload directly. These classes are optional, strongly-typed
// deserialization targets: e.g.
//   (await client.Analytics.TagsAsync())?.Deserialize<TagAnalyticsResponse>().
// They mirror the API's snake_case fields via [JsonPropertyName].

/// <summary>A workspace's own label for its posts. Never sent to a platform.</summary>
public sealed class Tag
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>blue, green, orange, red, purple, pink, teal, yellow, brown or gray.</summary>
    [JsonPropertyName("color")]
    public string Color { get; set; } = "";

    /// <summary>ISO 8601.</summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }
}

/// <summary>
/// <c>trial_reel</c> on a post and on <c>GET /analytics/posts</c> items: set
/// when the post went to Instagram as a Trial Reel, null otherwise.
/// </summary>
public sealed class TrialReel
{
    /// <summary>"MANUAL" (the person decides in the Instagram app) or "SS_PERFORMANCE" (Instagram shares it with followers when it does well).</summary>
    [JsonPropertyName("strategy")]
    public string Strategy { get; set; } = "";
}

/// <summary>Response of <c>GET /tags</c>.</summary>
public sealed class TagListResponse
{
    [JsonPropertyName("data")]
    public List<Tag> Data { get; set; } = new();
}

/// <summary>Response of <c>POST /tags</c> and <c>PATCH /tags/:id</c>.</summary>
public sealed class TagResponse
{
    [JsonPropertyName("data")]
    public Tag Data { get; set; } = new();
}

/// <summary><c>data</c> of <c>DELETE /tags/:id</c>.</summary>
public sealed class TagDeleteResult
{
    /// <summary>The deleted tag.</summary>
    [JsonPropertyName("tag")]
    public Tag Tag { get; set; } = new();

    /// <summary>Posts that carried the tag.</summary>
    [JsonPropertyName("posts_untagged")]
    public int PostsUntagged { get; set; }
}

/// <summary>Response of <c>DELETE /tags/:id</c>.</summary>
public sealed class TagDeleteResponse
{
    [JsonPropertyName("data")]
    public TagDeleteResult Data { get; set; } = new();
}

/// <summary>One post in the result of <c>POST /tags/apply</c>.</summary>
public sealed class TaggedPost
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    /// <summary>The post's tags now.</summary>
    [JsonPropertyName("tags")]
    public List<Tag> Tags { get; set; } = new();
}

/// <summary><c>data</c> of <c>POST /tags/apply</c>.</summary>
public sealed class TagApplyResult
{
    /// <summary>Posts whose tags changed.</summary>
    [JsonPropertyName("updated")]
    public int Updated { get; set; }

    [JsonPropertyName("posts")]
    public List<TaggedPost> Posts { get; set; } = new();

    /// <summary>Post ids that are not posts of this workspace.</summary>
    [JsonPropertyName("not_found")]
    public List<string> NotFound { get; set; } = new();
}

/// <summary>Response of <c>POST /tags/apply</c>.</summary>
public sealed class TagApplyResponse
{
    [JsonPropertyName("data")]
    public TagApplyResult Data { get; set; } = new();
}

/// <summary>How a set of posts did against your normal (<c>untagged</c>, and the base of <see cref="TagComparison"/>).</summary>
public class TagSummary
{
    [JsonPropertyName("posts")]
    public int Posts { get; set; }

    /// <summary>Posts with numbers.</summary>
    [JsonPropertyName("measured")]
    public int Measured { get; set; }

    [JsonPropertyName("median_impressions")]
    public double? MedianImpressions { get; set; }

    /// <summary>Percent (5.4 = 5.4%).</summary>
    [JsonPropertyName("engagement_rate")]
    public double? EngagementRate { get; set; }

    /// <summary>Fraction against your normal (0.38 = 38% above, -0.2 = 20% below).</summary>
    [JsonPropertyName("vs_normal")]
    public double? VsNormal { get; set; }

    /// <summary>Channel where the tag beats your normal by the most (3 posts there at least).</summary>
    [JsonPropertyName("best_channel")]
    public string? BestChannel { get; set; }

    /// <summary>False below <c>min_posts</c> measured posts: read <see cref="VsNormal"/> as a hint only.</summary>
    [JsonPropertyName("enough_posts")]
    public bool EnoughPosts { get; set; }
}

/// <summary>One tag in <c>GET /analytics/tags</c>.</summary>
public sealed class TagComparison : TagSummary
{
    [JsonPropertyName("tag")]
    public Tag Tag { get; set; } = new();
}

/// <summary>The period of <c>GET /analytics/tags</c>.</summary>
public sealed class TagAnalyticsPeriod
{
    [JsonPropertyName("start_date")]
    public string StartDate { get; set; } = "";

    [JsonPropertyName("end_date")]
    public string EndDate { get; set; } = "";
}

/// <summary><c>data</c> of <c>GET /analytics/tags</c>.</summary>
public sealed class TagAnalytics
{
    [JsonPropertyName("period")]
    public TagAnalyticsPeriod Period { get; set; } = new();

    /// <summary>The channel filter, or null for every connected channel.</summary>
    [JsonPropertyName("platform")]
    public string? Platform { get; set; }

    [JsonPropertyName("min_posts")]
    public int MinPosts { get; set; }

    /// <summary>Comparable tags first, best first.</summary>
    [JsonPropertyName("tags")]
    public List<TagComparison> Tags { get; set; } = new();

    /// <summary>The posts without a tag.</summary>
    [JsonPropertyName("untagged")]
    public TagSummary Untagged { get; set; } = new();

    /// <summary>Median impressions per channel in the period.</summary>
    [JsonPropertyName("normals")]
    public Dictionary<string, double?> Normals { get; set; } = new();
}

/// <summary>Response of <c>GET /analytics/tags</c>.</summary>
public sealed class TagAnalyticsResponse
{
    [JsonPropertyName("data")]
    public TagAnalytics Data { get; set; } = new();
}
