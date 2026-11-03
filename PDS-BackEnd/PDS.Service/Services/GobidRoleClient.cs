using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PDS.Domain.Response;
using PDS.Service.Settings;
using System.Net;
using System.Net.Http.Json;

namespace PDS.Service.Services;

/// <summary>
/// Cliente del endpoint de roles de GOBID (PUT api/{version}/Role → RoleService.UpsertAsync).
/// El Bearer M2M lo inyecta GobidM2MBearerHandler (encadenado al HttpClient tipado en Program.cs).
/// </summary>
public sealed class GobidRoleClient : IGobidRoleClient
{
    private readonly HttpClient _httpClient;
    private readonly GobidM2MSettings _m2mSettings;
    private readonly GobidRoleSettings _roleSettings;
    private readonly ILogger<GobidRoleClient> _logger;

    public GobidRoleClient(
        HttpClient httpClient,
        IOptions<GobidM2MSettings> m2mSettings,
        IOptions<GobidRoleSettings> roleSettings,
        ILogger<GobidRoleClient> logger)
    {
        _httpClient = httpClient;
        _m2mSettings = m2mSettings.Value;
        _roleSettings = roleSettings.Value;
        _logger = logger;
    }

    public async Task<IHttpResult<bool>> UpsertPositionRoleAsync(
        Guid positionId,
        string positionName,
        CancellationToken ct = default)
    {
        if (_roleSettings.OrganizationApplicationId <= 0)
        {
            _logger.LogError("GOBID Roles: {Section}:OrganizationApplicationId no está configurado.", GobidRoleSettings.SectionName);
            return Fail("No está configurado el OrganizationApplicationId de GOBID", HttpStatusCode.InternalServerError);
        }

        var endpoint = $"api/{_m2mSettings.ApiVersion}/Role";
        var request = new
        {
            roleName = positionName,
            roleDescription = positionName,
            setAutomaticClaims = true,
            organizationApplicationId = _roleSettings.OrganizationApplicationId,
            positionId,
            isActive = true,
            isSystemRole = false
        };

        try
        {
            using var response = await _httpClient.PutAsJsonAsync(endpoint, request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("GOBID Roles: PUT {Endpoint} → {Status}: {Body}", endpoint, (int)response.StatusCode, body);
                return Fail("GOBID no pudo crear el rol de la posición", HttpStatusCode.BadGateway);
            }

            return HttpResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear en GOBID el rol de la posición {PositionId}.", positionId);
            return Fail("No se pudo contactar el servicio de roles de GOBID", HttpStatusCode.BadGateway);
        }
    }

    public async Task<IHttpResult<bool>> DeletePositionRoleAsync(
        Guid positionId,
        CancellationToken ct = default)
    {
        // GOBID elimina todos los roles asociados a la posición, en cualquier organización-aplicación.
        var endpoint = $"api/{_m2mSettings.ApiVersion}/Role/by-position/{positionId}";

        try
        {
            using var response = await _httpClient.DeleteAsync(endpoint, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("GOBID Roles: DELETE {Endpoint} → {Status}: {Body}", endpoint, (int)response.StatusCode, body);
                return Fail("GOBID no pudo eliminar los roles de la posición", HttpStatusCode.BadGateway, "Error al eliminar rol en GOBID");
            }

            return HttpResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar en GOBID los roles de la posición {PositionId}.", positionId);
            return Fail("No se pudo contactar el servicio de roles de GOBID", HttpStatusCode.BadGateway, "Error al eliminar rol en GOBID");
        }
    }

    private static HttpResult<bool> Fail(string message, HttpStatusCode code, string title = "Error al crear rol en GOBID")
    {
        var detail = new Dictionary<string, IEnumerable<string>> { { "errors", [message] } };
        return HttpResult<bool>.Failure(title, detail, code);
    }
}
