using AutoMapper;
using Microsoft.Extensions.Logging;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.CatalogoLocalidad;
using PDS.Domain.Models;
using PDS.Domain.Response;
using System.Net;

namespace PDS.Service.Services;

// ─────────────────────────────── Tipo de localidad ───────────────────────────────
public interface ITipoLocalidadService
{
    Task<IHttpResult<IEnumerable<TipoLocalidadDto>>> GetAllAsync();
    Task<IHttpResult<TipoLocalidadDto>> GetAsync(Guid id);
    Task<IHttpResult<TipoLocalidadDto>> CreateAsync(CreateTipoLocalidadDto dto);
    Task<IHttpResult<TipoLocalidadDto>> UpdateAsync(Guid id, UpdateTipoLocalidadDto dto);
    Task<IHttpResult> DeleteAsync(Guid id);
}

public class TipoLocalidadService(
    ILogger<TipoLocalidadService> logger,
    IMapper mapper,
    IRepository<TipoLocalidad> repository
) : ITipoLocalidadService
{
    private readonly ILogger<TipoLocalidadService> _logger = logger;
    private readonly IMapper _mapper = mapper;
    private readonly IRepository<TipoLocalidad> _repository = repository;

    public async Task<IHttpResult<IEnumerable<TipoLocalidadDto>>> GetAllAsync()
    {
        var result = await _repository.GetAllAsync();
        var dtos = _mapper.Map<IEnumerable<TipoLocalidadDto>>(result.Value.OrderBy(x => x.Nombre));
        return HttpResult<IEnumerable<TipoLocalidadDto>>.Success(dtos);
    }

    public async Task<IHttpResult<TipoLocalidadDto>> GetAsync(Guid id)
    {
        var result = await _repository.GetAsync(x => x.Id == id);
        if (result.Value is null)
            return CatalogErrors.NotFound<TipoLocalidadDto>("tipo de localidad");
        return HttpResult<TipoLocalidadDto>.Success(_mapper.Map<TipoLocalidadDto>(result.Value));
    }

    public async Task<IHttpResult<TipoLocalidadDto>> CreateAsync(CreateTipoLocalidadDto dto)
    {
        var exists = await _repository.GetAsync(x => x.Codigo.ToLower() == dto.Codigo.ToLower());
        if (exists.Value is not null)
            return CatalogErrors.DuplicateCode<TipoLocalidadDto>();
        var entity = _mapper.Map<TipoLocalidad>(dto);
        var created = await _repository.CreateAsync(entity);
        return HttpResult<TipoLocalidadDto>.Success(_mapper.Map<TipoLocalidadDto>(created.Value), HttpStatusCode.Created);
    }

    public async Task<IHttpResult<TipoLocalidadDto>> UpdateAsync(Guid id, UpdateTipoLocalidadDto dto)
    {
        var current = await _repository.GetAsync(x => x.Id == id);
        if (current.Value is null)
            return CatalogErrors.NotFound<TipoLocalidadDto>("tipo de localidad");
        var dup = await _repository.GetAsync(x => x.Codigo.ToLower() == dto.Codigo.ToLower() && x.Id != id);
        if (dup.Value is not null)
            return CatalogErrors.DuplicateCode<TipoLocalidadDto>();
        var entity = current.Value;
        entity.Codigo = dto.Codigo;
        entity.Nombre = dto.Nombre;
        entity.Descripcion = dto.Descripcion;
        entity.Color = dto.Color;
        var updated = await _repository.UpdateAsync(entity);
        return HttpResult<TipoLocalidadDto>.Success(_mapper.Map<TipoLocalidadDto>(updated.Value));
    }

    public async Task<IHttpResult> DeleteAsync(Guid id)
    {
        var result = await _repository.DeleteAsync(x => x.Id == id);
        if (!result.IsSuccess)
            return CatalogErrors.DeleteFailed("tipo de localidad");
        return HttpResult.Success();
    }
}

