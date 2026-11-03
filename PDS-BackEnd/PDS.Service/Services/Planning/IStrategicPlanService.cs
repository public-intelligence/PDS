using PDS.Domain.DTOs.Planning;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

/// <summary>
/// PEI versionado por enmiendas (M3). Reglas replicadas del contrato funcional aprobado:
/// como máximo un Draft y un Current por organización; solo el Draft es editable; las metas
/// de años transcurridos son inmutables; los elementos con historia se descontinúan, nunca
/// se eliminan; toda mutación anota el registro de cambios y la bitácora general.
/// La autorización (planning:pei:edit / planning:pei:approve) vive en el controller.
/// </summary>
public interface IStrategicPlanService
{
    Task<IHttpResult<IEnumerable<StrategicPlanDto>>> ListAsync(int organizationId);
    Task<IHttpResult<StrategicPlanDetailDto>> GetByIdAsync(int organizationId, Guid id);

    /// <summary>Crea el primer PEI de la organización (Draft, versión 1). Falla si ya existe alguno.</summary>
    Task<IHttpResult<StrategicPlanDetailDto>> CreateInitialAsync(int organizationId, CreateStrategicPlanDto dto, Guid userId, string? userName);

    /// <summary>Abre una enmienda: clona la versión vigente en Draft conservando los LineageId.</summary>
    Task<IHttpResult<StrategicPlanDetailDto>> StartAmendmentAsync(int organizationId, CreateAmendmentDto dto, Guid userId, string? userName);

    /// <summary>Descarta la enmienda en curso (única ruta de borrado: hard delete del grafo Draft).</summary>
    Task<IHttpResult> DiscardDraftAsync(int organizationId, Guid userId, string? userName);

    /// <summary>Aprueba la enmienda (MAE): Draft→Current y la vigente anterior→Historical, en transacción.</summary>
    Task<IHttpResult<StrategicPlanDto>> ApproveDraftAsync(int organizationId, Guid userId, string? userName);

    Task<IHttpResult<StrategicPlanDto>> UpdatePlanAsync(int organizationId, Guid planId, UpdateStrategicPlanDto dto, Guid userId, string? userName);

    Task<IHttpResult<StrategicFocusDto>> AddFocusAsync(int organizationId, Guid planId, CreateStrategicFocusDto dto, Guid userId, string? userName);
    Task<IHttpResult<StrategicFocusDto>> UpdateFocusAsync(int organizationId, Guid planId, Guid focusId, UpdateStrategicFocusDto dto, Guid userId, string? userName);
    Task<IHttpResult> DeleteFocusAsync(int organizationId, Guid planId, Guid focusId, Guid userId, string? userName);

    Task<IHttpResult<StrategicObjectiveDto>> AddObjectiveAsync(int organizationId, Guid planId, Guid focusId, CreateStrategicObjectiveDto dto, Guid userId, string? userName);
    Task<IHttpResult<StrategicObjectiveDto>> UpdateObjectiveAsync(int organizationId, Guid planId, Guid objectiveId, UpdateStrategicObjectiveDto dto, Guid userId, string? userName);
    Task<IHttpResult> DeleteObjectiveAsync(int organizationId, Guid planId, Guid objectiveId, Guid userId, string? userName);

    Task<IHttpResult<StrategicProductDto>> AddProductAsync(int organizationId, Guid planId, Guid objectiveId, CreateStrategicProductDto dto, Guid userId, string? userName);
    Task<IHttpResult<StrategicProductDto>> UpdateProductAsync(int organizationId, Guid planId, Guid productId, UpdateStrategicProductDto dto, Guid userId, string? userName);
    Task<IHttpResult> DeleteProductAsync(int organizationId, Guid planId, Guid productId, Guid userId, string? userName);
    Task<IHttpResult<StrategicProductDto>> DiscontinueProductAsync(int organizationId, Guid planId, Guid productId, DiscontinueDto dto, Guid userId, string? userName);

    /// <summary>Reemplazo saneado de unidades (patrón reemplazarUnidadesPei del mock).</summary>
    Task<IHttpResult<StrategicProductDto>> ReplaceMeasureUnitsAsync(int organizationId, Guid planId, Guid productId, ReplaceMeasureUnitsDto dto, Guid userId, string? userName);

    Task<IHttpResult<StrategicProductDto>> DiscontinueMeasureUnitAsync(int organizationId, Guid planId, Guid unitId, DiscontinueDto dto, Guid userId, string? userName);
}
