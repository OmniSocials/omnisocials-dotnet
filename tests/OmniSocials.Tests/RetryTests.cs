using System.Net;
using Xunit;

namespace OmniSocials.Tests;

public class RetryTests
{
    private static OmniSocialsClient CreateClient(
        StubHttpMessageHandler handler,
        int maxRetries = 2,
        TimeSpan? timeout = null)
        => new(new OmniSocialsOptions
        {
            ApiKey = "omsk_test_key",
            BaseUrl = "https://api.test.local/v1",
            MaxRetries = maxRetries,
            Timeout = timeout ?? TimeSpan.FromSeconds(5),
            HttpMessageHandler = handler,
        });

    private static readonly Dictionary<string, string> RetryNow = new() { ["Retry-After"] = "0" };

    [Fact]
    public async Task Retries_429_then_succeeds()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.TooManyRequests,
            "{\"error\":{\"code\":\"rate_limit_exceeded\",\"message\":\"slow down\"}}",
            headers: RetryNow);
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"1\",\"status\":\"scheduled\"}}");
        using var client = CreateClient(handler);

        var result = await client.Posts.GetAsync("1");

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("1", result!.Value.GetProperty("data").GetProperty("id").GetString());
    }

    [Fact]
    public async Task Retries_500_then_succeeds()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.InternalServerError,
            "{\"error\":{\"code\":\"internal_error\",\"message\":\"boom\"}}",
            headers: RetryNow);
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":[]}");
        using var client = CreateClient(handler);

        var result = await client.Accounts.ListAsync();

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(0, result!.Value.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task Does_not_retry_4xx_other_than_429()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.BadRequest,
            "{\"error\":{\"code\":\"validation_error\",\"message\":\"bad\"}}");
        using var client = CreateClient(handler);

        await Assert.ThrowsAsync<ValidationException>(() => client.Accounts.ListAsync());

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Exhausted_retries_throw_RateLimitException()
    {
        var handler = new StubHttpMessageHandler();
        for (var i = 0; i < 3; i++)
        {
            handler.Enqueue(HttpStatusCode.TooManyRequests,
                "{\"error\":{\"code\":\"rate_limit_exceeded\",\"message\":\"slow down\"}}",
                headers: RetryNow);
        }
        using var client = CreateClient(handler, maxRetries: 2);

        await Assert.ThrowsAsync<RateLimitException>(() => client.Accounts.ListAsync());

        Assert.Equal(3, handler.Requests.Count); // 1 attempt + 2 retries
    }

    [Fact]
    public async Task MaxRetries_zero_disables_retries()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.InternalServerError,
            "{\"error\":{\"code\":\"internal_error\",\"message\":\"boom\"}}");
        using var client = CreateClient(handler, maxRetries: 0);

        await Assert.ThrowsAsync<ServerException>(() => client.Accounts.ListAsync());

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Retries_connection_error_then_succeeds()
    {
        var handler = new StubHttpMessageHandler();
        handler.EnqueueConnectionError();
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":[]}");
        using var client = CreateClient(handler);

        var result = await client.Accounts.ListAsync();

        Assert.Equal(2, handler.Requests.Count);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Exhausted_connection_errors_throw_ApiConnectionException()
    {
        var handler = new StubHttpMessageHandler();
        handler.EnqueueConnectionError();
        handler.EnqueueConnectionError();
        using var client = CreateClient(handler, maxRetries: 1);

        var ex = await Assert.ThrowsAsync<ApiConnectionException>(() => client.Accounts.ListAsync());

        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("Connection error", ex.Message);
    }

    [Fact]
    public async Task Per_attempt_timeout_is_retried_then_succeeds()
    {
        var handler = new StubHttpMessageHandler();
        handler.EnqueueDelayed(TimeSpan.FromSeconds(30), HttpStatusCode.OK, "{\"data\":[]}");
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":[]}");
        using var client = CreateClient(handler, maxRetries: 1, timeout: TimeSpan.FromMilliseconds(200));

        var result = await client.Accounts.ListAsync();

        Assert.Equal(2, handler.Requests.Count);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Timeout_with_no_retries_throws_ApiConnectionException()
    {
        var handler = new StubHttpMessageHandler();
        handler.EnqueueDelayed(TimeSpan.FromSeconds(30), HttpStatusCode.OK, "{\"data\":[]}");
        using var client = CreateClient(handler, maxRetries: 0, timeout: TimeSpan.FromMilliseconds(200));

        var ex = await Assert.ThrowsAsync<ApiConnectionException>(() => client.Accounts.ListAsync());

        Assert.Contains("timed out", ex.Message);
    }

    [Fact]
    public async Task Caller_cancellation_is_not_retried_and_propagates()
    {
        var handler = new StubHttpMessageHandler();
        handler.EnqueueDelayed(TimeSpan.FromSeconds(30), HttpStatusCode.OK, "{\"data\":[]}");
        using var client = CreateClient(handler);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.Accounts.ListAsync(cts.Token));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Multipart_upload_is_rebuilt_and_retried_on_429()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.TooManyRequests,
            "{\"error\":{\"code\":\"rate_limit_exceeded\",\"message\":\"slow down\"}}",
            headers: RetryNow);
        handler.Enqueue(HttpStatusCode.OK, "{\"data\":{\"id\":\"m1\"}}");
        using var client = CreateClient(handler);

        var result = await client.Media.UploadAsync(MediaUploadParams.FromBytes(
            new byte[] { 1, 2, 3 }, "pixel.png", name: "tiny"));

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("m1", result!.Value.GetProperty("data").GetProperty("id").GetString());
        // Both attempts must carry the full multipart body.
        Assert.All(handler.RequestBodies, body =>
        {
            Assert.NotNull(body);
            Assert.Contains("pixel.png", body);
            Assert.Contains("tiny", body);
        });
    }
}
