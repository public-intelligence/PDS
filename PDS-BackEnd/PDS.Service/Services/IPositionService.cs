using Microsoft.AspNetCore.Http;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.DTOs.Position;
using PDS.Domain.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace PDS.Service.Services;

public interface IPositionService
{
    public Task<IHttpResult<PagedResponseDto<PositionDto>>> ListAsync(
        int organizationId,
        Guid requestedById,
        PaginationFilterDto paginationFilter,
        HttpContext httpContext
    );
    public Task<IHttpResult<PagedResponseDto<PositionDto>>> SearchAsync(
        int organizationId,
        string searchTerm,
        PaginationFilterDto paginationFilter,
        HttpContext httpContext
    );
    public Task<IHttpResult<PagedResponseDto<PositionDto>>> SearchAsync(
        int organizationId,
        Guid organizationalStructureId,
        string searchTerm,
        PaginationFilterDto paginationFilter,
        HttpContext httpContext,
        bool includeDescendants = false
    );
    public Task<IHttpResult<PositionDto>> CreateAsync(
        int organizationId,
        Guid requestedById,
        CreatePositionDto createPositionDto
    );
    public Task<IHttpResult<PositionDto>> GetAsync(
        int organizationId,
        Guid requestedById,
        Guid positionId
    );
    public Task<IHttpResult<PositionDto>> UpdateAsync(
        int organizationId,
        Guid positionId,
        Guid requestedById,
        UpdatePositionDto updatePositionDto
    );
    public Task<IHttpResult> DeleteAsync(int organizationId, Guid requestedById, Guid positionId);
    public Task<IHttpResult<bool>> AnyAsync(int organizationId, Guid positionId);
    public Task<IHttpResult<bool>> AnyAsync(int organizationId, Guid organizationalStructureId, Guid positionId);
    Task<IHttpResult<PositionDto?>> GetAsync(int organizationId, Guid positionId);
    Task<IHttpResult<IEnumerable<PositionSupervisorLevelDto>>> GetSupervisorChainAsync(
        int organizationId,
        IEnumerable<Guid> positionIds
    );
    Task<IHttpResult<IEnumerable<PositionSupervisorLevelDto>>> GetSubordinatePositionsAsync(
        int organizationId,
        Guid supervisorPositionId
    );
    Task<IHttpResult<IEnumerable<PositionDto>>> GetByIdsAsync(
        int organizationId,
        IEnumerable<Guid> positionIds
    );
    Task<IHttpResult<IEnumerable<Guid>>> GetPositionIdsByStructureAsync(
        int organizationId,
        Guid organizationalStructureId,
        bool includeDescendants
    );
    Task<IHttpResult<PositionDto?>> GetDirectManagerByPositionAsync(
        int organizationId,
        Guid positionId
    );
}
