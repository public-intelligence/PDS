using Microsoft.AspNetCore.Http;
using PDS.Domain.DTOs.Pagination;

namespace PDS.Service.Services;

public interface IPaginationService
{
    PagedResponseDto<T> BuildPaginationUrl<T>(HttpContext httpContext, int pageNumber, int pageSize, int totalCount, IEnumerable<T> entityDtos);
    void SetPaginationHeaders<T>( HttpResponse httpResponse, PagedResponseDto<T> pagedResponseDto);
}
