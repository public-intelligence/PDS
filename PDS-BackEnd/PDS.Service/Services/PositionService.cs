using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.DTOs.Position;
using PDS.Domain.Models;
using PDS.Domain.Response;
using System.Net;

namespace PDS.Service.Services;

public class PositionService : IPositionService
{
    private readonly ILogger<PositionService> _logger;
    private readonly IMapper _mapper;
    private readonly IRepository<Position> _positionRepository;
    private readonly IRepository<OrganizationalStructure> _organizationalStructureRepository;
    private readonly IPaginationService _paginationService;
    private readonly IGobidRoleClient _gobidRoleClient;

    public PositionService(
        ILogger<PositionService> logger,
        IMapper mapper,
        IRepository<Position> positionRepository,
        IRepository<OrganizationalStructure> organizationalStructureRepository,
        IPaginationService paginationService,
        IGobidRoleClient gobidRoleClient
    )
    {
        _logger = logger;
        _mapper = mapper;
        _positionRepository = positionRepository;
        _organizationalStructureRepository = organizationalStructureRepository;
        _paginationService = paginationService;
        _gobidRoleClient = gobidRoleClient;
    }

    public async Task<IHttpResult<bool>> AnyAsync(int organizationId, Guid positionId)
    {
        var anyResult = await _positionRepository.AnyAsync(p =>
            p.OrganizationalStructure.OrganizationId == organizationId &&
            p.Id == positionId
        );
        if (!anyResult.IsSuccess)
        {
            var title = "Error al verificar posición";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo verificar la posición"] } };
            return HttpResult<bool>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var anyPosition = anyResult.Value;
        return HttpResult<bool>.Success(anyPosition);
    }

    public async Task<IHttpResult<bool>> AnyAsync(int organizationId, Guid organizationalStructureId, Guid positionId)
    {
        var anyResult = await _positionRepository.AnyAsync(p =>
            p.OrganizationalStructure.OrganizationId == organizationId &&
            p.OrganizationalStructureId == organizationalStructureId &&
            p.Id == positionId
        );
        if (!anyResult.IsSuccess)
        {
            var title = "Error al verificar posición";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo verificar la posición"] } };
            return HttpResult<bool>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var anyPosition = anyResult.Value;
        return HttpResult<bool>.Success(anyPosition);
    }

    public async Task<IHttpResult<PositionDto>> CreateAsync(
        int organizationId,
        Guid requestedById,
        CreatePositionDto createPositionDto
    )
    {
        //var isUserActiveInOrgResult =
        //    await _organizationService.CheckIfUserIsActiveInOrganizationAsync(
        //        organizationId,
        //        requestedById
        //    );
        //if (!isUserActiveInOrgResult.IsSuccess)
        //{
        //    var title = "Error al crear posición";
        //    var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo verificar si el usuario esta activo en la organización"] } };
        //    return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        //}

        //var isUserActiveInOrg = isUserActiveInOrgResult.Value;
        //if (!isUserActiveInOrg)
        //{
        //    var title = "Error al crear posición";
        //    var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["El usuario no esta activo en la organización"] } };
        //    return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        //}
        var organizationalStructureId = createPositionDto.OrganizationalStructureId;

        var existingPositionResult = await _positionRepository.GetAsync(p =>
            p.OrganizationalStructure.OrganizationId == organizationId && p.OrganizationalStructureId == organizationalStructureId && p.Name.ToLower().Trim() == createPositionDto.Name.ToLower().Trim()
        );

        if (!existingPositionResult.IsSuccess)
        {
            var title = "Error al crear posición";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo verificar si la posición ya existe"] } };
            return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var existingPosition = existingPositionResult.Value;
        if (existingPosition != null)
        {
            var title = "Error al crear posición";
            var detail = new Dictionary<string, IEnumerable<string>> { { "name", ["La posición ya existe"] } };
            return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var existingManagerPosition = await _positionRepository.GetAsync(p => p.OrganizationalStructure.OrganizationId == organizationId
        && p.OrganizationalStructureId == organizationalStructureId && p.IsManager);

        if (!existingManagerPosition.IsSuccess)
        {
            var title = "Error al crear posición";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo verificar si la posición de gerente ya existe"] } };
            return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }
        var managerPosition = existingManagerPosition.Value;
        if (managerPosition != null && createPositionDto.IsManager)
        {
            var title = "Error al crear posición";
            var detail = new Dictionary<string, IEnumerable<string>> { { "isManager", ["Ya existe una posición de gerente en la estructura organizacional"] } };
            return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }
        var position = _mapper.Map<Position>(createPositionDto);

        var newPositionResult = await _positionRepository.CreateAsync(position);
        if (!newPositionResult.IsSuccess)
        {
            var title = "Error al crear posición";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo crear la posición"] } };
            return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var newPosition = newPositionResult.Value;

        var gobidRoleResult = await _gobidRoleClient.UpsertPositionRoleAsync(newPosition.Id, newPosition.Name);
        if (!gobidRoleResult.IsSuccess)
        {
            _logger.LogWarning(
                "La posición {PositionId} se creó, pero no se pudo crear su rol en GOBID.",
                newPosition.Id
            );
        }

        var newPositionDto = _mapper.Map<PositionDto>(newPosition);

        return HttpResult<PositionDto>.Success(newPositionDto);
    }

    public async Task<IHttpResult> DeleteAsync(int organizationId, Guid requestedById, Guid positionId)
    {
        //var isUserActiveInOrgResult =
        //    await _organizationService.CheckIfUserIsActiveInOrganizationAsync(
        //        organizationId,
        //        requestedById
        //    );
        //if (!isUserActiveInOrgResult.IsSuccess)
        //{
        //    var title = "Error al eliminar posición";
        //    var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo verificar si el usuario esta activo en la organización"] } };
        //    return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        //}

        //var isUserActiveInOrg = isUserActiveInOrgResult.Value;
        //if (!isUserActiveInOrg)
        //{
        //    var title = "Error al eliminar posición";
        //    var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["El usuario no esta activo en la organización"] } };
        //    return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        //}

        var deletedResult = await _positionRepository.DeleteAsync(p =>
            p.OrganizationalStructure.OrganizationId == organizationId && p.Id == positionId
        );
        if (!deletedResult.IsSuccess)
        {
            var title = "Error al eliminar posición";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo eliminar la posición"] } };
            return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var gobidRoleResult = await _gobidRoleClient.DeletePositionRoleAsync(positionId);
        if (!gobidRoleResult.IsSuccess)
        {
            _logger.LogWarning(
                "La posición {PositionId} se eliminó, pero no se pudo eliminar su rol en GOBID.",
                positionId
            );
        }

        return HttpResult<PositionDto>.Success();
    }

    public async Task<IHttpResult<PositionDto>> GetAsync(
        int organizationId,
        Guid requestedById,
        Guid positionId
    )
    {
        //var isUserActiveInOrgResult =
        //    await _organizationService.CheckIfUserIsActiveInOrganizationAsync(
        //        organizationId,
        //        requestedById
        //    );
        //if (!isUserActiveInOrgResult.IsSuccess)
        //{
        //    var title = "Error al obtener posición";
        //    var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo verificar si el usuario esta activo en la organización"] } };
        //    return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        //}

        //var isUserActiveInOrg = isUserActiveInOrgResult.Value;
        //if (!isUserActiveInOrg)
        //{
        //    var title = "Error al obtener posición";
        //    var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["El usuario no esta activo en la organización"] } };
        //    return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        //}

        var positionResult = await _positionRepository.GetAsync(
            p => p.OrganizationalStructure.OrganizationId == organizationId && p.Id == positionId,
            query => query
            .Include(p => p.OccupationalGroup)
            .Include(p => p.OrganizationalStructure)
        );
        if (!positionResult.IsSuccess)
        {
            var title = "Error al obtener posición";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo obtener la posición"] } };
            return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var position = positionResult.Value;
        var positionDto = _mapper.Map<PositionDto>(position);

        return HttpResult<PositionDto>.Success(positionDto);
    }

    public async Task<IHttpResult<PagedResponseDto<PositionDto>>> ListAsync(
        int organizationId,
        Guid requestedById,
        PaginationFilterDto paginationFilter,
        HttpContext httpContext
    )
    {
        var pageNumber = paginationFilter.PageNumber;
        var pageSize = paginationFilter.PageSize;

        //var isUserActiveInOrgResult = await _organizationService.CheckIfUserIsActiveInOrganizationAsync(organizationId, requestedById);
        //if (!isUserActiveInOrgResult.IsSuccess)
        //{
        //    var title = "Error al obtener posiciones";
        //    var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo verificar si el usuario esta activo en la organización"] } };
        //    return HttpResult<PagedResponseDto<PositionDto>>.Failure(title, detail, HttpStatusCode.BadRequest);
        //}

        //var isUserActiveInOrg = isUserActiveInOrgResult.Value;
        //if (!isUserActiveInOrg)
        //{
        //    var title = "Error al obtener posiciones";
        //    var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["El usuario no esta activo en la organización"] } };
        //    return HttpResult<PagedResponseDto<PositionDto>>.Failure(title, detail, HttpStatusCode.BadRequest);
        //}

        var totalRecordsResult = await _positionRepository.CountAsync(p => p.OrganizationalStructure.OrganizationId == organizationId);

        var totalRecords = totalRecordsResult.Value;

        var positionsResult = await _positionRepository.FilterAsync(
            p => p.OrganizationalStructure.OrganizationId == organizationId,
            query =>
                query
                    .Include(p => p.OccupationalGroup)
                    .Include(p => p.OrganizationalStructure)
                    .OrderByDescending(p => p.OccupationalGroup.Name)
                    .ThenBy(p => p.OrganizationalStructure.Code)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
        );

        if (!positionsResult.IsSuccess)
        {
            var title = "Error al obtener posiciones";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo obtener las posiciones"] } };
            return HttpResult<PagedResponseDto<PositionDto>>.Failure(
                title, detail, HttpStatusCode.BadRequest
            );
        }

        var positions = positionsResult.Value;
        var positionDtos = await AppendStructureToDuplicateNamesAsync(organizationId, _mapper.Map<IEnumerable<PositionDto>>(positions));
        var totalPages = totalRecords > 0 ? (int)Math.Ceiling((double)totalRecords / pageSize) : 1;

        var pagedResponse = _paginationService.BuildPaginationUrl(httpContext, pageNumber, pageSize, totalRecords, positionDtos);

        return HttpResult<PagedResponseDto<PositionDto>>.Success(pagedResponse);
    }

    public async Task<IHttpResult<PagedResponseDto<PositionDto>>> SearchAsync(
        int organizationId,
        string searchTerm,
        PaginationFilterDto paginationFilter,
        HttpContext httpContext
    )
    {
        var pageNumber = paginationFilter.PageNumber < 1 ? 1 : paginationFilter.PageNumber;
        var pageSize = paginationFilter.PageSize;
        var totalRecordsResult = await _positionRepository.CountAsync(p =>
            p.OrganizationalStructure.OrganizationId == organizationId && (p.Name.Contains(searchTerm) || (p.Name + " - " + p.OrganizationalStructure.Name).Contains(searchTerm) || p.OccupationalGroup.Name.Contains(searchTerm))
        );

        var totalRecords = totalRecordsResult.Value;

        var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
        var searchResult = await _positionRepository.FilterAsync(
            p =>
                p.OrganizationalStructure.OrganizationId == organizationId &&
                (p.Name.Contains(searchTerm)
                || (p.Name + " - " + p.OrganizationalStructure.Name).Contains(searchTerm)
                || p.OccupationalGroup.Name.Contains(searchTerm)),
            query =>
                query
                   .Include(p => p.OccupationalGroup)
                   .Include(p => p.OrganizationalStructure)
                   .OrderBy(p => p.Name.StartsWith(searchTerm))
                   .ThenBy(p => p.OccupationalGroup.Name.StartsWith(searchTerm))
                   .Skip((pageNumber - 1) * pageSize)
                   .Take(pageSize)
        );

        var position = searchResult.Value;
        var positionDtos = await AppendStructureToDuplicateNamesAsync(organizationId, _mapper.Map<IEnumerable<PositionDto>>(position));

        var pagedResponse = _paginationService.BuildPaginationUrl(httpContext, pageNumber, pageSize, totalRecords, positionDtos);

        return HttpResult<PagedResponseDto<PositionDto>>.Success(pagedResponse);
    }

    public async Task<IHttpResult<PagedResponseDto<PositionDto>>> SearchAsync(
        int organizationId,
        Guid organizationalStructureId,
        string searchTerm,
        PaginationFilterDto paginationFilter,
        HttpContext httpContext,
        bool includeDescendants = false
    )
    {
        var pageNumber = paginationFilter.PageNumber < 1 ? 1 : paginationFilter.PageNumber;
        var pageSize = paginationFilter.PageSize;

        var structureIds = await GetStructureScopeAsync(organizationalStructureId, includeDescendants);

        var totalRecordsResult = await _positionRepository.CountAsync(p =>
            p.OrganizationalStructure.OrganizationId == organizationId && structureIds.Contains(p.OrganizationalStructureId) &&
            (p.Name.Contains(searchTerm) || (p.Name + " - " + p.OrganizationalStructure.Name).Contains(searchTerm) || p.OccupationalGroup.Name.Contains(searchTerm))
        );
        var totalRecords = totalRecordsResult.Value;

        var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
        var searchResult = await _positionRepository.FilterAsync(
            p =>
                p.OrganizationalStructure.OrganizationId == organizationId &&
                structureIds.Contains(p.OrganizationalStructureId) &&
                (p.Name.Contains(searchTerm)
                || (p.Name + " - " + p.OrganizationalStructure.Name).Contains(searchTerm)
                || p.OccupationalGroup.Name.Contains(searchTerm)),
            query =>
                query
                   .Include(p => p.OccupationalGroup)
                   .Include(p => p.OrganizationalStructure)
                   .OrderBy(p => p.Name.StartsWith(searchTerm))
                   .ThenBy(p => p.OccupationalGroup.Name.StartsWith(searchTerm))
                   .Skip((pageNumber - 1) * pageSize)
                   .Take(pageSize)
        );
        var position = searchResult.Value;
        var positionDtos = await AppendStructureToDuplicateNamesAsync(organizationId, _mapper.Map<IEnumerable<PositionDto>>(position));
        var pagedResponse = _paginationService.BuildPaginationUrl(httpContext, pageNumber, pageSize, totalRecords, positionDtos);
        return HttpResult<PagedResponseDto<PositionDto>>.Success(pagedResponse);
    }

    
    public async Task<IHttpResult<IEnumerable<Guid>>> GetPositionIdsByStructureAsync(
        int organizationId,
        Guid organizationalStructureId,
        bool includeDescendants
    )
    {
        var structureIds = await GetStructureScopeAsync(organizationalStructureId, includeDescendants);

        var positionsResult = await _positionRepository.FilterAsync(
            p => p.OrganizationalStructure.OrganizationId == organizationId
                 && structureIds.Contains(p.OrganizationalStructureId)
        );

        if (!positionsResult.IsSuccess)
        {
            var title = "Error al obtener posiciones de la estructura organizacional";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo obtener las posiciones de la estructura organizacional"] } };
            return HttpResult<IEnumerable<Guid>>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var positionIds = positionsResult.Value.Select(p => p.Id);
        return HttpResult<IEnumerable<Guid>>.Success(positionIds);
    }

   
    private async Task<HashSet<Guid>> GetStructureScopeAsync(Guid rootStructureId, bool includeDescendants)
    {
        var structureIds = new HashSet<Guid> { rootStructureId };
        if (!includeDescendants)
            return structureIds;

        var frontier = new List<Guid> { rootStructureId };
        while (frontier.Count > 0)
        {
            var childrenResult = await _organizationalStructureRepository.FilterAsync(
                s => s.ParentId.HasValue && frontier.Contains(s.ParentId.Value)
            );
            if (!childrenResult.IsSuccess)
                break;

            var newChildIds = childrenResult.Value
                .Select(s => s.Id)
                .Where(structureIds.Add)
                .ToList();

            frontier = newChildIds;
        }

        return structureIds;
    }

    public async Task<IHttpResult<PositionDto>> UpdateAsync(
        int organizationId,
        Guid positionId,
        Guid requestedById,
        UpdatePositionDto updatePositionDto
    )
    {
        //var isUserActiveInOrgResult =
        //    await _organizationService.CheckIfUserIsActiveInOrganizationAsync(
        //        organizationId,
        //        requestedById
        //    );
        //if (!isUserActiveInOrgResult.IsSuccess)
        //{
        //    var title = "Error al actualizar posición";
        //    var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo verificar si el usuario esta activo en la organización"] } };
        //    return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        //}
        //var isUserActiveInOrg = isUserActiveInOrgResult.Value;
        //if (!isUserActiveInOrg)
        //{
        //    var title = "Error al actualizar posición";
        //    var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["El usuario no esta activo en la organización"] } };
        //    return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        //}

        var positionResult = await _positionRepository.GetAsync(
            p => p.OrganizationalStructure.OrganizationId == organizationId && p.Id == positionId,
            query => query
            .Include(p => p.OccupationalGroup)
            .Include(p => p.OrganizationalStructure)
        );
        if (!positionResult.IsSuccess)
        {
            var title = "Error al actualizar posición";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo obtener la posición"] } };
            return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }
        var organizationalStructureId = updatePositionDto.OrganizationalStructureId;

        var currentPosition = positionResult.Value;
        if (currentPosition == null)
        {
            var title = "Error al actualizar posición";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["La posición no existe"] } };
            return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var positionNameExistsResult = await _positionRepository.GetAsync(p =>
            p.OrganizationalStructure.OrganizationId == organizationId && p.OrganizationalStructureId == organizationalStructureId && p.Name.ToLower().Trim() == updatePositionDto.Name.ToLower().Trim() && p.Id != positionId
        );
        if (!positionNameExistsResult.IsSuccess)
        {
            var title = "Error al actualizar posición";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo verificar si el nombre de la posición ya existe"] } };
            return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var positionNameExists = positionNameExistsResult.Value;
        if (positionNameExists != null)
        {
            var title = "Error al actualizar posición";
            var detail = new Dictionary<string, IEnumerable<string>> { { "name", ["El nombre de la posición ya existe"] } };
            return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }


        var existingManagerPosition = await _positionRepository.GetAsync(p => p.OrganizationalStructure.OrganizationId == organizationId
        && p.OrganizationalStructureId == organizationalStructureId && p.IsManager && p.Id != positionId);

        if (!existingManagerPosition.IsSuccess)
        {
            var title = "Error al crear posición";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo verificar si la posición de gerente ya existe"] } };
            return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }
        var managerPosition = existingManagerPosition.Value;
        if (managerPosition != null && updatePositionDto.IsManager)
        {
            var title = "Error al crear posición";
            var detail = new Dictionary<string, IEnumerable<string>> { { "isManager", ["Ya existe una posición de gerente en la estructura organizacional"] } };
            return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var position = _mapper.Map(updatePositionDto, currentPosition);

        var updatePositionResult = await _positionRepository.UpdateAsync(position);
        if (!updatePositionResult.IsSuccess)
        {
            var title = "Error al actualizar posición";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo actualizar la posición"] } };
            return HttpResult<PositionDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var updatedPosition = updatePositionResult.Value;
        return HttpResult<PositionDto>.Success(_mapper.Map<PositionDto>(updatedPosition));
    }

    public async Task<IHttpResult<PositionDto?>> GetAsync(int organizationId, Guid positionId)
    {
        var getPosition = await _positionRepository.GetAsync(
            p => p.OrganizationalStructure.OrganizationId == organizationId &&
            p.Id == positionId,
            query => query
            .Include(p => p.OccupationalGroup)
            .Include(p => p.OrganizationalStructure)
        );
        if (!getPosition.IsSuccess)
        {
            var title = "Error al actualizar posición";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo actualizar la posición"] } };
            return HttpResult<PositionDto?>.Failure(title, detail, HttpStatusCode.BadRequest);
        }
        var position = getPosition.Value;
        var positionDto = _mapper.Map<PositionDto>(position);
        return HttpResult<PositionDto?>.Success(positionDto);
    }

    /// <summary>
    /// Concatena el nombre de la estructura organizacional al nombre de las posiciones
    /// cuyo nombre se repite dentro de la organización.
    /// </summary>
    private async Task<List<PositionDto>> AppendStructureToDuplicateNamesAsync(
        int organizationId,
        IEnumerable<PositionDto> positionDtos
    )
    {
        var dtoList = positionDtos.ToList();
        if (dtoList.Count == 0)
            return dtoList;

        var names = dtoList.Select(p => p.Name).Distinct().ToList();
        var sameNameResult = await _positionRepository.FilterAsync(p =>
            p.OrganizationalStructure.OrganizationId == organizationId && names.Contains(p.Name)
        );
        if (!sameNameResult.IsSuccess)
            return dtoList;

        var duplicatedNames = sameNameResult.Value
            .GroupBy(p => p.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return dtoList
            .Select(p => duplicatedNames.Contains(p.Name.Trim()) && p.OrganizationalStructure != null
                ? p with { Name = $"{p.Name} - {p.OrganizationalStructure.Name}" }
                : p)
            .ToList();
    }

    public async Task<IHttpResult<IEnumerable<PositionSupervisorLevelDto>>> GetSupervisorChainAsync(
        int organizationId,
        IEnumerable<Guid> positionIds
    )
    {
        var positionIdList = positionIds.ToList();
        if (positionIdList.Count == 0)
            return HttpResult<IEnumerable<PositionSupervisorLevelDto>>.Success([]);

        var positionsResult = await _positionRepository.FilterAsync(
            p => p.OrganizationalStructure.OrganizationId == organizationId
                 && positionIdList.Contains(p.Id)
        );
        if (!positionsResult.IsSuccess)
        {
            var title = "Error al obtener posiciones";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo obtener las posiciones indicadas"] } };
            return HttpResult<IEnumerable<PositionSupervisorLevelDto>>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var currentStructureIds = positionsResult.Value
            .Select(p => p.OrganizationalStructureId)
            .Distinct()
            .ToHashSet();

        var visitedIds = new HashSet<Guid>(currentStructureIds);
        int level = 0;

        while (currentStructureIds.Count > 0)
        {
            level++;

            var supervisorPositionsResult = await _positionRepository.FilterAsync(
                p => currentStructureIds.Contains(p.OrganizationalStructureId)
                     && p.IsManager
                     && !positionIdList.Contains(p.Id),
                q => q.Include(p => p.OrganizationalStructure)
            );
            if (!supervisorPositionsResult.IsSuccess)
            {
                var title = "Error al obtener posiciones supervisoras";
                var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo obtener las posiciones supervisoras"] } };
                return HttpResult<IEnumerable<PositionSupervisorLevelDto>>.Failure(title, detail, HttpStatusCode.BadRequest);
            }

            if (supervisorPositionsResult.Value.Any())
            {
                return HttpResult<IEnumerable<PositionSupervisorLevelDto>>.Success(
                [
                    new PositionSupervisorLevelDto
                    {
                        Level = level,
                        Positions = [.._mapper.Map<IEnumerable<PositionDto>>(supervisorPositionsResult.Value)]
                    }
                ]);
            }

            var structuresResult = await _organizationalStructureRepository.FilterAsync(
                s => currentStructureIds.Contains(s.Id) && s.ParentId.HasValue
            );
            if (!structuresResult.IsSuccess)
            {
                var title = "Error al obtener jerarquía";
                var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo obtener la jerarquía organizacional"] } };
                return HttpResult<IEnumerable<PositionSupervisorLevelDto>>.Failure(title, detail, HttpStatusCode.BadRequest);
            }

            var parentIds = structuresResult.Value
                .Select(s => s.ParentId!.Value)
                .Distinct()
                .Where(id => !visitedIds.Contains(id))
                .ToHashSet();

            if (parentIds.Count == 0) break;

            visitedIds.UnionWith(parentIds);
            currentStructureIds = parentIds;
        }

        var noSupTitle = "Sin supervisor";
        var noSupDetail = new Dictionary<string, IEnumerable<string>> { { "errors", ["La posición no tiene supervisor asignado"] } };
        return HttpResult<IEnumerable<PositionSupervisorLevelDto>>.Failure(noSupTitle, noSupDetail, HttpStatusCode.NotFound);
    }

    public async Task<IHttpResult<IEnumerable<PositionSupervisorLevelDto>>> GetSubordinatePositionsAsync(
        int organizationId,
        Guid supervisorPositionId
    )
    {
        var isSupervisor = await _positionRepository.AnyAsync(p => p.Id == supervisorPositionId && p.IsManager);
        if(!isSupervisor.IsSuccess)
        {
            var title = "Error al verificar posición de supervisor";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo verificar si la posición es de supervisor"] } };
            return HttpResult<IEnumerable<PositionSupervisorLevelDto>>.Failure(title, detail, HttpStatusCode.BadRequest);
        }
        if(!isSupervisor.Value)
        {
            var title = "La posición no es de supervisor";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["La posición especificada no es de supervisor"] } };
            return HttpResult<IEnumerable<PositionSupervisorLevelDto>>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var supervisorResult = await _positionRepository.GetAsync(
                p => p.OrganizationalStructure.OrganizationId == organizationId && p.Id == supervisorPositionId,
                q => q.Include(p => p.OrganizationalStructure)
            );
        if (!supervisorResult.IsSuccess)
        {
            var title = "Error al obtener la posición del supervisor";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo obtener la posición del supervisor especificada"] } };
            return HttpResult<IEnumerable<PositionSupervisorLevelDto>>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var supervisor = supervisorResult.Value;
        if (supervisor == null)
        {
            var title = "Supervisor no encontrado";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["La posición del supervisor no existe"] } };
            return HttpResult<IEnumerable<PositionSupervisorLevelDto>>.Failure(title, detail, HttpStatusCode.NotFound);
        }

        var result = new List<PositionSupervisorLevelDto>();
        var structuresAtDistance = new List<Guid> { supervisor.OrganizationalStructureId };
        var visitedStructures = new HashSet<Guid> { supervisor.OrganizationalStructureId };
        int distance = 0;

        var positionsByLevel = new Dictionary<int, List<Position>>();

        while (structuresAtDistance.Count > 0)
        {
            var positionsResult = await _positionRepository.FilterAsync(
                p => structuresAtDistance.Contains(p.OrganizationalStructureId),
                q => q.Include(p => p.OrganizationalStructure)
            );
            if (!positionsResult.IsSuccess)
            {
                var title = "Error al obtener posiciones subordinadas";
                var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo obtener las posiciones subordinadas"] } };
                return HttpResult<IEnumerable<PositionSupervisorLevelDto>>.Failure(title, detail, HttpStatusCode.BadRequest);
            }

            foreach (var pos in positionsResult.Value)
            {
                if (pos.Id == supervisorPositionId)
                    continue;

                int level = pos.IsManager ? distance : distance + 1;

                if (level > 0)
                {
                    if (!positionsByLevel.TryGetValue(level, out var list))
                    {
                        list = [];
                        positionsByLevel[level] = list;
                    }
                    list.Add(pos);
                }
            }

            var childStructuresResult = await _organizationalStructureRepository.FilterAsync(
                s => s.ParentId.HasValue && structuresAtDistance.Contains(s.ParentId.Value)
            );
            if (!childStructuresResult.IsSuccess)
            {
                var title = "Error al obtener jerarquía inferior";
                var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo obtener la jerarquía organizacional inferior"] } };
                return HttpResult<IEnumerable<PositionSupervisorLevelDto>>.Failure(title, detail, HttpStatusCode.BadRequest);
            }

            var childIds = childStructuresResult.Value
                .Select(s => s.Id)
                .Where(id => !visitedStructures.Contains(id))
                .ToList();

            visitedStructures.UnionWith(childIds);
            structuresAtDistance = childIds;
            distance++;
        }

        var sortedResult = positionsByLevel
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new PositionSupervisorLevelDto
            {
                Level = kvp.Key,
                Positions = _mapper.Map<IEnumerable<PositionDto>>(kvp.Value).ToList()
            })
            .ToList();

        return HttpResult<IEnumerable<PositionSupervisorLevelDto>>.Success(sortedResult);
    }

    public async Task<IHttpResult<IEnumerable<PositionDto>>> GetByIdsAsync(
        int organizationId,
        IEnumerable<Guid> positionIds
    )
    {
        var positionIdList = positionIds.ToList();
        if (positionIdList.Count == 0)
            return HttpResult<IEnumerable<PositionDto>>.Success([]);

        var positionsResult = await _positionRepository.FilterAsync(
            p => p.OrganizationalStructure.OrganizationId == organizationId
                 && positionIdList.Contains(p.Id),
            query => query
                .Include(p => p.OccupationalGroup)
                .Include(p => p.OrganizationalStructure)
        );

        if (!positionsResult.IsSuccess)
        {
            var title = "Error al obtener posiciones por IDs";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo obtener la información de las posiciones indicadas"] } };
            return HttpResult<IEnumerable<PositionDto>>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var positions = positionsResult.Value;
        var positionDtos = await AppendStructureToDuplicateNamesAsync(organizationId, _mapper.Map<IEnumerable<PositionDto>>(positions));
        return HttpResult<IEnumerable<PositionDto>>.Success(positionDtos);
    }

    public async Task<IHttpResult<PositionDto?>> GetDirectManagerByPositionAsync(
        int organizationId,
        Guid positionId
    )
    {
        // Get the position with its organizational structure (to know the parent)
        var positionResult = await _positionRepository.GetAsync(
            p => p.OrganizationalStructure.OrganizationId == organizationId && p.Id == positionId,
            q => q.Include(p => p.OrganizationalStructure)
        );
        if (!positionResult.IsSuccess)
        {
            var title = "Error al obtener la posición";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo obtener la posición especificada"] } };
            return HttpResult<PositionDto?>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var position = positionResult.Value;
        if (position == null)
        {
            var title = "Posición no encontrada";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["La posición especificada no existe"] } };
            return HttpResult<PositionDto?>.Failure(title, detail, HttpStatusCode.NotFound);
        }

        Guid searchStructureId;
        if (position.IsManager)
        {
            // Manager looks up to parent structure to avoid returning itself
            if (position.OrganizationalStructure?.ParentId is not { } parentId)
                return HttpResult<PositionDto?>.Success(null);
            searchStructureId = parentId;
        }
        else
        {
            searchStructureId = position.OrganizationalStructureId;
        }

        var managerResult = await _positionRepository.GetAsync(
            p => p.OrganizationalStructureId == searchStructureId && p.IsManager,
            q => q.Include(p => p.OccupationalGroup).Include(p => p.OrganizationalStructure)
        );
        if (!managerResult.IsSuccess)
        {
            var title = "Error al obtener el encargado directo";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo obtener el encargado directo de la posición"] } };
            return HttpResult<PositionDto?>.Failure(title, detail, HttpStatusCode.BadRequest);
        }
        if (managerResult == null)
        {
            var title = "No se encontro el encargado directo";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo obtener el encargado directo de la posición"] } };
            return HttpResult<PositionDto?>.Failure(title, detail, HttpStatusCode.NotFound);
        }

        var positions = managerResult.Value;
        var positionDtos = _mapper.Map<PositionDto>(positions);
        return HttpResult<PositionDto?>.Success(positionDtos);
    }
}
