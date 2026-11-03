using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

public interface IRequirementService
{
    Task<IHttpResult<PagedResponseDto<RequirementDto>>> ListAsync(
        int organizationId,
        Guid? cycleId,
        Guid? consolidatorAreaId,
        Guid? originAreaId,
        string? status,
        PaginationFilterDto pagination);

    Task<IHttpResult<RequirementDetailDto>> GetByIdAsync(int organizationId, Guid id);

    Task<IHttpResult<RequirementDetailDto>> CreateFromTaskAsync(
        int organizationId,
        CreateRequirementDto dto,
        Guid userId,
        string? userName,
        Guid originAreaId,
        string originAreaName);

    Task<IHttpResult<RequirementDetailDto>> UpdateAsync(
        int organizationId,
        Guid id,
        UpdateRequirementDto dto,
        Guid userId,
        string? userName,
        Guid userAreaId);

    Task<IHttpResult> DeleteAsync(
        int organizationId,
        Guid id,
        Guid userId,
        string? userName,
        Guid userAreaId);

    // Las operaciones de consolidación reciben TODAS las áreas consolidadoras del usuario
    // (PlanningContextDto.ConsolidatorAreaIds). Una colección vacía deniega siempre: tener el
    // permiso no basta, hay que pertenecer a la consolidadora del requerimiento.
    Task<IHttpResult<RequirementDetailDto>> ConfirmPriceAsync(
        int organizationId,
        Guid id,
        decimal price,
        Guid userId,
        string? userName,
        IReadOnlyCollection<Guid> userConsolidatorAreaIds);

    Task<IHttpResult<RequirementDetailDto>> DecideAsync(
        int organizationId,
        Guid id,
        RequirementDecisionDto dto,
        Guid userId,
        string? userName,
        IReadOnlyCollection<Guid> userConsolidatorAreaIds);

    Task<IHttpResult<RequirementDetailDto>> ReduceAsync(
        int organizationId,
        Guid id,
        decimal approvedAmount,
        string reason,
        Guid userId,
        string? userName,
        IReadOnlyCollection<Guid> userConsolidatorAreaIds);

    Task<IHttpResult<RequirementDetailDto>> MergeAsync(
        int organizationId,
        MergeRequirementsDto dto,
        Guid userId,
        string? userName,
        IReadOnlyCollection<Guid> userConsolidatorAreaIds);

    Task<IHttpResult<IEnumerable<RequirementDto>>> ListByTaskAsync(int organizationId, Guid taskId);

    Task<IHttpResult<IEnumerable<PoaTaskOptionDto>>> GetAvailableTasksAsync(
        int organizationId,
        Guid? cycleId,
        Guid? areaId,
        Guid? productId,
        string? search);

    Task<IHttpResult<RequirementCommentDto>> AddCommentAsync(
        int organizationId,
        Guid id,
        string text,
        Guid userId,
        string? userName,
        string? areaName);
}
