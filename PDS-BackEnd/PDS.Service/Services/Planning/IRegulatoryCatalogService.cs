using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

public interface IRegulatoryCatalogService
{
    Task<IHttpResult<PagedResponseDto<ExpenseObjectDto>>> ListExpenseObjectsAsync(int organizationId, string? q = null, bool? onlyDetail = null, int page = 1, int pageSize = 50);
    Task<IHttpResult<PagedResponseDto<AccountingAccountDto>>> ListAccountingAccountsAsync(int organizationId, string? q = null, bool? onlyPostable = null, int page = 1, int pageSize = 50);
    Task<IHttpResult<IReadOnlyList<ConsolidatorAreaProfileDto>>> ListConsolidatorAreaProfilesAsync(int organizationId);

    // ───────────── Catálogo Regulatorio MasterSoft ─────────────
    Task<IHttpResult<PagedResponseDto<MastersoftAccountDto>>> ListMastersoftAccountsAsync(int organizationId, string? q = null, int page = 1, int pageSize = 50);
    Task<IHttpResult<PagedResponseDto<MastersoftDigecobMappingDto>>> ListMastersoftMappingsAsync(int organizationId, string? q = null, string? filterMode = null, int page = 1, int pageSize = 50);
    Task<IHttpResult<MastersoftDigecobMappingDto>> CreateOrUpdateMastersoftMappingAsync(int organizationId, CreateMastersoftMappingDto dto);
    Task<IHttpResult<bool>> DeleteMastersoftMappingAsync(int organizationId, Guid mappingId);
    Task<IHttpResult<MastersoftImportSummaryDto>> ImportMastersoftCatalogAndMappingsAsync(int organizationId, System.IO.Stream stream);
    Task<IHttpResult<byte[]>> ExportMastersoftHomologationReportAsync(int organizationId);
}
