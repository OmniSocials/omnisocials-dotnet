using System.Text.Json;

namespace OmniSocials;

/// <summary>Media Library folders.</summary>
public sealed class FoldersResource
{
    private readonly OmniSocialsClient _client;

    internal FoldersResource(OmniSocialsClient client) => _client = client;

    /// <summary><c>GET /folders</c>: all folders (flat list; build the tree via parent_id).</summary>
    public Task<JsonElement?> ListAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync("/folders", null, cancellationToken);

    /// <summary><c>POST /folders</c>: create a folder (optionally nested under parent_id).</summary>
    public Task<JsonElement?> CreateAsync(FolderCreateParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/folders", parameters, cancellationToken);

    /// <summary>
    /// <c>PATCH /folders/:id</c>: rename (<see cref="FolderUpdateParams.Name"/>) and/or
    /// move (<see cref="FolderUpdateParams.ParentId"/>, or
    /// <see cref="FolderUpdateParams.MoveToTopLevel"/> for the top level). Cycles are rejected.
    /// </summary>
    public Task<JsonElement?> UpdateAsync(string id, FolderUpdateParams parameters, CancellationToken cancellationToken = default)
    {
        // Built by hand: an explicit null parent_id (MoveToTopLevel) must survive
        // serialization, while unset fields are omitted.
        var body = new Dictionary<string, object?>();
        if (parameters.Name is not null) body["name"] = parameters.Name;
        if (parameters.MoveToTopLevel) body["parent_id"] = null;
        else if (parameters.ParentId is not null) body["parent_id"] = parameters.ParentId;
        return _client.PatchAsync($"/folders/{Uri.EscapeDataString(id)}", body, cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /folders/:id</c>: delete a folder. Media is preserved: files move
    /// to the root and subfolders move up to the deleted folder's parent.
    /// Resolves to null (204).
    /// </summary>
    public Task<JsonElement?> DeleteAsync(string id, CancellationToken cancellationToken = default)
        => _client.DeleteAsync($"/folders/{Uri.EscapeDataString(id)}", cancellationToken);
}