// ─────────────────────────── Naturaleza administrativa ───────────────────────────
public interface INaturalezaAdministrativaService
{
    Task<IHttpResult<IEnumerable<NaturalezaAdministrativaDto>>> GetAllAsync();
    Task<IHttpResult<NaturalezaAdministrativaDto>> GetAsync(Guid id);
    Task<IHttpResult<NaturalezaAdministrativaDto>> CreateAsync(CreateNaturalezaAdministrativaDto dto);
    Task<IHttpResult<NaturalezaAdministrativaDto>> UpdateAsync(Guid id, UpdateNaturalezaAdministrativaDto dto);
    Task<IHttpResult> DeleteAsync(Guid id);
}

public class NaturalezaAdministrativaService(
    ILogger<NaturalezaAdministrativaService> logger,
    IMapper mapper,
    IRepository<NaturalezaAdministrativa> repository
) : INaturalezaAdministrativaService
{
    private readonly ILogger<NaturalezaAdministrativaService> _logger = logger;
    private readonly IMapper _mapper = mapper;
    private readonly IRepository<NaturalezaAdministrativa> _repository = repository;

    public async Task<IHttpResult<IEnumerable<NaturalezaAdministrativaDto>>> GetAllAsync()
    {
        var result = await _repository.GetAllAsync();
        var dtos = _mapper.Map<IEnumerable<NaturalezaAdministrativaDto>>(result.Value.OrderBy(x => x.Nombre));
        return HttpResult<IEnumerable<NaturalezaAdministrativaDto>>.Success(dtos);
    }

    public async Task<IHttpResult<NaturalezaAdministrativaDto>> GetAsync(Guid id)
    {
        var result = await _repository.GetAsync(x => x.Id == id);
        if (result.Value is null)
            return CatalogErrors.NotFound<NaturalezaAdministrativaDto>("naturaleza administrativa");
        return HttpResult<NaturalezaAdministrativaDto>.Success(_mapper.Map<NaturalezaAdministrativaDto>(result.Value));
    }

    public async Task<IHttpResult<NaturalezaAdministrativaDto>> CreateAsync(CreateNaturalezaAdministrativaDto dto)
    {
        var exists = await _repository.GetAsync(x => x.Codigo.ToLower() == dto.Codigo.ToLower());
        if (exists.Value is not null)
            return CatalogErrors.DuplicateCode<NaturalezaAdministrativaDto>();
        var entity = _mapper.Map<NaturalezaAdministrativa>(dto);
        var created = await _repository.CreateAsync(entity);
        return HttpResult<NaturalezaAdministrativaDto>.Success(_mapper.Map<NaturalezaAdministrativaDto>(created.Value), HttpStatusCode.Created);
    }

    public async Task<IHttpResult<NaturalezaAdministrativaDto>> UpdateAsync(Guid id, UpdateNaturalezaAdministrativaDto dto)
    {
        var current = await _repository.GetAsync(x => x.Id == id);
        if (current.Value is null)
            return CatalogErrors.NotFound<NaturalezaAdministrativaDto>("naturaleza administrativa");
        var dup = await _repository.GetAsync(x => x.Codigo.ToLower() == dto.Codigo.ToLower() && x.Id != id);
        if (dup.Value is not null)
            return CatalogErrors.DuplicateCode<NaturalezaAdministrativaDto>();
        var entity = current.Value;
        entity.Codigo = dto.Codigo;
        entity.Nombre = dto.Nombre;
        entity.Descripcion = dto.Descripcion;
        var updated = await _repository.UpdateAsync(entity);
        return HttpResult<NaturalezaAdministrativaDto>.Success(_mapper.Map<NaturalezaAdministrativaDto>(updated.Value));
    }

    public async Task<IHttpResult> DeleteAsync(Guid id)
    {
        var result = await _repository.DeleteAsync(x => x.Id == id);
        if (!result.IsSuccess)
            return CatalogErrors.DeleteFailed("naturaleza administrativa");
        return HttpResult.Success();
    }
}

// ─────────────────────────────── Estado de localidad ───────────────────────────────
public interface IEstadoLocalidadService
{
    Task<IHttpResult<IEnumerable<EstadoLocalidadDto>>> GetAllAsync();
    Task<IHttpResult<EstadoLocalidadDto>> GetAsync(Guid id);
    Task<IHttpResult<EstadoLocalidadDto>> CreateAsync(CreateEstadoLocalidadDto dto);
    Task<IHttpResult<EstadoLocalidadDto>> UpdateAsync(Guid id, UpdateEstadoLocalidadDto dto);
    Task<IHttpResult> DeleteAsync(Guid id);
}

