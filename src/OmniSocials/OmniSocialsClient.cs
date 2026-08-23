using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OmniSocials;

/// <summary>
/// The OmniSocials API client.
///
/// <code>
/// var client = new OmniSocialsClient(); // reads OMNISOCIALS_API_KEY from env
/// var post = await client.Posts.CreateAsync(new PostCreateParams
/// {
///     Content = "Hello from the SDK",
///     Channels = new[] { "instagram", "linkedin" },
///     ScheduledAt = "2026-08-01T09:00:00Z",
/// });
/// </code>
/// </summary>
public sealed class OmniSocialsClient : IDisposable
{
    /// <summary>SDK version, also sent as the User-Agent.</summary>
    public const string Version = "0.5.0";

    internal const string UserAgent = "omnisocials-dotnet/" + Version;

    private const string DefaultBaseUrl = "https://api.omnisocials.com/v1";
    private const int DefaultMaxRetries = 2;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>JSON options used for request bodies: null properties are omitted.</summary>
    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _apiKey;
    private readonly HttpClient _http;

    /// <summary>The API base URL in use (no trailing slash).</summary>
    public string BaseUrl { get; }

    /// <summary>Per-request timeout (applies to every retry attempt individually).</summary>
    public TimeSpan Timeout { get; }

    /// <summary>Automatic retries on 429 / 5xx / connection errors.</summary>
    public int MaxRetries { get; }

    public PostsResource Posts { get; }
    public MediaResource Media { get; }
    public FoldersResource Folders { get; }
    public HashtagSetsResource HashtagSets { get; }
    public AccountsResource Accounts { get; }
    public AnalyticsResource Analytics { get; }
    public AudioResource Audio { get; }
    public LocationsResource Locations { get; }
    public WebhooksResource Webhooks { get; }
    public InboxResource Inbox { get; }

    /// <summary>
    /// Create a client. The API key comes from <paramref name="options"/> or, when not
    /// set there, from the <c>OMNISOCIALS_API_KEY</c> environment variable. Throws
    /// <see cref="AuthenticationException"/> when no key is available.
    /// </summary>
    public OmniSocialsClient(OmniSocialsOptions? options = null)
    {
        options ??= new OmniSocialsOptions();

        var apiKey = options.ApiKey ?? Environment.GetEnvironmentVariable("OMNISOCIALS_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new AuthenticationException(
                401,
                "missing_api_key",
                "No API key provided. Pass one with new OmniSocialsClient(new OmniSocialsOptions { ApiKey = \"omsk_live_...\" }) " +
                "or set the OMNISOCIALS_API_KEY environment variable. Create a key in the " +
                "OmniSocials app under Settings -> API Keys.");
        }

        _apiKey = apiKey;
        BaseUrl = (options.BaseUrl ?? DefaultBaseUrl).TrimEnd('/');
        Timeout = options.Timeout ?? DefaultTimeout;
        MaxRetries = options.MaxRetries ?? DefaultMaxRetries;

        _http = options.HttpMessageHandler is not null
            ? new HttpClient(options.HttpMessageHandler, disposeHandler: false)
            : new HttpClient();
        // Timeouts are enforced per attempt with a CancellationTokenSource so that
        // retries still work; HttpClient.Timeout would abort the whole sequence.
        _http.Timeout = System.Threading.Timeout.InfiniteTimeSpan;

