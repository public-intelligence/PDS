using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PDS.Domain.DTOs.Auth;
using PDS.Security;
using PDS.Service.Services;

namespace PDS.Controllers;

/// <summary>
/// Autenticación de usuarios con GOBID. El refresh token de cada navegador vive solo en una cookie
/// HttpOnly propia de PDS (AuthCookieSettings) y se reenvía a GOBID de forma explícita por petición.
/// </summary>
[ApiController]
[Route("auth")]
[Consumes("application/json")]
[Produces("application/json")]
[EnableCors(GobidUserAuthRegistration.CorsPolicy)]
public class AuthController : ControllerBase
{
    private readonly IGobidAuthClient _gobidAuthClient;
    private readonly ILogger<AuthController> _logger;
    private readonly AuthCookieSettings _cookie;
    private readonly string _clientBaseUrl;

    public AuthController(
        IGobidAuthClient gobidAuthClient,
        ILogger<AuthController> logger,
        IConfiguration configuration,
        IOptions<AuthCookieSettings> cookieSettings)
    {
        _gobidAuthClient = gobidAuthClient;
        _logger = logger;
        _cookie = cookieSettings.Value;
        _clientBaseUrl = configuration["ClientBaseUrl"]
            ?? throw new InvalidOperationException("ClientBaseUrl no está configurado.");
    }

    [AllowAnonymous]
    [HttpPost("token")]
    [ProducesResponseType(typeof(AuthTokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<AuthTokenResponseDto>> ExchangeCode(
        [FromBody] ExchangeCodeRequestDto request,
        CancellationToken cancellationToken)
    {
        var gobidRequest = new ExchangeCodeGobidRequestDto
        {
            Code = request.Code,
            OrganizationApplicationId = request.OrganizationApplicationId,
            RedirectUri = _clientBaseUrl
        };

        var result = await _gobidAuthClient.ExchangeCodeAsync(gobidRequest, cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Falló ExchangeCode contra GOBID con estado {StatusCode}", (int)result.StatusCode);
            return StatusCode((int)result.StatusCode, result.Error);
        }

        SetRefreshCookie(result.Value.RefreshToken);
        return Ok(result.Value.Response);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthTokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<AuthTokenResponseDto>> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(_cookie.Name, out var refreshToken) || string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized(new { message = "Sesión no encontrada. Inicie sesión nuevamente." });

        var result = await _gobidAuthClient.RefreshTokenAsync(refreshToken, cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Falló RefreshToken contra GOBID con estado {StatusCode}", (int)result.StatusCode);
            if ((int)result.StatusCode is >= 400 and < 500)
                DeleteRefreshCookie();
            return StatusCode((int)result.StatusCode, result.Error);
        }

        SetRefreshCookie(result.Value.RefreshToken);
        return Ok(result.Value.Response);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(_cookie.Name, out var refreshToken) && !string.IsNullOrWhiteSpace(refreshToken))
            await _gobidAuthClient.LogoutAsync(refreshToken, cancellationToken);

        DeleteRefreshCookie();
        return NoContent();
    }

    private void SetRefreshCookie(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogWarning("GOBID no devolvió refresh token; la sesión no podrá renovarse.");
            return;
        }

        Response.Cookies.Append(_cookie.Name, refreshToken, BuildCookieOptions(DateTimeOffset.UtcNow.AddDays(_cookie.MaxAgeDays)));
    }

    private void DeleteRefreshCookie() =>
        Response.Cookies.Delete(_cookie.Name, BuildCookieOptions(expires: null));

    private CookieOptions BuildCookieOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = _cookie.Secure,
        SameSite = _cookie.SameSite,
        Path = _cookie.Path,
        Expires = expires,
        IsEssential = true
    };
}
