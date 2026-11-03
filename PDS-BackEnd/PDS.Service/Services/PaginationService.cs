using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using PDS.Domain.DTOs.Pagination;
namespace PDS.Service.Services;

public class PaginationService : IPaginationService
{
    private readonly ILogger<PaginationService> _logger;

    public PaginationService(ILogger<PaginationService> logger)
    {
        _logger = logger;
    }
    public PagedResponseDto<T> BuildPaginationUrl<T>(HttpContext httpContext, int pageNumber, int pageSize, int totalCount, IEnumerable<T> entityDtos)
    {
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var firstPage = pageNumber > 1 ? BuildPaginationUrl(httpContext, 1) : null;
        var lastPage = pageNumber < totalPages ? BuildPaginationUrl(httpContext, totalPages) : null;
        var nextPage = pageNumber < totalPages ? BuildPaginationUrl(httpContext, pageNumber + 1) : null;
        var previousPage = pageNumber > 1 ? BuildPaginationUrl(httpContext, pageNumber - 1) : null;

        var pagedResponse = new PagedResponseDto<T>(entityDtos, pageNumber, pageSize, totalCount, firstPage, lastPage, nextPage, previousPage);
        return pagedResponse;
    }
    public void SetPaginationHeaders<T>(HttpResponse httpResponse, PagedResponseDto<T> pagedResponseDto)
    {
        httpResponse.Headers["X-Current-Page"] = pagedResponseDto.PageNumber.ToString();
        httpResponse.Headers["X-Page-Count"] = pagedResponseDto.TotalPages.ToString();
        httpResponse.Headers["X-Page-Size"] = pagedResponseDto.PageSize.ToString();
        httpResponse.Headers["X-Total"] = pagedResponseDto.TotalRecords.ToString();

        var links = new List<string>();

        if (pagedResponseDto.FirstPage != null) links.Add($"<{pagedResponseDto.FirstPage}>; rel=\"first\"");
        if (pagedResponseDto.LastPage != null) links.Add($"<{pagedResponseDto.LastPage}>; rel=\"last\"");
        if (pagedResponseDto.NextPage != null) links.Add($"<{pagedResponseDto.NextPage}>; rel=\"next\"");
        if (pagedResponseDto.PreviousPage != null) links.Add($"<{pagedResponseDto.PreviousPage}>; rel=\"prev\"");

        if (links.Count != 0)
        {
            httpResponse.Headers["Link"] = string.Join(", ", links);
        }
        httpResponse.Headers["Access-Control-Expose-Headers"] = "X-Current-Page, X-Page-Count, X-Page-Size, X-Total, Link";
    }
    private static string BuildPaginationUrl(HttpContext httpContext, int pageNumber)
    {
        var requestUrl = httpContext.Request.GetEncodedUrl();
        var uri = new Uri(requestUrl);
        var baseUrl = uri.GetLeftPart(UriPartial.Path);
        var queryParams = QueryHelpers.ParseQuery(uri.Query)
            .SelectMany((x, _) => x.Value, (col, value) => new KeyValuePair<string, string>(col.Key, col.Key == "pageNumber" ? pageNumber.ToString() : value!))
            .ToDictionary(x => x.Key, x => x.Value);
        queryParams["pageNumber"] = pageNumber.ToString();
        var finalUrl = QueryHelpers.AddQueryString(baseUrl, queryParams);
        return finalUrl;
    }

}