        Posts = new PostsResource(this);
        Media = new MediaResource(this);
        Folders = new FoldersResource(this);
        HashtagSets = new HashtagSetsResource(this);
        Accounts = new AccountsResource(this);
        Analytics = new AnalyticsResource(this);
        Audio = new AudioResource(this);
        Locations = new LocationsResource(this);
        Webhooks = new WebhooksResource(this);
        Inbox = new InboxResource(this);
    }

    /// <summary><c>GET /health</c> (no scopes required).</summary>
    public Task<JsonElement?> HealthAsync(CancellationToken cancellationToken = default)
        => GetAsync("/health", null, cancellationToken);

    public void Dispose() => _http.Dispose();

    // ─── HTTP verb helpers used by resources ─────────────────────────────────

    internal Task<JsonElement?> GetAsync(
        string path,
        IReadOnlyList<KeyValuePair<string, string?>>? query,
        CancellationToken cancellationToken)
        => RequestAsync(HttpMethod.Get, path, query, null, null, cancellationToken);

    internal Task<JsonElement?> PostAsync(
        string path,
        object? body,
        CancellationToken cancellationToken)
        => RequestAsync(HttpMethod.Post, path, null, body, null, cancellationToken);

    internal Task<JsonElement?> PostFormAsync(
        string path,
        Func<HttpContent> contentFactory,
        CancellationToken cancellationToken)
        => RequestAsync(HttpMethod.Post, path, null, null, contentFactory, cancellationToken);

    internal Task<JsonElement?> PatchAsync(
        string path,
        object body,
        CancellationToken cancellationToken)
        => RequestAsync(HttpMethod.Patch, path, null, body, null, cancellationToken);

    internal Task<JsonElement?> DeleteAsync(
        string path,
        CancellationToken cancellationToken)
        => RequestAsync(HttpMethod.Delete, path, null, null, null, cancellationToken);

    /// <summary>
    /// Core request loop: builds the URL, sends the request with a per-attempt
    /// timeout, and retries 429 / 5xx / connection errors / timeouts with
    /// exponential backoff plus jitter (honoring Retry-After when present).
    /// Returns the parsed response body as-is; 204 returns null.
    /// </summary>
    internal async Task<JsonElement?> RequestAsync(
        HttpMethod method,
        string path,
        IReadOnlyList<KeyValuePair<string, string?>>? query,
        object? jsonBody,
        Func<HttpContent>? contentFactory,
        CancellationToken cancellationToken)
    {
        var url = BuildUrl(path, query);
        // Serialize once; a fresh HttpContent is built per attempt because a
        // request message (and its content) cannot be reused after sending.
        string? json = jsonBody is null ? null : JsonSerializer.Serialize(jsonBody, SerializerOptions);

        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(method, url);
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _apiKey);
            request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            if (contentFactory is not null)
            {
                request.Content = contentFactory();
            }
            else if (json is not null)
            {
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            HttpResponseMessage response;
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                cts.CancelAfter(Timeout);
                try
                {
                    // ResponseContentRead: the timeout covers the body download too,
                    // and the body stays readable after the CTS is disposed.
                    response = await _http
                        .SendAsync(request, HttpCompletionOption.ResponseContentRead, cts.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw; // caller cancellation is never swallowed or retried
                }
                catch (OperationCanceledException ex)
                {
                    if (attempt < MaxRetries)
                    {
                        await Task.Delay(BackoffDelay(attempt, null), cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    throw new ApiConnectionException(
                        $"Request timed out after {Timeout.TotalMilliseconds:F0}ms ({method} {url}).", ex);
                }
                catch (HttpRequestException ex)
                {
                    if (attempt < MaxRetries)
                    {
                        await Task.Delay(BackoffDelay(attempt, null), cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    throw new ApiConnectionException($"Connection error ({method} {url}): {ex.Message}", ex);
                }
            }

            using (response)
            {
                if (response.IsSuccessStatusCode)
                {
                    if ((int)response.StatusCode == 204) return null;
                    var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(text)) return null;
                    try
                    {
                        using var doc = JsonDocument.Parse(text);
                        return doc.RootElement.Clone();
                    }
                    catch (JsonException)
                    {
                        // Non-JSON 2xx body; surface it as a JSON string element.
                        return JsonSerializer.SerializeToElement(text);
                    }
                }

                var status = (int)response.StatusCode;
                var retryAfter = ParseRetryAfter(response);
                var retryable = status == 429 || status >= 500;
                if (retryable && attempt < MaxRetries)
                {
                    await Task.Delay(BackoffDelay(attempt, retryAfter), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var errorText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw BuildError(status, errorText, retryAfter);
            }
        }
    }

    // ─── Internals ───────────────────────────────────────────────────────────

    private string BuildUrl(string path, IReadOnlyList<KeyValuePair<string, string?>>? query)
    {
        var sb = new StringBuilder(BaseUrl);
        if (!path.StartsWith('/')) sb.Append('/');
        sb.Append(path);
        if (query is not null)
        {
            var first = true;
            foreach (var (key, value) in query)
            {
                if (value is null) continue;
                sb.Append(first ? '?' : '&');
                first = false;
                sb.Append(Uri.EscapeDataString(key)).Append('=').Append(Uri.EscapeDataString(value));
            }
        }
        return sb.ToString();
    }

    /// <summary>Exponential backoff (0.5s, 1s, 2s, ...) with jitter; Retry-After wins.</summary>
    private static TimeSpan BackoffDelay(int attempt, double? retryAfterSeconds)
    {
        if (retryAfterSeconds.HasValue)
        {
            return TimeSpan.FromSeconds(Math.Min(Math.Max(retryAfterSeconds.Value, 0), 60));
        }
        var baseMs = 500d * Math.Pow(2, attempt);
        var jitter = 0.75 + Random.Shared.NextDouble() * 0.5; // 75% - 125%
        return TimeSpan.FromMilliseconds(Math.Min(baseMs * jitter, 30_000));
    }

    /// <summary>Parse a Retry-After header (delta-seconds or HTTP-date) to seconds.</summary>
    private static double? ParseRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter is null) return null;
        if (retryAfter.Delta.HasValue)
        {
            return Math.Max(retryAfter.Delta.Value.TotalSeconds, 0);
        }
        if (retryAfter.Date.HasValue)
        {
            var seconds = Math.Ceiling((retryAfter.Date.Value - DateTimeOffset.UtcNow).TotalSeconds);
            return seconds > 0 ? seconds : 0;
        }
        return null;
    }

    private static ApiException BuildError(int status, string text, double? retryAfter)
    {
        JsonElement? body = null;
        string? code = null;
        var message = $"Request failed with status {status}.";

        if (!string.IsNullOrEmpty(text))
        {
            try
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement.Clone();
                body = root;
                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty("error", out var error))
                {
                    if (error.ValueKind == JsonValueKind.Object)
                    {
                        if (error.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.String)
                        {
                            code = c.GetString();
                        }
                        if (error.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                        {
                            message = m.GetString()!;
                        }
                    }
                    else if (error.ValueKind == JsonValueKind.String)
                    {
                        message = error.GetString()!;
                    }
                }
            }
            catch (JsonException)
            {
                // Non-JSON error body (e.g. an HTML 413 page from the CDN).
            }
        }

        return ApiException.Create(status, code, message, body, retryAfter);
    }
}
