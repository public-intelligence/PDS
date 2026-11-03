using PDS.Domain.DTOs.StructureVersion;
using PDS.Domain.Enums;
using PDS.Domain.Response;

namespace PDS.Service.Services;

/// <summary>
/// Gestiona el ciclo de vida de las versiones de la estructura: borrador, flujo de aprobación,
/// publicación (con fotografía inmutable) y comparación (RF-VH-03 / RF-FA-01).
/// </summary>
public interface IStructureVersionService
{
    Task<IHttpResult<IEnumerable<StructureVersionSummaryDto>>> ListAsync(int organizationId, VersionStatus? status);
    Task<IHttpResult<StructureVersionDetailDto>> GetAsync(int organizationId, Guid id);

    Task<IHttpResult<StructureVersionDetailDto>> CreateDraftAsync(
        int organizationId, Guid userId, string? userName, CreateStructureVersionDto dto);

    Task<IHttpResult> SubmitReviewAsync(int organizationId, Guid id, Guid userId, string? userName, string? comment);
    Task<IHttpResult> ApproveAsync(int organizationId, Guid id, Guid userId, string? userName, string? comment);
    Task<IHttpResult> RejectAsync(int organizationId, Guid id, Guid userId, string? userName, string? comment);

    Task<IHttpResult<StructureVersionSummaryDto>> PublishAsync(
        int organizationId, Guid id, Guid userId, string? userName, PublishVersionDto dto);

    Task<IHttpResult<VersionComparisonDto>> CompareAsync(
        int organizationId, Guid? fromVersionId, DateOnly? fromDate, Guid? toVersionId, DateOnly? toDate);
}
