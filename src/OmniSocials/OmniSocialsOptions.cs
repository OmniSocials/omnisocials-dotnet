namespace OmniSocials;

/// <summary>Configuration for <see cref="OmniSocialsClient"/>.</summary>
public sealed class OmniSocialsOptions
{
    /// <summary>
    /// API key (<c>omsk_live_*</c> / <c>omsk_test_*</c>). Defaults to the
    /// <c>OMNISOCIALS_API_KEY</c> environment variable. Create a key in the
    /// OmniSocials app under Settings -> API Keys.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>API base URL. Defaults to <c>https://api.omnisocials.com/v1</c>.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Per-request timeout. Defaults to 30 seconds.</summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>Automatic retries on 429 / 5xx / connection errors. Defaults to 2.</summary>
    public int? MaxRetries { get; set; }

    /// <summary>
    /// Optional custom <see cref="System.Net.Http.HttpMessageHandler"/> (proxies,
    /// dependency injection, testing). The client wraps it in its own
    /// <see cref="HttpClient"/> and does not dispose the handler.
    /// </summary>
    public HttpMessageHandler? HttpMessageHandler { get; set; }
}
