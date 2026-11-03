using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

public interface IBudgetCeilingService
{
    Task<IHttpResult<PagedResponseDto<BudgetCeilingDto>>> ListAsync(
        int organizationId,
        int? fiscalYear = null,
        string? fundingSource = null,
        string? status = null,
        string? q = null,
        Guid? expenseObjectId = null,
        int page = 1,
        int pageSize = 20);

    Task<IHttpResult<BudgetCeilingDetailDto>> GetByIdAsync(int organizationId, Guid id);

    Task<IHttpResult<BudgetCeilingDetailDto>> CreateAsync(int organizationId, CreateBudgetCeilingDto dto, Guid userId, string? userName);

    Task<IHttpResult<BudgetCeilingDetailDto>> UpdateAsync(int organizationId, Guid id, UpdateBudgetCeilingDto dto, Guid userId, string? userName);

    Task<IHttpResult<BudgetCeilingDetailDto>> AddAdjustmentAsync(int organizationId, Guid ceilingId, CreateBudgetAdjustmentDto dto, Guid userId, string? userName);

    Task<IHttpResult<BudgetValidationResultDto>> ValidateRequirementBudgetAsync(int organizationId, BudgetValidationRequestDto request);

    Task<IHttpResult<BudgetImpactPreviewDto>> PreviewImpactAsync(int organizationId, Guid ceilingId, decimal proposedCeilingAmount);

    Task<IHttpResult<IReadOnlyList<BudgetMonitoringDto>>> GetMonitoringReportAsync(int organizationId, int? fiscalYear = null, string? fundingSource = null);

    Task<IHttpResult<IReadOnlyList<AreaConsumptionReportDto>>> GetAreaConsumptionReportAsync(int organizationId, int? fiscalYear = null, Guid? expenseObjectId = null);

    Task<IHttpResult<IReadOnlyList<CrossExecutionReportDto>>> GetCrossExecutionReportAsync(int organizationId, int? fiscalYear = null);

    Task RecalculateCeilingCycleAsync(int organizationId, Guid ceilingId);
}
