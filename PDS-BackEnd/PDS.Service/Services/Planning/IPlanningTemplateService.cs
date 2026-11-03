using PDS.Domain.DTOs.Planning;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

public interface IPlanningTemplateService
{
    Task<IHttpResult<IEnumerable<PlanningTemplateDto>>> ListAsync(int organizationId);
    Task<IHttpResult<PlanningTemplateDetailDto>> GetByIdAsync(int organizationId, Guid id);
    Task<IHttpResult<PlanningTemplateDetailDto>> CreateAsync(int organizationId, CreatePlanningTemplateDto dto, Guid userId, string? userName);
    Task<IHttpResult<PlanningTemplateDetailDto>> UpdateAsync(int organizationId, Guid id, UpdatePlanningTemplateDto dto);
    Task<IHttpResult<PlanningTemplateColumnDto>> AddColumnAsync(int organizationId, Guid templateId, CreatePlanningTemplateColumnDto dto, Guid userId, string? userName);
    Task<IHttpResult<PlanningTemplateColumnDto>> UpdateColumnAsync(int organizationId, Guid templateId, Guid columnId, UpdatePlanningTemplateColumnDto dto);
    Task<IHttpResult<PlanningTemplateDetailDto>> ActivateAsync(int organizationId, Guid id, Guid userId, string? userName);
    Task<IHttpResult<PlanningTemplateDetailDto>> SeedStandardColumnsAsync(int organizationId, Guid templateId, Guid userId, string? userName);
    Task<IHttpResult<PlanningTemplateDetailDto>> DeleteColumnAsync(int organizationId, Guid templateId, Guid columnId, Guid userId, string? userName);
    Task<IHttpResult<bool>> DeleteAsync(int organizationId, Guid id, Guid userId, string? userName);
}
