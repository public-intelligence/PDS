using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

public interface IPlanningCycleService
{
    Task<IHttpResult<IEnumerable<PlanningCycleDto>>> ListAsync(int organizationId);
    Task<IHttpResult<PlanningCycleDetailDto>> GetByIdAsync(int organizationId, Guid id);
    Task<IHttpResult<PlanningCycleDetailDto>> CreateDraftAsync(int organizationId, CreatePlanningCycleDto dto, Guid userId, string? userName);
    Task<IHttpResult<PlanningCycleDetailDto>> UpdateDraftAsync(int organizationId, Guid id, UpdatePlanningCycleDto dto, Guid userId, string? userName);
    Task<IHttpResult<PlanningCycleDetailDto>> PublishAsync(int organizationId, Guid id, Guid userId, string? userName);
    Task<IHttpResult> CloseAsync(int organizationId, Guid id, Guid userId, string? userName);
    Task<IHttpResult> ReopenAsync(int organizationId, Guid id, Guid userId, string? userName);
    Task<IHttpResult<IEnumerable<EligibleAreaDto>>> GetEligibleAreasAsync(int organizationId);
}
