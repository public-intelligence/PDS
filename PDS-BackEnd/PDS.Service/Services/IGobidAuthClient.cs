using PDS.Domain.DTOs.Auth;
using PDS.Domain.Response;

namespace PDS.Service.Services;

public interface IGobidAuthClient
{
    Task<IHttpResult<GobidUserTokens>> ExchangeCodeAsync(ExchangeCodeGobidRequestDto requestDto, CancellationToken cancellationToken = default);

    /// <summary>Renueva la sesión del usuario dueño de <paramref name="refreshToken"/> (cookie del navegador).</summary>
    Task<IHttpResult<GobidUserTokens>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Revoca en GOBID el refresh token del usuario. Best-effort.</summary>
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}
