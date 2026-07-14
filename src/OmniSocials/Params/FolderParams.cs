using System.Text.Json.Serialization;

namespace OmniSocials;

/// <summary>Body for <c>POST /folders</c>.</summary>
public class FolderCreateParams
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>Nest the new folder under this folder.</summary>
    [JsonPropertyName("parent_id")]
    public string? ParentId { get; set; }
}

/// <summary>Parameters for <c>PATCH /folders/:id</c>.</summary>
public sealed class FolderUpdateParams
{
    /// <summary>New folder name.</summary>
    public string? Name { get; set; }

    /// <summary>Move under this folder. Cycles are rejected.</summary>
    public string? ParentId { get; set; }

    /// <summary>
    /// When true, sends an explicit <c>"parent_id": null</c> to move the folder
    /// to the top level. Takes precedence over <see cref="ParentId"/>.
    /// </summary>
    public bool MoveToTopLevel { get; set; }
}
