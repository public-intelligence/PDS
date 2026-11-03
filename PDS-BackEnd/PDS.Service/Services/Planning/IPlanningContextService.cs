using PDS.Domain.DTOs.Planning;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

/// <summary>
/// Resuelve el contexto de planificación del usuario: área organizacional (vía GOBID Personnel →
/// Position local → ascenso a unidad nivel 1) y permisos efectivos. Cachea el resultado 15 min.
/// </summary>
public interface IPlanningContextService
{
    Task<IHttpResult<PlanningContextDto>> GetContextAsync(
        int organizationId,
        Guid userId,
        string? userName,
        string? personnelId,
        IReadOnlyCollection<string>? tokenPermissions,
        CancellationToken ct = default);

    /// <summary>Invalida el contexto cacheado del usuario (p. ej. tras una reestructuración).</summary>
    void InvalidateContext(int organizationId, Guid userId, string? personnelId = null);
}
