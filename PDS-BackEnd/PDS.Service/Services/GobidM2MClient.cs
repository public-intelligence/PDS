using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PDS.Domain.DTOs.Auth;
using PDS.Domain.Response;
using PDS.Service.Settings;
using System.Net;
using System.Net.Http.Json;

namespace PDS.Service.Services;

public sealed class GobidM2MClient : IGobidM2MClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GobidM2MClient> _logger;
    private readonly GobidM2MSettings _settings;

    public GobidM2MClient(
        HttpClient httpClient,
        IOptions<GobidM2MSettings> settings,
        ILogger<GobidM2MClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<IHttpResult<GobidAppTokenResponse>> GetTokenAsync(CancellationToken ct = default)
    {
        var endpoint = $"api/{_settings.ApiVersion}/Auth/app/token";
        var request = new GobidAppLoginRequest
        {
            ClientId = _settings.ClientId,
            ClientSecret = _settings.ClientSecret,
            Scope = _settings.Scope
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, request, ct);
            return await ToResultAsync(response, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener token M2M de GOBID.");
            return HttpResult<GobidAppTokenResponse>.Failure(
                "Error de comunicación con GOBID",
                new Dictionary<string, IEnumerable<string>>
                {
                    { "errors", ["No se pudo contactar al servicio de autenticación M2M"] }
                },
                HttpStatusCode.BadGateway
            );
        }
    }

    public async Task<IHttpResult<GobidAppTokenResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var endpoint = $"api/{_settings.ApiVersion}/Auth/app/refresh";
        var request = new GobidAppRefreshRequest { RefreshToken = refreshToken };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, request, ct);
            return await ToResultAsync(response, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al renovar token M2M de GOBID.");
            return HttpResult<GobidAppTokenResponse>.Failure(
                "Error de comunicación con GOBID",
                new Dictionary<string, IEnumerable<string>>
                {
                    { "errors", ["No se pudo renovar el token M2M"] }
                },
                HttpStatusCode.BadGateway
            );
        }
    }

    public async Task<bool> ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        var endpoint = $"api/{_settings.ApiVersion}/Auth/app/validate?token={Uri.EscapeDataString(token)}";

        try
        {
            var response = await _httpClient.PostAsync(endpoint, null, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar token M2M contra GOBID.");
            return false;
        }
    }

    private static async Task<IHttpResult<GobidAppTokenResponse>> ToResultAsync(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadFromJsonAsync<GobidAppTokenResponse>(cancellationToken: ct);

            if (payload is null || string.IsNullOrWhiteSpace(payload.AccessToken))
            {
                return HttpResult<GobidAppTokenResponse>.Failure(
                    "Respuesta inválida de GOBID M2M",
                    new Dictionary<string, IEnumerable<string>>
                    {
                        { "errors", ["GOBID no devolvió un accessToken válido"] }
                    },
                    HttpStatusCode.BadGateway
                );
            }

            return HttpResult<GobidAppTokenResponse>.Success(payload);
        }

        var statusCode = response.StatusCode;
        var detail = statusCode switch
        {
            HttpStatusCode.BadRequest => "Credenciales M2M inválidas",
            HttpStatusCode.Unauthorized => "No autorizado para obtener token M2M",
            _ => "Error temporal en el servicio de autenticación M2M"
        };

        return HttpResult<GobidAppTokenResponse>.Failure(
            "Error de autenticación M2M en GOBID",
            new Dictionary<string, IEnumerable<string>>
            {
                { "errors", [detail] }
            },
            statusCode
        );
    }
}
