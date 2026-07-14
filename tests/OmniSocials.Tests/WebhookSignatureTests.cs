using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace OmniSocials.Tests;

public class WebhookSignatureTests
{
    private const string Secret = "whsec_test_secret_123";

    private const string Payload =
        "{\"id\":\"evt_1\",\"type\":\"post.published\",\"created_at\":\"2026-07-14T09:00:00.000Z\"," +
        "\"data\":{\"post_id\":42,\"workspace_id\":7,\"status\":\"published\",\"targets\":[]}}";

    /// <summary>
    /// Sign exactly like backend/services/webhooks/webhookDispatcher.js:
    /// HMAC-SHA256 hex over "{timestamp}.{rawBody}", header "t=&lt;ts&gt;,v1=&lt;hex&gt;".
    /// </summary>
    private static string Sign(string secret, long timestamp, string rawBody)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hex = Convert.ToHexString(
            hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{rawBody}"))).ToLowerInvariant();
        return $"t={timestamp},v1={hex}";
    }

    private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [Fact]
    public void Valid_signature_round_trips_and_returns_parsed_event()
    {
        var signature = Sign(Secret, Now(), Payload);

        var evt = WebhookSignature.Verify(Payload, signature, Secret);

        Assert.Equal(JsonValueKind.Object, evt.ValueKind);
        Assert.Equal("post.published", evt.GetProperty("type").GetString());
        Assert.Equal(42, evt.GetProperty("data").GetProperty("post_id").GetInt32());
    }

    [Fact]
    public void Byte_array_payload_overload_verifies()
    {
        var signature = Sign(Secret, Now(), Payload);

        var evt = WebhookSignature.Verify(Encoding.UTF8.GetBytes(Payload), signature, Secret);

        Assert.Equal("evt_1", evt.GetProperty("id").GetString());
    }

    [Fact]
    public void Tampered_payload_fails()
    {
        var signature = Sign(Secret, Now(), Payload);
        var tampered = Payload.Replace("\"post_id\":42", "\"post_id\":43");

        var ex = Assert.Throws<WebhookVerificationException>(
            () => WebhookSignature.Verify(tampered, signature, Secret));
        Assert.Contains("no v1 signature matches", ex.Message);
    }

    [Fact]
    public void Stale_timestamp_fails()
    {
        var stale = Now() - 301;
        var signature = Sign(Secret, stale, Payload);

        var ex = Assert.Throws<WebhookVerificationException>(
            () => WebhookSignature.Verify(Payload, signature, Secret, toleranceSeconds: 300));
        Assert.Contains("tolerance", ex.Message);
    }

    [Fact]
    public void Old_timestamp_passes_when_within_custom_tolerance()
    {
        var old = Now() - 400;
        var signature = Sign(Secret, old, Payload);

        var evt = WebhookSignature.Verify(Payload, signature, Secret, toleranceSeconds: 600);

        Assert.Equal("evt_1", evt.GetProperty("id").GetString());
    }

    [Fact]
    public void Zero_tolerance_disables_staleness_check()
    {
        var ancient = Now() - 100_000;
        var signature = Sign(Secret, ancient, Payload);

        var evt = WebhookSignature.Verify(Payload, signature, Secret, toleranceSeconds: 0);

        Assert.Equal("evt_1", evt.GetProperty("id").GetString());
    }

    [Fact]
    public void Wrong_secret_fails()
    {
        var signature = Sign("whsec_other_secret", Now(), Payload);

        Assert.Throws<WebhookVerificationException>(
            () => WebhookSignature.Verify(Payload, signature, Secret));
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("t=notanumber,v1=abc")]
    [InlineData("v1=deadbeef")] // missing timestamp
    [InlineData("t=1720000000")] // missing v1
    public void Malformed_signature_header_fails(string signature)
    {
        Assert.Throws<WebhookVerificationException>(
            () => WebhookSignature.Verify(Payload, signature, Secret));
    }

    [Fact]
    public void Missing_secret_fails()
    {
        var signature = Sign(Secret, Now(), Payload);

        Assert.Throws<WebhookVerificationException>(
            () => WebhookSignature.Verify(Payload, signature, ""));
    }

    [Fact]
    public void Extra_header_pairs_are_tolerated()
    {
        var ts = Now();
        var valid = Sign(Secret, ts, Payload); // t=<ts>,v1=<hex>
        var hex = valid.Split("v1=")[1];
        var signature = $"t={ts},v0=ignored,v1={hex}";

        var evt = WebhookSignature.Verify(Payload, signature, Secret);

        Assert.Equal("evt_1", evt.GetProperty("id").GetString());
    }

    [Fact]
    public void Non_json_payload_with_valid_signature_fails()
    {
        const string payload = "this is not json";
        var signature = Sign(Secret, Now(), payload);

        var ex = Assert.Throws<WebhookVerificationException>(
            () => WebhookSignature.Verify(payload, signature, Secret));
        Assert.Contains("not valid JSON", ex.Message);
    }
}
