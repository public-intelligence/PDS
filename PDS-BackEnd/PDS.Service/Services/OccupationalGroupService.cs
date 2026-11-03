using AutoMapper;
using Microsoft.Extensions.Logging;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.OccupationalGroups;
using PDS.Domain.Models;
using PDS.Domain.Response;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace PDS.Service.Services;

public class OccupationalGroupService : IOccupationalGroupService
{
    private readonly ILogger<OccupationalGroupService> _logger;
    private readonly IMapper _mapper;
    private readonly IRepository<OccupationalGroup> _occupationalGroupRepository;

    public OccupationalGroupService(
        ILogger<OccupationalGroupService> logger,
        IMapper mapper,
        IRepository<OccupationalGroup> occupationalGroup
    )
    {
        _logger = logger;
        _mapper = mapper;
        _occupationalGroupRepository = occupationalGroup;
    }

    public async Task<IHttpResult<OccupationalGroupDto>> CreateOccupationalGroupAsync(
        CreateOccupationalGroupDto createOccupationalGroupDto
    )
    {
        var existingGroupResult = await _occupationalGroupRepository.GetAsync(
            og => og.Name.ToLower().Trim() == createOccupationalGroupDto.Name.ToLower().Trim()
        );
        if (!existingGroupResult.IsSuccess)
        {
            var title = "Error al crear el grupo ocupacional";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo verificar la existencia del grupo ocupacional"] } };
            return HttpResult<OccupationalGroupDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var existingGroup = existingGroupResult.Value;
        if (existingGroup != null)
        {
            var title = "Error al crear el grupo ocupacional";
            var detail = new Dictionary<string, IEnumerable<string>> { { "name", ["Ya existe un grupo ocupacional con el mismo nombre"] } };
            return HttpResult<OccupationalGroupDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var occupationalGroup = _mapper.Map<OccupationalGroup>(createOccupationalGroupDto);

        var result = await _occupationalGroupRepository.CreateAsync(occupationalGroup);
        if (!result.IsSuccess)
        {
            var title = "Error al crear el grupo ocupacional";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo crear el grupo ocupacional"] } };
            return HttpResult<OccupationalGroupDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var newOccupationalGroup = result.Value;
        var occupationalGroupDto = _mapper.Map<OccupationalGroupDto>(newOccupationalGroup);

        return HttpResult<OccupationalGroupDto>.Success(occupationalGroupDto);
    }

    public async Task<IHttpResult> DeleteOccupationalGroupByIdAsync(Guid id)
    {
        var deleteResult = await _occupationalGroupRepository.DeleteAsync(o => o.Id == id);
        if (!deleteResult.IsSuccess)
        {
            var title = "Error al eliminar el grupo ocupacional";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo eliminar el grupo ocupacional"] } };
            return HttpResult.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        return HttpResult.Success();
    }

    public async Task<IHttpResult<IEnumerable<OccupationalGroupDto>>> GetAllOccupationalGroupsAsync()
    {
        var occupationalGroupsResult = await _occupationalGroupRepository.GetAllAsync();
        if (!occupationalGroupsResult.IsSuccess)
        {
            var title = "Error al obtener los grupos ocupacionales";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudieron obtener los grupos ocupacionales"] } };
            return HttpResult<IEnumerable<OccupationalGroupDto>>.Failure(title, detail, HttpStatusCode.BadRequest
            );
        }

        var occupationalGroups = occupationalGroupsResult.Value;
        var orderedOccupationalGroups = occupationalGroups.OrderBy(og => og.Name);
        var occupationalGroupDtos = _mapper.Map<IEnumerable<OccupationalGroupDto>>(
            orderedOccupationalGroups
        );

        return HttpResult<IEnumerable<OccupationalGroupDto>>.Success(occupationalGroupDtos);
    }

    public async Task<IHttpResult<OccupationalGroupDto>> GetOccupationalGroupAsync(Guid id)
    {
        var result = await _occupationalGroupRepository.GetAsync(e => e.Id == id);

        if (!result.IsSuccess)
        {
            var title = "Error al obtener el grupo ocupacional";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo obtener el grupo ocupacional"] } };
            return HttpResult<OccupationalGroupDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var occupationalGroup = result.Value;
        var occupationalGroupDto = _mapper.Map<OccupationalGroupDto>(occupationalGroup);

        return HttpResult<OccupationalGroupDto>.Success(occupationalGroupDto);
    }

    public async Task<IHttpResult<OccupationalGroupDto>> UpdateOccupationalGroupAsync(
        Guid occupationalGroupId,
        UpdateOccupationalGroupDto updateOccupationalGroupDto
    )
    {
        var details = new Dictionary<string, IEnumerable<string>>();

        var groupToUpdateResult = await _occupationalGroupRepository.GetAsync(p =>
            p.Id == occupationalGroupId
        );
        if (!groupToUpdateResult.IsSuccess)
        {
            details.TryAdd("errors", ["No se pudo obtener el grupo ocupacional"]);
        }

        var occupationalGroup = groupToUpdateResult.Value;
        if (occupationalGroup == null)
        {
            details.TryAdd("name", ["El nombre del grupo ocupacional no puede estar vacio"]);
        }

        var existingGroupResult = await _occupationalGroupRepository.GetAsync(
            og => og.Name.ToLower().Trim() == updateOccupationalGroupDto.Name.ToLower().Trim() &&
                  og.Id != occupationalGroupId
        );

        if (!existingGroupResult.IsSuccess)
        {
            details.TryAdd("errors", ["No se pudo verificar la existencia del grupo ocupacional"]);
        }
        var existingGroup = existingGroupResult.Value;
        if (existingGroup != null)
        {
            details.TryAdd("name", ["Ya existe un grupo ocupacional con el mismo nombre"]);
        }

        if (details.Count > 0)
        {
            var title = "No se pudo actualizar el grupo ocupacional";
            return HttpResult<OccupationalGroupDto>.Failure(title, details, HttpStatusCode.BadRequest);
        }

        occupationalGroup.Name = updateOccupationalGroupDto.Name;

        var updateResult = await _occupationalGroupRepository.UpdateAsync(occupationalGroup);
        if (!updateResult.IsSuccess)
        {
            var title = "Error al actualizar el grupo ocupacional";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo actualizar el grupo ocupacional"] } };
            return HttpResult<OccupationalGroupDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        if (details.Count > 0)
        {
            var title = "No se pudo actualizar el grupo ocupacional";
            return HttpResult<OccupationalGroupDto>.Failure(title, details, HttpStatusCode.BadRequest);
        }

        var occupationalGroupDto = _mapper.Map<OccupationalGroupDto>(updateResult.Value);
        return HttpResult<OccupationalGroupDto>.Success(occupationalGroupDto);
    }
}
