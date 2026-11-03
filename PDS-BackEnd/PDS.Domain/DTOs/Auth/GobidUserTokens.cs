namespace PDS.Domain.DTOs.Auth;

/// <summary>
/// Resultado de GOBID para un usuario: lo que se devuelve al navegador en el body y el refresh
/// token (leído del Set-Cookie de GOBID), que solo viaja en la cookie HttpOnly propia de PDS.
/// </summary>
public sealed record GobidUserTokens(AuthTokenResponseDto Response, string? RefreshToken);
