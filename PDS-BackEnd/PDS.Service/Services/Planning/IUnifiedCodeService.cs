using PDS.Domain.DTOs.Planning;
using PDS.Domain.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using PDS.Domain.DTOs.Pagination;

namespace PDS.Service.Services.Planning;

/// <summary>
/// Catálogo UNSPSC de la DGCP. Es dato regulatorio de referencia: se carga con el
/// importador de la Maestra Institucional (<see cref="IMasterDataImportService"/>).
/// </summary>
public interface IUnifiedCodeService
{
    Task<IHttpResult<IEnumerable<UnifiedCodeDto>>> ListAsync(
        int organizationId, string? level, Guid? parentId, string? q, bool? enabled, int pageNumber = 1, int pageSize = 20);
    Task<IHttpResult<PagedResponseDto<UnifiedCodeDto>>> ListPagedAsync(
        int organizationId, string? level = null, Guid? parentId = null, string? codePrefix = null, string? q = null, bool? enabled = null, int page = 1, int pageSize = 50);
    Task<IHttpResult<UnifiedCodeDetailDto>> GetByIdAsync(int organizationId, Guid id);
}
