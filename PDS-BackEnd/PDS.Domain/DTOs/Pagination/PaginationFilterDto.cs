using Microsoft.AspNetCore.Mvc;

namespace PDS.Domain.DTOs.Pagination;

public class PaginationFilterDto
{
    [FromQuery(Name = "pageNumber")]
    public int PageNumber { get; init; }

    [FromQuery(Name = "pageSize")]
    public int PageSize { get; init; }

    public PaginationFilterDto()
    {
        PageNumber = 1;
        PageSize = 20;

    }
    public PaginationFilterDto(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        PageSize = pageSize > 100 ? 100 : pageSize;
    }
}
