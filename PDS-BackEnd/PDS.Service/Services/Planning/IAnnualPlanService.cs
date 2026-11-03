using PDS.Domain.DTOs.Planning;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

/// <summary>
/// Plan anual (POA) de un área dentro de un ciclo (M5). La consulta canónica filtra por
/// ciclo; los códigos de productos/tareas los genera el servidor (ISequenceService); la
/// validación de completitud es central (IPoaValidationService) y bloquea el sometimiento.
/// Autorización en el controller: editar exige poa:edit + área propia; devolver/aprobar,
/// poa:review. Ciclo cerrado → 409 en toda escritura.
/// </summary>
public interface IAnnualPlanService
{
    Task<IHttpResult<IEnumerable<AnnualPlanDto>>> ListAsync(int organizationId, Guid? cycleId, Guid? areaId, string? status);
    Task<IHttpResult<AnnualPlanWorkspaceDto>> GetWorkspaceAsync(int organizationId, Guid planId);
    Task<IHttpResult<PlanValidationDto>> GetValidationAsync(int organizationId, Guid planId);

    /// <summary>Área dueña y estado del plan (para el check de área del controller).</summary>
    Task<IHttpResult<PlanAccessInfo>> GetAccessInfoAsync(int organizationId, Guid planId);

    /// <summary>Crea un producto con valores por defecto y código de secuencia (patrón addRow del mock).</summary>
    Task<IHttpResult<PoaProductDto>> AddProductAsync(int organizationId, Guid planId, Guid userId, string? userName);

    Task<IHttpResult<PoaProductDto>> UpdateProductAsync(int organizationId, Guid planId, Guid productId, UpdatePoaProductDto dto, Guid userId, string? userName);
    Task<IHttpResult> DeleteProductAsync(int organizationId, Guid planId, Guid productId, Guid userId, string? userName);

    /// <summary>Somete a revisión: bloquea con las incidencias si el plan está incompleto.</summary>
    Task<IHttpResult<AnnualPlanDto>> SubmitAsync(int organizationId, Guid planId, Guid userId, string? userName);

    Task<IHttpResult<AnnualPlanDto>> ReturnAsync(int organizationId, Guid planId, ReturnPlanDto dto, Guid userId, string? userName);
    Task<IHttpResult<AnnualPlanDto>> ApproveAsync(int organizationId, Guid planId, Guid userId, string? userName);
}

/// <summary>Datos mínimos para el check de autorización por área en el controller.</summary>
public record PlanAccessInfo
{
    public required Guid AreaId { get; init; }
    public required string Status { get; init; }
    public required bool CycleClosed { get; init; }
}
