namespace PDS.Domain.DTOs.Pagination;

public class PagedResponseDto<T> 
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalRecords { get; set; }
    public string? FirstPage { get; set; }
    public string? LastPage { get; set; }
    public string? NextPage { get; set; }
    public string? PreviousPage { get; set; }
    public IEnumerable<T> Value { get; set; }
   

    public PagedResponseDto(IEnumerable<T> value, int pageNumber, int pageSize, int totalRecords, string? firstPage = null, string? lastPage = null, string? nextPage = null, string? previousPage = null)
    {
        Value = value;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalRecords = totalRecords;
        TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
        FirstPage = firstPage;
        LastPage = lastPage;
        NextPage = nextPage;
        PreviousPage = previousPage;
    }
}
