using System.Text.Json;

namespace OmniSocials;

/// <summary>
/// Base class for everything the OmniSocials SDK throws.
///
/// Hierarchy:
/// <code>
/// OmniSocialsException                (base)
///   ApiException                      (non-2xx HTTP response; has Status/Code/Body)
///     AuthenticationException         (401)
///     PermissionDeniedException       (403)
///     NotFoundException               (404)
///     ValidationException             (400 / 422)
///     RateLimitException              (429; exposes RetryAfter seconds)
///     ServerException                 (>= 500)
///   ApiConnectionException            (network failure / timeout)
///   WebhookVerificationException      (invalid webhook signature)
/// </code>
///
/// <c>Code</c> and <c>Message</c> are taken from the API error body
/// <c>{ "error": { "code", "message" } }</c> when present.
/// </summary>
public class OmniSocialsException : Exception
{
    public OmniSocialsException(string message) : base(message) { }

    public OmniSocialsException(string message, Exception? innerException)
        : base(message, innerException) { }
}

/// <summary>A non-2xx HTTP response from the OmniSocials API.</summary>
public class ApiException : OmniSocialsException
{
    /// <summary>HTTP status code of the failed response.</summary>
    public int Status { get; }

    /// <summary>Machine-readable error code from the API body (e.g. "validation_error").</summary>
    public string? Code { get; }

    /// <summary>The parsed response body, when the API returned JSON.</summary>
    public JsonElement? Body { get; }

    public ApiException(int status, string? code, string message, JsonElement? body = null)
        : base(message)
    {
        Status = status;
        Code = code;
        Body = body;
    }

    /// <summary>Map an HTTP status to the most specific <see cref="ApiException"/> subclass.</summary>
    public static ApiException Create(
        int status,
        string? code,
        string message,
        JsonElement? body = null,
        double? retryAfter = null)
    {
        return status switch
        {
            400 or 422 => new ValidationException(status, code, message, body),
            401 => new AuthenticationException(status, code, message, body),
            403 => new PermissionDeniedException(status, code, message, body),
            404 => new NotFoundException(status, code, message, body),
            429 => new RateLimitException(status, code, message, body, retryAfter),
            >= 500 => new ServerException(status, code, message, body),
            _ => new ApiException(status, code, message, body),
        };
    }
}

/// <summary>401: missing or invalid API key.</summary>
public class AuthenticationException : ApiException
{
    public AuthenticationException(int status, string? code, string message, JsonElement? body = null)
        : base(status, code, message, body) { }
}

/// <summary>403: the API key lacks the required scope, or access is denied.</summary>
public class PermissionDeniedException : ApiException
{
    public PermissionDeniedException(int status, string? code, string message, JsonElement? body = null)
        : base(status, code, message, body) { }
}

/// <summary>404: the resource does not exist (or belongs to another workspace).</summary>
public class NotFoundException : ApiException
{
    public NotFoundException(int status, string? code, string message, JsonElement? body = null)
        : base(status, code, message, body) { }
}

/// <summary>400 / 422: the request was rejected as invalid.</summary>
public class ValidationException : ApiException
{
    public ValidationException(int status, string? code, string message, JsonElement? body = null)
        : base(status, code, message, body) { }
}

/// <summary>429: rate limit exceeded (100 requests per minute per API key).</summary>
public class RateLimitException : ApiException
{
    /// <summary>Seconds to wait before retrying, from the Retry-After header (if present).</summary>
    public double? RetryAfter { get; }

    public RateLimitException(int status, string? code, string message, JsonElement? body = null, double? retryAfter = null)
        : base(status, code, message, body)
    {
        RetryAfter = retryAfter;
    }
}

/// <summary>500+: the API failed on the server side.</summary>
public class ServerException : ApiException
{
    public ServerException(int status, string? code, string message, JsonElement? body = null)
        : base(status, code, message, body) { }
}

/// <summary>A network failure or request timeout (no HTTP response was received).</summary>
public class ApiConnectionException : OmniSocialsException
{
    public ApiConnectionException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}

/// <summary>Webhook signature verification failed. Treat the delivery as unauthenticated.</summary>
public class WebhookVerificationException : OmniSocialsException
{
    public WebhookVerificationException(string message) : base(message) { }
}