public class EstadoLocalidadService(
    ILogger<EstadoLocalidadService> logger,
    IMapper mapper,
    IRepository<EstadoLocalidad> repository
) : IEstadoLocalidadService
{
    private readonly ILogger<EstadoLocalidadService> _logger = logger;
    private readonly IMapper _mapper = mapper;
    private readonly IRepository<EstadoLocalidad> _repository = repository;

    public async Task<IHttpResult<IEnumerable<EstadoLocalidadDto>>> GetAllAsync()
    {
        var result = await _repository.GetAllAsync();
        var dtos = _mapper.Map<IEnumerable<EstadoLocalidadDto>>(result.Value.OrderBy(x => x.Nombre));
        return HttpResult<IEnumerable<EstadoLocalidadDto>>.Success(dtos);
    }

    public async Task<IHttpResult<EstadoLocalidadDto>> GetAsync(Guid id)
    {
        var result = await _repository.GetAsync(x => x.Id == id);
        if (result.Value is null)
            return CatalogErrors.NotFound<EstadoLocalidadDto>("estado de localidad");
        return HttpResult<EstadoLocalidadDto>.Success(_mapper.Map<EstadoLocalidadDto>(result.Value));
    }

    public async Task<IHttpResult<EstadoLocalidadDto>> CreateAsync(CreateEstadoLocalidadDto dto)
    {
        var exists = await _repository.GetAsync(x => x.Codigo.ToLower() == dto.Codigo.ToLower());
        if (exists.Value is not null)
            return CatalogErrors.DuplicateCode<EstadoLocalidadDto>();
        var entity = _mapper.Map<EstadoLocalidad>(dto);
        var created = await _repository.CreateAsync(entity);
        return HttpResult<EstadoLocalidadDto>.Success(_mapper.Map<EstadoLocalidadDto>(created.Value), HttpStatusCode.Created);
    }

    public async Task<IHttpResult<EstadoLocalidadDto>> UpdateAsync(Guid id, UpdateEstadoLocalidadDto dto)
    {
        var current = await _repository.GetAsync(x => x.Id == id);
        if (current.Value is null)
            return CatalogErrors.NotFound<EstadoLocalidadDto>("estado de localidad");
        var dup = await _repository.GetAsync(x => x.Codigo.ToLower() == dto.Codigo.ToLower() && x.Id != id);
        if (dup.Value is not null)
            return CatalogErrors.DuplicateCode<EstadoLocalidadDto>();
        var entity = current.Value;
        entity.Codigo = dto.Codigo;
        entity.Nombre = dto.Nombre;
        entity.Descripcion = dto.Descripcion;
        entity.Color = dto.Color;
        entity.EsTerminal = dto.EsTerminal;
        var updated = await _repository.UpdateAsync(entity);
        return HttpResult<EstadoLocalidadDto>.Success(_mapper.Map<EstadoLocalidadDto>(updated.Value));
    }

    public async Task<IHttpResult> DeleteAsync(Guid id)
    {
        var result = await _repository.DeleteAsync(x => x.Id == id);
        if (!result.IsSuccess)
            return CatalogErrors.DeleteFailed("estado de localidad");
        return HttpResult.Success();
    }
}

internal static class CatalogErrors
{
    public static HttpResult<T> NotFound<T>(string label) =>
        (HttpResult<T>)HttpResult<T>.Failure($"No se encontró el {label}",
            new Dictionary<string, IEnumerable<string>> { { "errors", [$"No se encontró el {label} solicitado"] } },
            HttpStatusCode.NotFound);

    public static HttpResult<T> DuplicateCode<T>() =>
        (HttpResult<T>)HttpResult<T>.Failure("Código duplicado",
            new Dictionary<string, IEnumerable<string>> { { "codigo", ["Ya existe un registro con ese código"] } },
            HttpStatusCode.Conflict);

    public static HttpResult DeleteFailed(string label) =>
        (HttpResult)HttpResult.Failure($"Error al eliminar el {label}",
            new Dictionary<string, IEnumerable<string>> { { "errors", [$"No se pudo eliminar el {label}"] } },
            HttpStatusCode.BadRequest);
}
