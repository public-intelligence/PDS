

namespace PDS.Security;

/// <summary>
/// Cookie HttpOnly en la que PDS guarda el refresh token de GOBID de cada navegador.
/// El frontend y la API viven en dominios distintos, por eso el valor por defecto es SameSite=None; Secure.
/// </summary>
public class AuthCookieSettings
{
    public const string SectionName = "AuthCookie";

    public string Name { get; init; } = "pds_refresh_token";
    public string Path { get; init; } = "/api/v1/auth";
    public bool Secure { get; init; } = true;
    public SameSiteMode SameSite { get; init; } = SameSiteMode.None;
    public int MaxAgeDays { get; init; } = 7;
}
