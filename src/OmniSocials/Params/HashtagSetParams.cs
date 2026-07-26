using System.Text.Json.Serialization;

namespace OmniSocials;

/// <summary>Body for <c>POST /hashtag-sets</c>.</summary>
public class HashtagSetCreateParams
{
    /// <summary>Set name; posts match it case-insensitively via <see cref="PostCreateParams.HashtagSet"/>.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>The tags: a <c>string[]</c>, or a single string of tags.</summary>
    [JsonPropertyName("hashtags")]
    public object? Hashtags { get; set; }
}

/// <summary>Body for <c>PATCH /hashtag-sets/:id</c>. Null properties are omitted.</summary>
public class HashtagSetUpdateParams
{
    /// <summary>New set name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Replaces the FULL hashtag list: a <c>string[]</c>, or a single string of tags.</summary>
    [JsonPropertyName("hashtags")]
    public object? Hashtags { get; set; }
}
