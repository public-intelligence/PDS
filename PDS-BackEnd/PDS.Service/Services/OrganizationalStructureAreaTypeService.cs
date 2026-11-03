using AutoMapper;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.OrganizationalStructureAreatype;
using PDS.Domain.Models;
using PDS.Domain.Response;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace PDS.Service.Services;

public class OrganizationalStructureAreaTypeService : IOrganizationalStructureAreaTypeService
{
    private readonly IRepository<OrganizationalStructureAreaType> _organizationalStructureAreaTypeRepository;
    private readonly IMapper _mapper;

    public OrganizationalStructureAreaTypeService(
        IRepository<OrganizationalStructureAreaType> organizationalStructureAreaTypeRepository,
        IMapper mapper)
    {
        _organizationalStructureAreaTypeRepository = organizationalStructureAreaTypeRepository;
        _mapper = mapper;
    }

    public async Task<IHttpResult<IEnumerable<OrganizationalStructureAreaTypeDto>>> ListAsync()
    {
        var result = await _organizationalStructureAreaTypeRepository.GetAllAsync();
        if (!result.IsSuccess)
        {
            var title = "Error al obtener los tipo de area";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo obtener los tipo de area"] } };
            return HttpResult<IEnumerable<OrganizationalStructureAreaTypeDto>>.Failure(title, detail, HttpStatusCode.BadRequest);
        }
        var organizationalStructureAreaTypes = result.Value;
        if (organizationalStructureAreaTypes == null)
        {
            var title = "Error al obtener el tipo de area";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No existen registros"] } };
            return HttpResult<IEnumerable<OrganizationalStructureAreaTypeDto>>.Failure(title, detail, HttpStatusCode.BadRequest);
        }
        var ordered = organizationalStructureAreaTypes.OrderBy(e => e.Order);
        var organizationalStructureAreaTypeDto = _mapper.Map<IEnumerable<OrganizationalStructureAreaTypeDto>>(ordered);
        return HttpResult<IEnumerable<OrganizationalStructureAreaTypeDto>>.Success(organizationalStructureAreaTypeDto);
    }
    public async Task<IHttpResult<OrganizationalStructureAreaTypeDto>> GetAsync(
        Guid organizationalStructureAreaTypeId)
    {
        var result = await _organizationalStructureAreaTypeRepository.GetAsync(e => e.Id == organizationalStructureAreaTypeId);
        if (!result.IsSuccess)
        {
            var title = "Error al obtener el tipo de area";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo obtener el tipo de area"] } };
            return HttpResult<OrganizationalStructureAreaTypeDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var organizationalStructureAreaType = result.Value;
        if (organizationalStructureAreaType == null)
        {
            var title = "Error al obtener el tipo de area";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["El tipo de area no existe"] } };
            return HttpResult<OrganizationalStructureAreaTypeDto>.Failure(title, detail, HttpStatusCode.NotFound);
        }

