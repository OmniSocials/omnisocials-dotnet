using System.Net;
using Xunit;

namespace OmniSocials.Tests;

public class ErrorMappingTests
{
    private static OmniSocialsClient CreateClient(StubHttpMessageHandler handler, int maxRetries = 0)
        => new(new OmniSocialsOptions
        {
            ApiKey = "omsk_test_key",
            BaseUrl = "https://api.test.local/v1",
            MaxRetries = maxRetries,
            Timeout = TimeSpan.FromSeconds(5),
            HttpMessageHandler = handler,
        });

    [Fact]
    public async Task Maps_404_to_NotFoundException_with_code_and_body()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.NotFound,
            "{\"error\":{\"code\":\"not_found\",\"message\":\"Post not found\"}}");
        using var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => client.Posts.GetAsync("nope"));

        Assert.Equal(404, ex.Status);
        Assert.Equal("not_found", ex.Code);
        Assert.Equal("Post not found", ex.Message);
        Assert.NotNull(ex.Body);
        Assert.Equal("not_found",
            ex.Body!.Value.GetProperty("error").GetProperty("code").GetString());
    }

    [Theory]
    [InlineData(400, typeof(ValidationException))]
    [InlineData(422, typeof(ValidationException))]
    [InlineData(401, typeof(AuthenticationException))]
    [InlineData(403, typeof(PermissionDeniedException))]
    [InlineData(404, typeof(NotFoundException))]
    [InlineData(500, typeof(ServerException))]
    [InlineData(503, typeof(ServerException))]
    [InlineData(409, typeof(ApiException))] // no specific subclass
    public async Task Maps_status_codes_to_exception_types(int status, Type expectedType)
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue((HttpStatusCode)status,
            "{\"error\":{\"code\":\"some_code\",\"message\":\"Some message\"}}");
        using var client = CreateClient(handler);

        var ex = await Assert.ThrowsAnyAsync<ApiException>(() => client.Accounts.ListAsync());

        Assert.Equal(expectedType, ex.GetType());
        Assert.Equal(status, ex.Status);
        Assert.Equal("some_code", ex.Code);
        Assert.Equal("Some message", ex.Message);
    }

    [Fact]
    public async Task Maps_429_to_RateLimitException_with_RetryAfter()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.TooManyRequests,
            "{\"error\":{\"code\":\"rate_limit_exceeded\",\"message\":\"Too many requests\"}}",
            headers: new Dictionary<string, string> { ["Retry-After"] = "17" });
        using var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<RateLimitException>(() => client.Accounts.ListAsync());

        Assert.Equal(429, ex.Status);
        Assert.Equal("rate_limit_exceeded", ex.Code);
        Assert.Equal(17, ex.RetryAfter);
    }

    [Fact]
    public async Task Error_body_with_plain_string_error_uses_it_as_message()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.BadRequest, "{\"error\":\"channels is required\"}");
        using var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => client.Posts.CreateAsync(new PostCreateParams { Content = "hi" }));

        Assert.Equal("channels is required", ex.Message);
        Assert.Null(ex.Code);
    }

    [Fact]
    public async Task Non_json_error_body_falls_back_to_generic_message()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.BadRequest, "<html>413 from the CDN</html>", contentType: "text/html");
        using var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => client.Accounts.ListAsync());

        Assert.Equal("Request failed with status 400.", ex.Message);
        Assert.Null(ex.Body);
    }

    [Fact]
    public async Task Delete_204_resolves_to_null()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.NoContent);
        using var client = CreateClient(handler);

        var result = await client.Posts.DeleteAsync("42");

        Assert.Null(result);
    }

    [Fact]
    public async Task All_exceptions_derive_from_OmniSocialsException()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.NotFound, "{\"error\":{\"code\":\"not_found\",\"message\":\"nope\"}}");
        using var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => client.Posts.GetAsync("x"));

        Assert.IsAssignableFrom<ApiException>(ex);
        Assert.IsAssignableFrom<OmniSocialsException>(ex);
    }
}
