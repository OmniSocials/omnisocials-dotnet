using System.Net;
using Xunit;

namespace OmniSocials.Tests;

// Serialized within the class (xunit default) so the environment variable
// mutations never race each other.
public class ClientConstructionTests
{
    private const string EnvVar = "OMNISOCIALS_API_KEY";

    private static void WithEnv(string? value, Action action)
    {
        var original = Environment.GetEnvironmentVariable(EnvVar);
        try
        {
            Environment.SetEnvironmentVariable(EnvVar, value);
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvVar, original);
        }
    }

    [Fact]
    public void Reads_api_key_from_environment()
    {
        WithEnv("omsk_test_from_env", () =>
        {
            using var client = new OmniSocialsClient();
            Assert.Equal("https://api.omnisocials.com/v1", client.BaseUrl);
        });
    }

    [Fact]
    public void Missing_api_key_throws_AuthenticationException_at_construction()
    {
        WithEnv(null, () =>
        {
            var ex = Assert.Throws<AuthenticationException>(() => new OmniSocialsClient());
            Assert.Equal("missing_api_key", ex.Code);
            Assert.Contains("OMNISOCIALS_API_KEY", ex.Message);
            Assert.Contains("Settings -> API Keys", ex.Message);
        });
    }

    [Fact]
    public async Task Explicit_api_key_wins_over_environment()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"status\":\"ok\"}");

        await WithEnvAsync("omsk_test_env_key", async () =>
        {
            using var client = new OmniSocialsClient(new OmniSocialsOptions
            {
                ApiKey = "omsk_test_arg_key",
                HttpMessageHandler = handler,
            });
            await client.HealthAsync();
        });

        var auth = handler.Requests[0].Headers.GetValues("Authorization").Single();
        Assert.Equal("Bearer omsk_test_arg_key", auth);
    }

    [Fact]
    public async Task Defaults_and_headers_are_applied()
    {
        var handler = new StubHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"status\":\"ok\",\"version\":\"1.0.0\"}");

        using var client = new OmniSocialsClient(new OmniSocialsOptions
        {
            ApiKey = "omsk_test_key",
            HttpMessageHandler = handler,
        });

        Assert.Equal(TimeSpan.FromSeconds(30), client.Timeout);
        Assert.Equal(2, client.MaxRetries);
        Assert.Equal("https://api.omnisocials.com/v1", client.BaseUrl);

        var health = await client.HealthAsync();
        Assert.Equal("ok", health!.Value.GetProperty("status").GetString());

        var request = handler.Requests[0];
        Assert.Equal("https://api.omnisocials.com/v1/health", request.RequestUri!.ToString());
        Assert.Equal($"omnisocials-dotnet/{OmniSocialsClient.Version}",
            string.Join("", request.Headers.GetValues("User-Agent")));
    }

    [Fact]
    public void Base_url_trailing_slash_is_trimmed()
    {
        using var client = new OmniSocialsClient(new OmniSocialsOptions
        {
            ApiKey = "omsk_test_key",
            BaseUrl = "https://api.test.local/v1///",
        });
        Assert.Equal("https://api.test.local/v1", client.BaseUrl);
    }

    private static async Task WithEnvAsync(string? value, Func<Task> action)
    {
        var original = Environment.GetEnvironmentVariable(EnvVar);
        try
        {
            Environment.SetEnvironmentVariable(EnvVar, value);
            await action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvVar, original);
        }
    }
}
