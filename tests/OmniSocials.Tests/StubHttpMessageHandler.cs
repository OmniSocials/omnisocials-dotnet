using System.Net;
using System.Text;

namespace OmniSocials.Tests;

/// <summary>
/// A scripted HttpMessageHandler: enqueue responses (or behaviors) and inspect
/// the requests the client actually sent.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _behaviors = new();

    public List<HttpRequestMessage> Requests { get; } = new();
    public List<string?> RequestBodies { get; } = new();

    public void Enqueue(
        HttpStatusCode status,
        string? body = null,
        string contentType = "application/json",
        IDictionary<string, string>? headers = null)
    {
        _behaviors.Enqueue((_, _) =>
        {
            var response = new HttpResponseMessage(status);
            if (body is not null)
            {
                response.Content = new StringContent(body, Encoding.UTF8, contentType);
            }
            if (headers is not null)
            {
                foreach (var (key, value) in headers)
                {
                    response.Headers.TryAddWithoutValidation(key, value);
                }
            }
            return Task.FromResult(response);
        });
    }

    /// <summary>Respond after a delay (used to trigger the client's per-attempt timeout).</summary>
    public void EnqueueDelayed(TimeSpan delay, HttpStatusCode status, string? body = null)
    {
        _behaviors.Enqueue(async (_, cancellationToken) =>
        {
            await Task.Delay(delay, cancellationToken);
            var response = new HttpResponseMessage(status);
            if (body is not null)
            {
                response.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }
            return response;
        });
    }

    /// <summary>Throw a connection error for the next request.</summary>
    public void EnqueueConnectionError(string message = "socket closed")
    {
        _behaviors.Enqueue((_, _) => Task.FromException<HttpResponseMessage>(new HttpRequestException(message)));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken));

        if (_behaviors.Count == 0)
        {
            throw new InvalidOperationException("StubHttpMessageHandler: no stubbed response left.");
        }
        return await _behaviors.Dequeue()(request, cancellationToken);
    }
}
