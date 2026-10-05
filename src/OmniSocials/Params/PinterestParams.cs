namespace OmniSocials;

/// <summary>
/// Query parameters for <c>GET /pinterest/products</c>. Every property is
/// optional.
/// </summary>
public sealed class PinterestProductListParams
{
    /// <summary>
    /// Where to read product Pins from: "catalog" (the Pinterest catalog, needs
    /// catalog access) or "pins" (the account's own Pins). Default: "catalog"
    /// when the connection has catalog access, else "pins".
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Catalog source only. A product group id from <c>product_groups</c>.
    /// Default: the group named "All Products", else the first group.
    /// </summary>
    public string? ProductGroupId { get; set; }

    /// <summary>Cursor from the previous response's <c>bookmark</c>, to get the next page.</summary>
    public string? Bookmark { get; set; }

    /// <summary>Catalog source only. Products per page (1-100, default 25).</summary>
    public int? PageSize { get; set; }
}
