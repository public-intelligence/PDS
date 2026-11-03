using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using PDS.Service.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace PDS.Security;

/// <summary>
/// Esquema de autenticación para tokens M2M emitidos por GOBID.
/// Lee el Bearer del header Authorization, detecta tokens M2M por el KID
/// ("app:{applicationId}") y el claim "is_service_account=true", luego
/// los valida contra GOBID y construye el ClaimsPrincipal correspondiente.
/// </summary>
public sealed class GobidM2MAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "GobidM2M";

    private readonly IGobidM2MClient _m2mClient;

    public GobidM2MAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IGobidM2MClient m2mClient)
        : base(options, logger, encoder)
    {
        _m2mClient = m2mClient;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var rawToken = ExtractBearerToken();
        if (rawToken is null)
            return AuthenticateResult.NoResult();

        var jwt = SafeReadToken(rawToken);
        if (jwt is null)
            return AuthenticateResult.NoResult();

        // Solo manejamos tokens M2M: kid = "app:{applicationId}" + is_service_account = "true"
        if (!IsM2MToken(jwt))
            return AuthenticateResult.NoResult();

        var isValid = await _m2mClient.ValidateTokenAsync(rawToken);
        if (!isValid)
        {
            Logger.LogWarning("Token M2M rechazado por GOBID (kid={Kid}).", jwt.Header.Kid);
            return AuthenticateResult.Fail("Token M2M inválido o expirado.");
        }

        var principal = BuildPrincipal(jwt);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return AuthenticateResult.Success(ticket);
    }

    private string? ExtractBearerToken()
    {
        var authorization = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        return authorization["Bearer ".Length..].Trim();
    }

    private static JwtSecurityToken? SafeReadToken(string token)
    {
        try { return new JwtSecurityTokenHandler().ReadJwtToken(token); }
        catch { return null; }
    }

    private static bool IsM2MToken(JwtSecurityToken jwt)
    {
        var kid = jwt.Header.Kid;
        if (string.IsNullOrEmpty(kid) || !kid.StartsWith("app:", StringComparison.OrdinalIgnoreCase))
            return false;

        var isServiceAccount = jwt.Claims
            .FirstOrDefault(c => c.Type == "is_service_account")?.Value;

        return string.Equals(isServiceAccount, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static ClaimsPrincipal BuildPrincipal(JwtSecurityToken jwt)
    {
        // Construimos la identidad con los claims tal como vienen del token M2M de GOBID.
        // La firma ya fue verificada por GOBID en ValidateTokenAsync, así que los claims son confiables.
        var identity = new ClaimsIdentity(jwt.Claims, SchemeName);
        return new ClaimsPrincipal(identity);
    }
}
