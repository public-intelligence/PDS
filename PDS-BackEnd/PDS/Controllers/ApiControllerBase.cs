using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using PDS.Domain.Enums;
using PDS.Domain.Response;
using PDS.Service.Services.Calendario;

namespace PDS.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    private const string M2MAccessPermission = "access:M2M";

    protected bool TryGetUserAndOrganizationIds(out Guid userId, out int organizationId)
    {
        userId = Guid.Empty;
        organizationId = default;

        var userIdClaim = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("user_id")
            ?? User.FindFirstValue("userId");

        var organizationIdClaim = User.FindFirstValue("organization_id")
            ?? User.FindFirstValue("organizationId")
            ?? User.FindFirstValue("org_id");

        var validUser = Guid.TryParse(userIdClaim, out userId);
        var validOrg = int.TryParse(organizationIdClaim, out organizationId);

        var isServiceAccount = string.Equals(
            User.FindFirstValue("is_service_account"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (isServiceAccount)
        {
            if (!validOrg) organizationId = 1;
            return true;
        }

        return validUser && validOrg;
    }

    /// <summary>Identificador del personal asociado al usuario autenticado (claim personnel_id / personnelId).</summary>
    protected string? GetPersonnelId()
        => User.FindFirstValue("personnel_id") ?? User.FindFirstValue("personnelId");

    /// <summary>Nombre legible del usuario autenticado (claim person_full_name), para denormalizar en la trazabilidad.</summary>
    protected string? GetUserName()
        => User.FindFirstValue("person_full_name") ?? User.FindFirstValue(ClaimTypes.Name);

    /// <summary>Nombre y acrónimo de la organización (tenant) tomados de los claims del token.</summary>
    protected (string? Name, string? Acronym) GetOrganizationProfile()
        => (User.FindFirstValue("organization_name"), User.FindFirstValue("organization_acronym"));

    protected Task<IReadOnlyCollection<string>?> GetPermissionFromTokenAsync()
    {
        var claims = User.FindAll("permissions")
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();

        if (claims.Count == 0)
        {
            return Task.FromResult<IReadOnlyCollection<string>?>(null);
        }

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var claimValue in claims)
        {
            var trimmed = claimValue.Trim();
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                try
                {
                    var permissions = JsonSerializer.Deserialize<string[]>(trimmed);
                    if (permissions is not null)
                    {
                        foreach (var p in permissions)
                        {
                            if (!string.IsNullOrWhiteSpace(p))
                                result.Add(p.Trim());
                        }
                    }
                }
                catch (JsonException)
                {
                    result.Add(trimmed);
                }
            }
            else
            {
                result.Add(trimmed);
            }
        }

        return Task.FromResult<IReadOnlyCollection<string>?>(result.Count > 0 ? result : Array.Empty<string>());
    }

    /// <summary>RF-LO-44: canal de auditoría de la petición (<c>X-Client-Application</c> presente = API externa, ausente = UI de PDS).</summary>
    protected CanalCambio CanalActual => CanalResolver.FromHeader(Request.Headers[CanalResolver.HeaderName].FirstOrDefault());

    /// <summary>
    /// Quien llama a un endpoint del calendario laboral (tenant, usuario, canal y permisos del token), o <c>null</c> si el token no
    /// trae usuario y organización.
    /// </summary>
    protected async Task<CalendarioActor?> TryBuildCalendarioActorAsync()
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId)) return null;
        var permisos = await GetPermissionFromTokenAsync() ?? Array.Empty<string>();
        return new CalendarioActor(organizationId, userId, GetUserName(), CanalActual, permisos);
    }

    /// <summary>Valor de <c>If-None-Match</c> de la petición (vacío si no viene).</summary>
    protected string? IfNoneMatchHeader => Request.Headers.IfNoneMatch.Count == 0 ? null : Request.Headers.IfNoneMatch.ToString();

    /// <summary>
    /// RNF-LO-02: responde una lectura cacheable. Siempre pone el <c>ETag</c>; si el cliente ya lo tiene responde 304 sin cuerpo.
    /// </summary>
    protected ActionResult ResponderLectura<T>(IHttpResult<Lectura<T>> result) where T : class
    {
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        Response.Headers.ETag = result.Value.ETag;
        return result.Value.NoModificado ? StatusCode(StatusCodes.Status304NotModified) : Ok(result.Value.Valor);
    }

    protected async Task<bool> HasPermissionAsync(string permission)
        => (await GetPermissionFromTokenAsync())?.Contains(permission, StringComparer.OrdinalIgnoreCase) == true;

    protected async Task<ActionResult?> RequireM2MServiceAccountAsync(params string[] requiredPermissions)
    {
        var isServiceAccount = string.Equals(
            User.FindFirstValue("is_service_account"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (!isServiceAccount)
        {
            return StatusCode(403, new
            {
                errors = new[] { "Las rutas M2M están reservadas para cuentas de servicio M2M." }
            });
        }

        return await ValidateM2MPermissionsAsync(requiredPermissions);
    }

    protected async Task<ActionResult?> ValidateM2MPermissionsAsync(params string[] requiredPermissions)
    {
        var isServiceAccount = string.Equals(
            User.FindFirstValue("is_service_account"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (!isServiceAccount)
        {
            return null;
        }

        var permissions = await GetPermissionFromTokenAsync();
        if (permissions is null || permissions.Count == 0)
        {
            return Unauthorized(new
            {
                errors = new[] { "No se pudo obtener el permiso desde el token M2M." }
            });
        }

        if (!permissions.Contains(M2MAccessPermission, StringComparer.OrdinalIgnoreCase))
        {
            return StatusCode(403, new
            {
                errors = new[] { $"El token M2M debe incluir el permiso '{M2MAccessPermission}'." }
            });
        }

        if (requiredPermissions.Length == 0)
        {
            return null;
        }

        var missingPermission = requiredPermissions.FirstOrDefault(
            required => !permissions.Contains(required, StringComparer.OrdinalIgnoreCase));

        if (missingPermission is null)
        {
            return null;
        }

        return StatusCode(403, new
        {
            errors = new[]
            {
                $"El token M2M requiere también el permiso '{missingPermission}'."
            }
        });
    }
}
