using System.Globalization;
using System.Text.Json;

namespace OmniSocials;

/// <summary>
/// Pinterest product Pins for product tagging: list the product Pins of the
/// connected Pinterest account and check one Pin before using it in
/// <c>product_tags</c> on a post.
/// </summary>
public sealed class PinterestResource
{
    private readonly OmniSocialsClient _client;

    internal PinterestResource(OmniSocialsClient client) => _client = client;

    /// <summary>
    /// <c>GET /pinterest/products</c>: list the product Pins of the connected
    /// Pinterest account. Pass a result's <c>pin_id</c> in <c>product_tags</c>
    /// inside the <c>Pinterest</c> options of a post to tag the product on the
    /// Pin (max 24 per Pin). Pinterest only accepts a product Pin that is
    /// public, belongs to the same account and links to a website that account
    /// claimed; products of other merchants cannot be tagged.
    /// <para>
    /// The "catalog" source reads the Pinterest catalog (with <c>price</c>,
    /// <c>currency</c>, <c>availability</c>, <c>item_id</c>) and needs catalog
    /// access, which is given one time in the OmniSocials composer (Pinterest
    /// options, Add products, Connect catalog). The "pins" source reads the
    /// account's own Pins and works on every connection; one call scans up to
    /// 250 Pins, so <c>products</c> can be empty while <c>bookmark</c> is set
    /// (call again with the bookmark). Without
    /// <see cref="PinterestProductListParams.Source"/> the API uses "catalog"
    /// when the connection has catalog access, else "pins".
    /// </para>
    /// <para>
    /// The response is not the usual <c>data</c> envelope:
    /// <c>{ "products": [ { pin_id, title, description, link, image_url, price, currency, availability, item_id } ], "bookmark", "source", "catalog_access" }</c>
    /// plus <c>product_groups</c> and <c>product_group_id</c> for the catalog
    /// source, or <c>{ "error": { code, message } }</c> without <c>products</c>
    /// when the list could not be read, both with HTTP 200. code is one of
    /// <c>pinterest_not_connected</c>,
    /// <c>pinterest_catalog_access_required</c> or <c>platform_error</c>. A bad
    /// source or product group id throws a 400 <see cref="ValidationException"/>.
    /// </para>
    /// </summary>
    public Task<JsonElement?> ListProductsAsync(PinterestProductListParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<KeyValuePair<string, string?>>
        {
            new("source", parameters?.Source),
            new("product_group_id", parameters?.ProductGroupId),
            new("bookmark", parameters?.Bookmark),
            new("page_size", parameters?.PageSize?.ToString(CultureInfo.InvariantCulture)),
        };
        return _client.GetAsync("/pinterest/products", queryParams, cancellationToken);
    }

    /// <summary>
    /// <c>GET /pinterest/products/validate?id=</c>: check whether a Pin can be
    /// used in <c>product_tags</c> before creating the post. The id is a Pin id
    /// or a Pin link (<c>https://www.pinterest.com/pin/&lt;id&gt;/</c>). The
    /// response is <c>{ valid, pin_id, ... }</c>; <c>unverified: true</c> means
    /// the check could not run and the publish step is the final check.
    /// </summary>
    public Task<JsonElement?> ValidateProductAsync(string id, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<KeyValuePair<string, string?>> { new("id", id) };
        return _client.GetAsync("/pinterest/products/validate", queryParams, cancellationToken);
    }
}
