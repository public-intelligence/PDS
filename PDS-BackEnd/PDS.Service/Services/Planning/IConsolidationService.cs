using System;
using System.Threading.Tasks;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

public interface IConsolidationService
{
    Task<IHttpResult<ConsolidationBoardDto>> GetBoardAsync(int organizationId, Guid cycleId);
    Task<IHttpResult<ProductDemandBoardDto>> GetProductBoardAsync(int organizationId, Guid cycleId, Guid? consolidatorAreaId = null);

    Task<IHttpResult<ExpenseObjectsBoardPagedDto>> GetExpenseObjectsBoardAsync(int organizationId, Guid cycleId, int pageNumber = 1, int pageSize = 40, string? search = null);
    Task<IHttpResult<ProductsBoardPagedDto>> GetProductsBoardAsync(int organizationId, Guid cycleId, Guid? consolidatorAreaId = null, int pageNumber = 1, int pageSize = 40, string? search = null);
    Task<IHttpResult<ConsolidatorAreasBoardPagedDto>> GetConsolidatorAreasBoardAsync(int organizationId, Guid cycleId, int pageNumber = 1, int pageSize = 40, string? search = null);

    Task<IHttpResult> OpenAdjustmentsPhaseAsync(int organizationId, Guid cycleId, Guid userId, string? userName);
    Task<IHttpResult> SubmitAdjustmentsAsync(int organizationId, Guid planId, Guid userId, string? userName);
    Task<IHttpResult<byte[]>> ExportPaccAsync(int organizationId, Guid cycleId);
}

