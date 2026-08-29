namespace OmniSocials;

/// <summary>
/// Query parameters for <c>GET /locations/search</c>. Pass either <see cref="Q"/>
/// or the <see cref="Latitude"/> + <see cref="Longitude"/> pair (coordinates are
/// Threads only); neither, both, a <see cref="Q"/> under 2 characters, or
/// out-of-range coordinates return a 400 validation error.
/// </summary>
public sealed class LocationSearchParams
{
    /// <summary>Free-text search (min 2 characters).</summary>
    public string? Q { get; set; }

    /// <summary>
    /// Location source: "instagram" (Facebook Places, the default) or "threads"
    /// (Meta's Threads location catalog). The two sources use different ids.
    /// </summary>
    public string? Platform { get; set; }

    /// <summary>Latitude (-90..90). Threads only; pair with <see cref="Longitude"/> instead of <see cref="Q"/>.</summary>
    public double? Latitude { get; set; }

    /// <summary>Longitude (-180..180). Threads only; pair with <see cref="Latitude"/> instead of <see cref="Q"/>.</summary>
    public double? Longitude { get; set; }
}
