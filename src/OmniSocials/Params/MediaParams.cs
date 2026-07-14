using System.Text.Json.Serialization;

namespace OmniSocials;

/// <summary>Query parameters for <c>GET /media</c>.</summary>
public sealed class MediaListParams
{
    /// <summary>Max items to return (default 20, max 100).</summary>
    public int? Limit { get; set; }

    /// <summary>Items to skip (default 0).</summary>
    public int? Offset { get; set; }

    /// <summary>Free-text search over name + filename.</summary>
    public string? Search { get; set; }

    /// <summary>Filter by folder. Use "root" (or "null") for unfiled items.</summary>
    public string? FolderId { get; set; }
}

/// <summary>
/// Parameters for <c>POST /media/upload</c> (multipart). Provide the file as
/// exactly one of <see cref="Bytes"/>, <see cref="Stream"/>, or <see cref="FilePath"/>.
/// </summary>
public sealed class MediaUploadParams
{
    /// <summary>The file contents as raw bytes.</summary>
    public byte[]? Bytes { get; set; }

    /// <summary>The file contents as a readable stream (buffered in memory so retries work).</summary>
    public Stream? Stream { get; set; }

    /// <summary>Path of a file on disk to upload.</summary>
    public string? FilePath { get; set; }

    /// <summary>Filename sent with the multipart part (inferred from <see cref="FilePath"/> when omitted).</summary>
    public string? Filename { get; set; }

    /// <summary>Optional MIME type for the file part (e.g. "image/jpeg").</summary>
    public string? ContentType { get; set; }

    /// <summary>Optional human-readable label so the asset is findable by name later.</summary>
    public string? Name { get; set; }

    /// <summary>Folder name to file the asset under (created at top level if missing).</summary>
    public string? Folder { get; set; }

    /// <summary>Id of an existing folder to file the asset under.</summary>
    public string? FolderId { get; set; }

    /// <summary>Create upload params from a file on disk.</summary>
    public static MediaUploadParams FromFile(string path, string? name = null, string? folder = null)
        => new() { FilePath = path, Name = name, Folder = folder };

    /// <summary>Create upload params from raw bytes.</summary>
    public static MediaUploadParams FromBytes(byte[] bytes, string filename, string? name = null, string? folder = null)
        => new() { Bytes = bytes, Filename = filename, Name = name, Folder = folder };

    /// <summary>Create upload params from a readable stream.</summary>
    public static MediaUploadParams FromStream(Stream stream, string filename, string? name = null, string? folder = null)
        => new() { Stream = stream, Filename = filename, Name = name, Folder = folder };
}

/// <summary>Body for <c>POST /media/upload-from-url</c>.</summary>
public class MediaUploadFromUrlParams
{
    /// <summary>Publicly accessible http(s) URL. Files up to 1GB are supported.</summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    /// <summary>Optional human-readable label so the asset is findable by name later.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Folder name to file the asset under (created at top level if missing).</summary>
    [JsonPropertyName("folder")]
    public string? Folder { get; set; }

    /// <summary>Id of an existing folder to file the asset under.</summary>
    [JsonPropertyName("folder_id")]
    public string? FolderId { get; set; }
}

/// <summary>Body for <c>POST /media/upload-from-base64</c>.</summary>
public class MediaUploadFromBase64Params
{
    /// <summary>Base64-encoded file data (without a data URI prefix).</summary>
    [JsonPropertyName("data")]
    public string Data { get; set; } = "";

    /// <summary>MIME type, e.g. "image/jpeg", "image/png", "application/pdf".</summary>
    [JsonPropertyName("mime_type")]
    public string MimeType { get; set; } = "";

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    /// <summary>Optional human-readable label so the asset is findable by name later.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Folder name to file the asset under (created at top level if missing).</summary>
    [JsonPropertyName("folder")]
    public string? Folder { get; set; }

    /// <summary>Id of an existing folder to file the asset under.</summary>
    [JsonPropertyName("folder_id")]
    public string? FolderId { get; set; }
}

/// <summary>
/// Body for <c>POST /media/check</c>. Provide one of: a public <see cref="Url"/>,
/// an existing <see cref="MediaId"/>, or <see cref="SizeBytes"/> + <see cref="Mime"/>.
/// </summary>
public class MediaCheckParams
{
    /// <summary>Public URL of the file to preflight.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Id of an already-uploaded Library item.</summary>
    [JsonPropertyName("media_id")]
    public string? MediaId { get; set; }

    /// <summary>Raw size in bytes (pair with <see cref="Mime"/>).</summary>
    [JsonPropertyName("size_bytes")]
    public long? SizeBytes { get; set; }

    /// <summary>MIME type (pair with <see cref="SizeBytes"/>).</summary>
    [JsonPropertyName("mime")]
    public string? Mime { get; set; }
}

/// <summary>Parameters for <c>PATCH /media/:id</c>.</summary>
public sealed class MediaUpdateParams
{
    /// <summary>Human-readable label. Send an empty string to clear it.</summary>
    public string? Name { get; set; }

    /// <summary>Move the file into this folder.</summary>
    public string? FolderId { get; set; }

    /// <summary>
    /// When true, sends an explicit <c>"folder_id": null</c> to move the file
    /// to the root ("All media"). Takes precedence over <see cref="FolderId"/>.
    /// </summary>
    public bool MoveToRoot { get; set; }
}
