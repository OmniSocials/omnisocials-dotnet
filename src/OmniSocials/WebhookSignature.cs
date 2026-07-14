using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OmniSocials;

/// <summary>
/// Verify OmniSocials webhook deliveries (Stripe-style scheme).
///
/// The signed value is <c>"{timestamp}.{rawBody}"</c>, HMAC-SHA256 with the
/// webhook secret, hex digest; the <c>X-OmniSocials-Signature</c> header is
/// <c>t=&lt;unix&gt;,v1=&lt;hex&gt;</c>.
/// </summary>
public static class WebhookSignature
{
    private const int DefaultToleranceSeconds = 300;

    /// <summary>
    /// Verify a webhook delivery and return the parsed event.
    /// </summary>
    /// <param name="payload">
    /// The RAW request body, exactly as received. Do not parse and re-serialize
    /// it first: the signature is computed over the raw bytes.
    /// </param>
    /// <param name="signature">Value of the X-OmniSocials-Signature header: <c>t=&lt;unix&gt;,v1=&lt;hex&gt;</c>.</param>
    /// <param name="secret">The webhook's signing secret (shown once on create / rotate-secret).</param>
    /// <param name="toleranceSeconds">Max allowed age of the timestamp, in seconds. Defaults to 300 (5 minutes).</param>
    /// <returns>The parsed event object on success.</returns>
    /// <exception cref="WebhookVerificationException">On any failure (bad signature, stale timestamp, malformed input).</exception>
    public static JsonElement Verify(
        string payload,
        string signature,
        string secret,
        int toleranceSeconds = DefaultToleranceSeconds)
    {
        if (string.IsNullOrEmpty(secret))
        {
            throw new WebhookVerificationException("No webhook secret provided.");
        }
        if (string.IsNullOrEmpty(signature))
        {
            throw new WebhookVerificationException(
                "No signature header provided. Expected the X-OmniSocials-Signature header value.");
        }
        if (payload is null)
        {
            throw new WebhookVerificationException("No payload provided.");
        }

        // Parse `t=<unix>,v1=<hex>` (tolerate extra/unknown pairs and multiple v1).
        string? timestampRaw = null;
        var candidateSignatures = new List<string>();
        foreach (var part in signature.Split(','))
        {
            var eq = part.IndexOf('=');
            if (eq == -1) continue;
            var key = part[..eq].Trim();
            var value = part[(eq + 1)..].Trim();
            if (key == "t") timestampRaw = value;
            else if (key == "v1") candidateSignatures.Add(value);
        }

        if (timestampRaw is null || !long.TryParse(timestampRaw, out var timestamp))
        {
            throw new WebhookVerificationException(
                "Unable to extract timestamp from signature header. Expected format: t=<unix>,v1=<hex>.");
        }
        if (candidateSignatures.Count == 0)
        {
            throw new WebhookVerificationException(
                "Unable to extract v1 signature from signature header. Expected format: t=<unix>,v1=<hex>.");
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestampRaw}.{payload}"));
        var expectedBytes = Encoding.ASCII.GetBytes(Convert.ToHexString(hash).ToLowerInvariant());

        var matches = false;
        foreach (var candidate in candidateSignatures)
        {
            var candidateBytes = Encoding.ASCII.GetBytes(candidate);
            if (candidateBytes.Length == expectedBytes.Length &&
                CryptographicOperations.FixedTimeEquals(candidateBytes, expectedBytes))
            {
                matches = true;
            }
        }

        if (!matches)
        {
            throw new WebhookVerificationException(
                "Webhook signature verification failed: no v1 signature matches the expected signature.");
        }

        var nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (toleranceSeconds > 0 && nowSeconds - timestamp > toleranceSeconds)
        {
            throw new WebhookVerificationException(
                $"Webhook timestamp is outside the allowed tolerance of {toleranceSeconds}s " +
                $"(event is {nowSeconds - timestamp}s old). Possible replay.");
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            throw new WebhookVerificationException(
                "Webhook payload is not valid JSON (did you pass the raw request body?).");
        }
    }

    /// <summary>
    /// Verify a webhook delivery from the raw body bytes. See
    /// <see cref="Verify(string, string, string, int)"/>.
    /// </summary>
    public static JsonElement Verify(
        byte[] payload,
        string signature,
        string secret,
        int toleranceSeconds = DefaultToleranceSeconds)
    {
        if (payload is null)
        {
            throw new WebhookVerificationException("No payload provided.");
        }
        return Verify(Encoding.UTF8.GetString(payload), signature, secret, toleranceSeconds);
    }
}
