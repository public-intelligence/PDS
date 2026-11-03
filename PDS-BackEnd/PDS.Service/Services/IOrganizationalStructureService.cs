using Microsoft.AspNetCore.Http;
using PDS.Domain.DTOs.OrganizationalStructure;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.Response;

namespace PDS.Service.Services;

public interface IOrganizationalStructureService
{
    Task<IResult<bool>> AnyAsync(int organizationId, Guid organizationalStructureId);
    Task<IHttpResult<IEnumerable<OrganizationalStructureDto>>> ListAsync(
        int organizationId
    );
    Task<IHttpResult<PagedResponseDto<OrganizationalStructureWhioutDependenciesDto>>> ListPaginatedAsync(
        int organizationId,
        PaginationFilterDto paginationFilter,
        HttpContext httpContext,
        string? searchTerm = null,
        bool includeExternal = true
    );
    Task<IHttpResult<OrganizationalStructureDto>> GetAsync(
        int organizationId,
        Guid organizationalStructureId,
        Guid requestedById
    );
    Task<IHttpResult<OrganizationalStructureDto>> CreateAsync(
        int organizationId,
        Guid requestedById,
        CreateOrganizationalStructureDto createOrganizationalStructureDto
    );
    Task<IHttpResult<IEnumerable<OrganizationalStructureDto>>> ImportAsync(
        int organizationId,
        Guid requestedById,
        ImportStructureDto importStructureDto
    );
    Task<IHttpResult<OrganizationalStructureDto>> UpdateAsync(
        int organizationId,
        Guid organizationalStructureId,
        Guid requestedById,
        UpdateOrganizationalStructureDto updateOrganizationalStructureDto
    );
    Task<IHttpResult> DeleteAsync(
        int organizationId,
        Guid organizationalStructureId,
        Guid requestedById
    );
    Task<IHttpResult<IEnumerable<OrganizationalStructureDto>>> SearchAsync(
        int organizationId,
        string searchTerm
    );
}
