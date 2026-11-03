using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PDS.Service.Settings;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PDS.Service.Services;

public sealed class GobidUserPositionsClient : IGobidUserPositionsClient
{
    private readonly HttpClient _httpClient;
    private readonly GobidM2MSettings _settings;
    private readonly ILogger<GobidUserPositionsClient> _logger;

    public GobidUserPositionsClient(
        HttpClient httpClient,
        IOptions<GobidM2MSettings> settings,
        ILogger<GobidUserPositionsClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> GetUserPositionsJsonAsync(
        int organizationApplicationId,
        string? searchposition,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        var version = _settings.ApiVersion ?? "v1";
        var endpoint = $"api/{version}/User/organization-application/{organizationApplicationId}/position?pageNumber={pageNumber}&pageSize={pageSize}";

        if (!string.IsNullOrWhiteSpace(searchposition))
        {
            endpoint += $"&searchposition={Uri.EscapeDataString(searchposition)}";
        }

        try
        {
            using var response = await _httpClient.GetAsync(endpoint, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GOBID UserPositions: GET {Endpoint} returned HTTP {StatusCode}: {Body}", endpoint, (int)response.StatusCode, body);
            }

            return body;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GOBID UserPositions API endpoint {Endpoint}", endpoint);
            throw;
        }
    }
}
