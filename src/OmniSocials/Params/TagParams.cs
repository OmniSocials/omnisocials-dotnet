using System.Text.Json.Serialization;

namespace OmniSocials;

/// <summary>Body for <c>POST /tags</c>.</summary>
public class TagCreateParams
{
    /// <summary>Tag name, max 40 characters, unique in the workspace (case-insensitive).</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// One of blue, green, orange, red, purple, pink, teal, yellow, brown or gray.
    /// Omit for the next colour of the palette.
    /// </summary>
    [JsonPropertyName("color")]
    public string? Color { get; set; }
}

/// <summary>Body for <c>PATCH /tags/:id</c>. Null properties are omitted (they keep their value).</summary>
public class TagUpdateParams
{
    /// <summary>New tag name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>New colour (see <see cref="TagCreateParams.Color"/>).</summary>
    [JsonPropertyName("color")]
    public string? Color { get; set; }
}

/// <summary>
/// Body for <c>POST /tags/apply</c>. <see cref="Add"/>, <see cref="Remove"/>
/// and <see cref="Set"/> take tag ids (numbers) or names (strings); a new name
/// in <see cref="Add"/> or <see cref="Set"/> becomes a tag.
/// </summary>
public class TagApplyParams
{
    /// <summary>The posts to change, at most 500. Ids that are not posts of the workspace come back in <c>not_found</c>.</summary>
    [JsonPropertyName("post_ids")]
    public IList<string> PostIds { get; set; } = new List<string>();

    /// <summary>Tags to add to every post (ids or names).</summary>
    [JsonPropertyName("add")]
    public IList<object>? Add { get; set; }

    /// <summary>Tags to take off every post (ids or names).</summary>
    [JsonPropertyName("remove")]
    public IList<object>? Remove { get; set; }

    /// <summary>Replaces every post's tags with these (ids or names).</summary>
    [JsonPropertyName("set")]
    public IList<object>? Set { get; set; }
}

/// <summary>Query parameters for <c>GET /analytics/tags</c>.</summary>
public sealed class TagAnalyticsParams
{
    /// <summary>"YYYY-MM-DD", "YYYY-MM" or ISO 8601. Default: 90 days ago.</summary>
    public string? StartDate { get; set; }

    /// <summary>"YYYY-MM-DD", "YYYY-MM" or ISO 8601 (inclusive). Default: today.</summary>
    public string? EndDate { get; set; }

    /// <summary>Only this channel's numbers, e.g. "tiktok". Default: every connected channel.</summary>
    public string? Platform { get; set; }

    /// <summary>Measured posts a tag needs before <c>enough_posts</c> is true (default 5, 1 to 50).</summary>
    public int? MinPosts { get; set; }
}
