using System.Net;
using System.Text;
using ModelForge.Sidecar.Services;
using Xunit;

namespace ModelForge.Sidecar.Tests;

public class BackendBridgeClientTests
{
    [Fact]
    public async Task GetDictionaryTermsAsync_UnwrapsServiceExportEnvelope()
    {
        var handler = new StubHandler("""{"traceId":"trace","data":{"terms":[{"id":"t1","term":"DRAFT","category":"Compliance","severity":"Warning","updatedAt":"2026-06-06T00:00:00Z"}],"count":1,"exportedAtUtc":"2026-06-06T00:00:00Z"}}""");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5095") };
        var client = new BackendBridgeClient(httpClient);

        var terms = await client.GetDictionaryTermsAsync();

        Assert.Single(terms);
        Assert.Equal("DRAFT", terms[0].Term);
        Assert.Equal("/api/dictionary/service-export", handler.LastRequest?.RequestUri?.PathAndQuery);
        Assert.True(handler.LastRequest?.Headers.Contains("X-Trace-Id"));
    }

    private sealed class StubHandler(string responseBody) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });
        }
    }
}
