using System.Net.Http.Headers;
using System.Text.Json;

namespace OmniSocials;

/// <summary>Media Library: upload, list, organize, preflight-check, delete.</summary>
public sealed class MediaResource
{
    private readonly OmniSocialsClient _client;

    internal MediaResource(OmniSocialsClient client) => _client = client;

    /// <summary><c>GET /media</c>: list the media library (newest first).</summary>
    public Task<JsonElement?> ListAsync(MediaListParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var query = new List<KeyValuePair<string, string?>>
        {
            new("limit", parameters?.Limit?.ToString()),
            new("offset", parameters?.Offset?.ToString()),
            new("search", parameters?.Search),
            new("folder_id", parameters?.FolderId),
        };
        return _client.GetAsync("/media", query, cancellationToken);
    }

    /// <summary><c>GET /media/:id</c>: fetch a single media item.</summary>
    public Task<JsonElement?> GetAsync(string id, CancellationToken cancellationToken = default)
        => _client.GetAsync($"/media/{Uri.EscapeDataString(id)}", null, cancellationToken);

    /// <summary>
    /// <c>POST /media/upload</c>: upload a file as multipart form data (max 50MB
    /// for images, 100MB request cap overall; use UploadFromUrlAsync or
    /// CreateUploadUrlAsync for larger videos, up to 1GB).
    ///
    /// Provide the file as bytes, a stream, or a file path (see
    /// <see cref="MediaUploadParams"/>). A PDF is rasterized into image slides
    /// and returned as a carousel (<c>slides</c> + <c>media_ids</c>).
    /// </summary>
    public async Task<JsonElement?> UploadAsync(MediaUploadParams parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is null) throw new ArgumentNullException(nameof(parameters));

        // Buffer the file once so every retry attempt can rebuild the form body.
        byte[] bytes;
        var filename = parameters.Filename;
        if (parameters.FilePath is not null)
        {
            bytes = await File.ReadAllBytesAsync(parameters.FilePath, cancellationToken).ConfigureAwait(false);
            filename ??= Path.GetFileName(parameters.FilePath);
        }
        else if (parameters.Bytes is not null)
        {
            bytes = parameters.Bytes;
        }
        else if (parameters.Stream is not null)
        {
            using var buffer = new MemoryStream();
            await parameters.Stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            bytes = buffer.ToArray();
        }
        else
        {
            throw new ArgumentException(
                "Provide the file as exactly one of Bytes, Stream, or FilePath.", nameof(parameters));
        }
        filename ??= "upload.bin";

        HttpContent ContentFactory()
        {
            var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(bytes);
            if (parameters.ContentType is not null)
            {
                file.Headers.ContentType = MediaTypeHeaderValue.Parse(parameters.ContentType);
            }
            form.Add(file, "\"file\"", $"\"{filename.Replace("\"", "")}\"");
            if (parameters.Name is not null) form.Add(new StringContent(parameters.Name), "\"name\"");
            if (parameters.Folder is not null) form.Add(new StringContent(parameters.Folder), "\"folder\"");
            if (parameters.FolderId is not null) form.Add(new StringContent(parameters.FolderId), "\"folder_id\"");
            return form;
        }

        return await _client.PostFormAsync("/media/upload", ContentFactory, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// <c>POST /media/upload-from-url</c>: the server fetches a public URL
    /// (files up to 1GB; large videos finish processing in the background and
    /// come back with status "processing").
    /// </summary>
    public Task<JsonElement?> UploadFromUrlAsync(MediaUploadFromUrlParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/media/upload-from-url", parameters, cancellationToken);

    /// <summary><c>POST /media/upload-from-base64</c>: upload base64-encoded file data.</summary>
    public Task<JsonElement?> UploadFromBase64Async(MediaUploadFromBase64Params parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/media/upload-from-base64", parameters, cancellationToken);

    /// <summary>
    /// <c>POST /media/create-upload-url</c>: mint a one-time presigned upload URL
    /// for large files (up to 1GB). POST the file as multipart form data (field
    /// name "file") to the returned <c>upload_url</c> within
    /// <c>expires_in_seconds</c>; no auth headers are needed on that second request.
    /// </summary>
    public Task<JsonElement?> CreateUploadUrlAsync(CancellationToken cancellationToken = default)
        => _client.PostAsync("/media/create-upload-url", null, cancellationToken);

    /// <summary>
    /// <c>POST /media/check</c>: preflight a file's compatibility with the
    /// workspace's connected platforms BEFORE uploading. Provide one of: a
    /// public url, an existing media_id, or size_bytes + mime.
    /// </summary>
    public Task<JsonElement?> CheckAsync(MediaCheckParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/media/check", parameters, cancellationToken);

    /// <summary><c>PATCH /media/:id</c>: rename a file and/or move it into a folder.</summary>
    public Task<JsonElement?> UpdateAsync(string id, MediaUpdateParams parameters, CancellationToken cancellationToken = default)
    {
        // Built by hand: an explicit null folder_id (MoveToRoot) must survive
        // serialization, while unset fields are omitted.
        var body = new Dictionary<string, object?>();
        if (parameters.Name is not null) body["name"] = parameters.Name;
        if (parameters.MoveToRoot) body["folder_id"] = null;
        else if (parameters.FolderId is not null) body["folder_id"] = parameters.FolderId;
        return _client.PatchAsync($"/media/{Uri.EscapeDataString(id)}", body, cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /media/:id</c>: delete a media file. Resolves to null (204).
    /// Fails with 409 <c>media_in_use</c> when the file is attached to a
    /// scheduled or publishing post.
    /// </summary>
    public Task<JsonElement?> DeleteAsync(string id, CancellationToken cancellationToken = default)
        => _client.DeleteAsync($"/media/{Uri.EscapeDataString(id)}", cancellationToken);
}
