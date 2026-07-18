using System.Text.Json;

namespace OmniSocials;

/// <summary>Instagram Reels audio (Meta's licensed catalog).</summary>
public sealed class AudioResource
{
    private readonly OmniSocialsClient _client;

    internal AudioResource(OmniSocialsClient client) => _client = client;

    /// <summary>
    /// <c>GET /audio/search?q=&amp;type=</c>: search Meta's licensed audio catalog for
    /// Instagram Reels. Omit <paramref name="query"/> for trending audio;
    /// <paramref name="type"/> is "music" (default) or "original_sound". Use a result's
    /// audio_id as instagram.audio_id on a reel post (with optional
    /// instagram.audio_volume / instagram.video_volume, integers 0-100). preview_url
    /// is a temporary URL (~1.5 days); never persist it.
    /// </summary>
    public Task<JsonElement?> SearchAsync(string? query = null, string? type = null, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<KeyValuePair<string, string?>>
        {
            new("q", query),
            new("type", type),
        };
        return _client.GetAsync("/audio/search", queryParams, cancellationToken);
    }
}
