using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ModelForge.Excel.Commands;
using ModelForge.Excel.Configuration;

namespace ModelForge.Excel.Services
{
    public sealed class BackendBridgeClient : IDisposable
    {
        private readonly HttpClient _httpClient;

        public BackendBridgeClient(BridgeOptions options)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(options.BackendBaseUrl),
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
            };
        }

        public async Task<string> GetHealthAsync()
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, "/health"))
            {
                request.Headers.Add("X-Trace-Id", Guid.NewGuid().ToString("N"));
                using (var response = await _httpClient.SendAsync(request).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task<string> DispatchCommandAsync(string commandId, OfficeCommandHost host)
        {
            var json = "{\"commandId\":\"" + EscapeJson(commandId) + "\",\"host\":" + (int)host + ",\"userId\":\"local-vsto\"}";
            using (var request = new HttpRequestMessage(HttpMethod.Post, "/api/commands/dispatch"))
            {
                request.Headers.Add("X-Trace-Id", Guid.NewGuid().ToString("N"));
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var response = await _httpClient.SendAsync(request).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        private static string EscapeJson(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