        var organizationalStructureAreaTypeDto = _mapper.Map<OrganizationalStructureAreaTypeDto>(organizationalStructureAreaType);
        return HttpResult<OrganizationalStructureAreaTypeDto>.Success(organizationalStructureAreaTypeDto);
    }

    public async Task<IHttpResult<OrganizationalStructureAreaTypeDto>> CreateAsync(
        CreateOrganizationalStructureAreaTypeDto createOrganizationalStructureAreaTypeDto)
    {
        var details = new Dictionary<string, IEnumerable<string>>();
        var orgEstrAreaTypeExist = await _organizationalStructureAreaTypeRepository.GetAsync(
            e => e.Name.ToLower().Trim() == createOrganizationalStructureAreaTypeDto.Name.ToLower().Trim());

        if (!orgEstrAreaTypeExist.IsSuccess)
        {
            details.TryAdd("errors", ["No se pudo verificar si el nombre ya esta registrado"]);
        }

        var orgEstrAreaType = orgEstrAreaTypeExist.Value;
        if (orgEstrAreaType != null)
        {
            details.TryAdd("errors", ["El nombre ya esta registrado"]);
        }

        if (details.Count > 0)
        {
            var title = "No se pudo crear la organización";
            return HttpResult<OrganizationalStructureAreaTypeDto>.Failure(title, details, HttpStatusCode.BadRequest);
        }

        var orgEstrucAreaType = _mapper.Map<OrganizationalStructureAreaType>(createOrganizationalStructureAreaTypeDto);

        var result = await _organizationalStructureAreaTypeRepository.CreateAsync(orgEstrucAreaType);
        if (!result.IsSuccess)
        {
            var title = "Error al crear el tipo de area";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo crear el tipo de area"] } };
            return HttpResult<OrganizationalStructureAreaTypeDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var newOrgEstrucAreaType = result.Value;
        var orgEstrucAreaTypeDto = _mapper.Map<OrganizationalStructureAreaTypeDto>(newOrgEstrucAreaType);
        return HttpResult<OrganizationalStructureAreaTypeDto>.Success(orgEstrucAreaTypeDto);
    }
    public async Task<IHttpResult<OrganizationalStructureAreaTypeDto>> UpdateAsync(
        Guid organizationalStructureAreaTypeId,
        UpdateOrganizationalStructureAreaTypeDto updateOrganizationalStructureAreaTypeDto)
    {
        var details = new Dictionary<string, IEnumerable<string>>();
        var orgEstrAreaTypeExist = await _organizationalStructureAreaTypeRepository.GetAsync(
            e => e.Name.ToLower().Trim() == updateOrganizationalStructureAreaTypeDto.Name.ToLower().Trim());
        if (!orgEstrAreaTypeExist.IsSuccess)
        {
            details.TryAdd("errors", ["No se pudo verificar si el nombre ya esta registrado"]);
        }

        var orgEstrAreaType = orgEstrAreaTypeExist.Value;
        if (orgEstrAreaType != null)
        {
            details.TryAdd("name", ["El nombre ya esta registrado"]);
        }

        if (details.Count > 0)
        {
            var title = "No se pudo actualizar el tipo de area";
            return HttpResult<OrganizationalStructureAreaTypeDto>.Failure(title, details, HttpStatusCode.BadRequest);
        }

        var result = await _organizationalStructureAreaTypeRepository.GetAsync(e => e.Id == organizationalStructureAreaTypeId);
        if (!result.IsSuccess)
        {
            var title = "Error al actualizar el tipo de area";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo obtener el tipo de area"] } };
            return HttpResult<OrganizationalStructureAreaTypeDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var organizationalStructureAreaType = result.Value;
        if (organizationalStructureAreaType == null)
        {
            var title = "Error al actualizar el tipo de area";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["El tipo de area no existe"] } };
            return HttpResult<OrganizationalStructureAreaTypeDto>.Failure(title, detail, HttpStatusCode.NotFound);
        }

        organizationalStructureAreaType.Name = updateOrganizationalStructureAreaTypeDto.Name;

        var updateResult = await _organizationalStructureAreaTypeRepository.UpdateAsync(organizationalStructureAreaType);
        if (!updateResult.IsSuccess)
        {
            var title = "Error al actualizar el tipo de area";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo actualizar el tipo de area"] } };
            return HttpResult<OrganizationalStructureAreaTypeDto>.Failure(title, detail, HttpStatusCode.NotFound);
        }

        var updateAreaType = updateResult.Value;
        var updateAreaTypeDto = _mapper.Map<OrganizationalStructureAreaTypeDto>(updateAreaType);
        return HttpResult<OrganizationalStructureAreaTypeDto>.Success(updateAreaTypeDto);
    }

    public async Task<IHttpResult> DeleteAsync(
        Guid organizationalStructureAreaTypeId)
    {
        var deleteResult = await _organizationalStructureAreaTypeRepository.DeleteAsync(o => o.Id == organizationalStructureAreaTypeId);
        if (!deleteResult.IsSuccess)
        {
            var title = "Error al eliminar el tipo de area";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo eliminar el tipo de area"] } };
            return HttpResult<OrganizationalStructureAreaTypeDto>.Failure(title, detail, HttpStatusCode.BadRequest);
        }
        return HttpResult.Success();
    }
}