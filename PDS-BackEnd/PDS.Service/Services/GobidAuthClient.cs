using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PDS.Domain.DTOs.Auth;
using PDS.Domain.Response;
using PDS.Service.Settings;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PDS.Service.Services;

/// <summary>
/// Cliente de autenticación de usuarios contra GOBID.
/// Seguridad: GOBID solo acepta el refresh token como cookie "refreshToken". El HttpClient se registra
/// con UseCookies=false (ver GobidUserAuthRegistration) y el token de cada usuario se envía de forma
/// explícita en su propia petición. Nunca usar un CookieContainer compartido: con cookies duplicadas
/// GOBID usa la última y un usuario recibiría la sesión de otro.
/// </summary>
public class GobidAuthClient : IGobidAuthClient
{
    private const string RefreshCookieName = "refreshToken";

    private readonly HttpClient _httpClient;
    private readonly ILogger<GobidAuthClient> _logger;
    private readonly GobidAuthSettings _settings;

    public GobidAuthClient(
        HttpClient httpClient,
        IOptions<GobidAuthSettings> settings,
        ILogger<GobidAuthClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<IHttpResult<GobidUserTokens>> ExchangeCodeAsync(
        ExchangeCodeGobidRequestDto requestDto,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"api/{_settings.ApiVersion}/Auth/token";

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(endpoint, requestDto, cancellationToken);
            return await ToResultAsync(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error llamando a GOBID token endpoint.");
            return CommunicationFailure();
        }
    }

    public async Task<IHttpResult<GobidUserTokens>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"api/{_settings.ApiVersion}/Auth/refresh";

        try
        {
            using var request = WithRefreshCookie(new HttpRequestMessage(HttpMethod.Post, endpoint), refreshToken);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return await ToResultAsync(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error llamando a GOBID refresh endpoint.");
            return CommunicationFailure();
        }
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var endpoint = $"api/{_settings.ApiVersion}/Auth/logout";

        try
        {
            using var request = WithRefreshCookie(new HttpRequestMessage(HttpMethod.Post, endpoint), refreshToken);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("GOBID logout respondió {StatusCode}.", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error llamando a GOBID logout endpoint.");
        }
    }

    private static HttpRequestMessage WithRefreshCookie(HttpRequestMessage request, string refreshToken)
    {
        request.Headers.Add("Cookie", $"{RefreshCookieName}={refreshToken}");
        return request;
    }

    private static string? ReadRefreshTokenFromSetCookie(HttpResponseHeaders headers)
    {
        if (!headers.TryGetValues("Set-Cookie", out var cookies))
            return null;

        const string prefix = RefreshCookieName + "=";
        foreach (var cookie in cookies)
        {
            if (!cookie.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var value = cookie[prefix.Length..];
            var end = value.IndexOf(';');
            value = end >= 0 ? value[..end] : value;
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }

    private static HttpResult<GobidUserTokens> CommunicationFailure() =>
        HttpResult<GobidUserTokens>.Failure(
            "Error de comunicación con GOBID",
            new Dictionary<string, IEnumerable<string>>
            {
                { "errors", ["No se pudo contactar al servicio de autenticación"] }
            },
            HttpStatusCode.BadGateway
        );

    private static async Task<IHttpResult<GobidUserTokens>> ToResultAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadFromJsonAsync<AuthTokenResponseDto>(cancellationToken: cancellationToken);

            if (payload is null || string.IsNullOrWhiteSpace(payload.AccessToken))
            {
                return HttpResult<GobidUserTokens>.Failure(
                    "Respuesta inválida de GOBID",
                    new Dictionary<string, IEnumerable<string>>
                    {
                        { "errors", ["GOBID no devolvió un accessToken válido"] }
                    },
                    HttpStatusCode.BadGateway
                );
            }

            return HttpResult<GobidUserTokens>.Success(
                new GobidUserTokens(payload, ReadRefreshTokenFromSetCookie(response.Headers)));
        }

        var statusCode = response.StatusCode;
        var detail = statusCode switch
        {
            HttpStatusCode.BadRequest => "Datos inválidos para autenticación",
            HttpStatusCode.Unauthorized => "No autorizado para obtener o renovar token",
            _ => "Error temporal en el servicio de autenticación"
        };

        return HttpResult<GobidUserTokens>.Failure(
            "Error de autenticación en GOBID",
            new Dictionary<string, IEnumerable<string>>
            {
                { "errors", [detail] }
            },
            statusCode
        );
    }
}
