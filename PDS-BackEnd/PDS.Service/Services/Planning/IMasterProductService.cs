using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

public interface IMasterProductService
{
    Task<IHttpResult<IReadOnlyList<MasterProductSearchItemDto>>> SearchAsync(int organizationId, string query, string? itemType = null, int take = 20);
    Task<IHttpResult<PagedResponseDto<MasterProductDto>>> ListAsync(int organizationId, string? q = null, string? state = null, Guid? consolidatorAreaId = null, bool? missingMapping = null, bool? requiresReview = null, string? unspscPrefix = null, int page = 1, int pageSize = 20);
    Task<IHttpResult<MasterProductDetailDto>> GetByIdAsync(int organizationId, Guid id);
    Task<IHttpResult<MasterProductDetailDto>> CreateAsync(int organizationId, CreateMasterProductDto dto, Guid userId, string? userName);
    Task<IHttpResult<MasterProductDetailDto>> UpdateAsync(int organizationId, Guid id, UpdateMasterProductDto dto, Guid userId, string? userName);
    Task<IHttpResult<MasterProductDetailDto>> UpdateMappingAsync(int organizationId, Guid id, UpdateMasterProductMappingDto dto, Guid userId, string? userName);
    Task<IHttpResult<MasterProductDetailDto>> RatifyConsolidatorAreaAsync(int organizationId, Guid id, RatifyConsolidatorAreaDto dto, Guid userId, string? userName);
    Task<IHttpResult<int>> BulkRatifyAsync(int organizationId, BulkRatifyByUnspscDto dto, Guid userId, string? userName);
    Task<IHttpResult<MasterProductDetailDto>> SetStateAsync(int organizationId, Guid id, string state, Guid userId, string? userName);
    Task<IHttpResult<MasterProductDetailDto>> RequestProductAsync(int organizationId, RequestMasterProductDto dto, Guid userId, string? userName, Guid? userAreaId, string? userAreaName);
    Task<IHttpResult<MasterProductMappingSummaryDto>> GetMappingSummaryAsync(int organizationId);
    Task<IHttpResult<bool>> DeleteAsync(int organizationId, Guid id, Guid userId, string? userName);
}
