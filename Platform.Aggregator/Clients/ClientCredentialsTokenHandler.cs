using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Platform.Aggregator.Clients;

public class ClientCredentialsTokenHandler(
    IHttpClientFactory clientFactory,
    IConfiguration configuration,
    ILogger<ClientCredentialsTokenHandler> logger) : DelegatingHandler
{
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(cancellationToken);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string?> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
        {
            return _cachedToken;
        }

        try
        {
            var idServerUrl = configuration["IdentityServer:Authority"] ?? "http://localhost:5400";
            var client = clientFactory.CreateClient();
            var response = await client.PostAsync($"{idServerUrl}/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = "aggregator",
                ["client_secret"] = "aggregator-secret",
                ["scope"] = "products.read orders.read reviews.read"
            }), cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("access_token", out var tokenProp))
                {
                    _cachedToken = tokenProp.GetString();
                    _tokenExpiry = DateTime.UtcNow.AddMinutes(10);
                    return _cachedToken;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to obtain client credentials token for Aggregator");
        }

        return null;
    }
}
