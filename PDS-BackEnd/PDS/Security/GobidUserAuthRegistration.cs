using Microsoft.Extensions.Options;
using PDS.Service.Services;
using PDS.Service.Settings;

namespace PDS.Security;

/// <summary>
/// Registro del cliente de autenticación de usuarios contra GOBID (intercambio de código, refresh y logout).
/// Se usa desde Program.cs y desde las pruebas, para que ambas ejerciten el mismo registro.
/// </summary>
public static class GobidUserAuthRegistration
{
    /// <summary>Política CORS de /api/v1/auth/*: solo orígenes conocidos pueden leer respuestas con credenciales.</summary>
    public const string CorsPolicy = "PdsAuth";

    public static IServiceCollection AddGobidUserAuth(this IServiceCollection services, IConfiguration configuration)
    {
        // Sin cookies en el handler: el contenedor del handler es compartido por todos los usuarios y
        // GOBID usaría el refresh token de otra persona (incidente de sesiones cruzadas, 2026-10-23).
        services
            .AddHttpClient<IGobidAuthClient, GobidAuthClient>((sp, httpClient) =>
            {
                var settings = sp.GetRequiredService<IOptions<GobidAuthSettings>>().Value;
                httpClient.BaseAddress = new Uri(settings.BaseUrl);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { UseCookies = false });

        services.Configure<AuthCookieSettings>(configuration.GetSection(AuthCookieSettings.SectionName));

        var origins = (configuration.GetSection("AllowedClientOrigins").Get<List<string>>() ?? [])
            .Append(configuration["ClientBaseUrl"])
            .Select(ToOrigin)
            .Where(o => o is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        services.AddCors(o => o.AddPolicy(CorsPolicy, policy => policy
            .WithOrigins(origins!)
            .AllowAnyHeader()
            .WithMethods("POST")
            .AllowCredentials()));

        return services;
    }

    private static string? ToOrigin(string? url) =>
        Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri)
            ? uri.GetLeftPart(UriPartial.Authority)
            : null;
}
