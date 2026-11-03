using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.Responsable;
using PDS.Domain.DTOs.Servicio;
using PDS.Domain.Models;
using PDS.Domain.Response;
using System.Net;

namespace PDS.Service.Services;

// ─────────────────────────────── Servicio (por organización) ───────────────────────────────
public interface IServicioService
{
    Task<IHttpResult<IEnumerable<ServicioDto>>> GetAllAsync(int organizationId);
    Task<IHttpResult<ServicioDto>> GetAsync(int organizationId, Guid id);
    Task<IHttpResult<ServicioDto>> CreateAsync(int organizationId, CreateServicioDto dto);
    Task<IHttpResult<ServicioDto>> UpdateAsync(int organizationId, Guid id, UpdateServicioDto dto);
    Task<IHttpResult> DeleteAsync(int organizationId, Guid id);
}

public class ServicioService(
    ILogger<ServicioService> logger,
    IMapper mapper,
    IRepository<Servicio> repository
) : IServicioService
{
    private readonly ILogger<ServicioService> _logger = logger;
    private readonly IMapper _mapper = mapper;
    private readonly IRepository<Servicio> _repository = repository;

    public async Task<IHttpResult<IEnumerable<ServicioDto>>> GetAllAsync(int organizationId)
    {
        var result = await _repository.FilterAsync(s => s.OrganizationId == organizationId);
        var dtos = _mapper.Map<IEnumerable<ServicioDto>>(result.Value.OrderBy(s => s.Nombre));
        return HttpResult<IEnumerable<ServicioDto>>.Success(dtos);
    }

    public async Task<IHttpResult<ServicioDto>> GetAsync(int organizationId, Guid id)
    {
        var result = await _repository.GetAsync(s => s.OrganizationId == organizationId && s.Id == id);
        if (result.Value is null)
            return CatalogErrors.NotFound<ServicioDto>("servicio");
        return HttpResult<ServicioDto>.Success(_mapper.Map<ServicioDto>(result.Value));
    }

    public async Task<IHttpResult<ServicioDto>> CreateAsync(int organizationId, CreateServicioDto dto)
    {
        var exists = await _repository.GetAsync(s => s.OrganizationId == organizationId && s.Nombre.ToLower() == dto.Nombre.ToLower());
        if (exists.Value is not null)
            return (HttpResult<ServicioDto>)HttpResult<ServicioDto>.Failure("Servicio duplicado",
                new Dictionary<string, IEnumerable<string>> { { "nombre", ["Ya existe un servicio con ese nombre"] } }, HttpStatusCode.Conflict);
        var entity = _mapper.Map<Servicio>(dto);
        entity.OrganizationId = organizationId;
        var created = await _repository.CreateAsync(entity);
        return HttpResult<ServicioDto>.Success(_mapper.Map<ServicioDto>(created.Value), HttpStatusCode.Created);
    }

    public async Task<IHttpResult<ServicioDto>> UpdateAsync(int organizationId, Guid id, UpdateServicioDto dto)
    {
        var current = await _repository.GetAsync(s => s.OrganizationId == organizationId && s.Id == id);
        if (current.Value is null)
            return CatalogErrors.NotFound<ServicioDto>("servicio");
        var dup = await _repository.GetAsync(s => s.OrganizationId == organizationId && s.Nombre.ToLower() == dto.Nombre.ToLower() && s.Id != id);
        if (dup.Value is not null)
            return (HttpResult<ServicioDto>)HttpResult<ServicioDto>.Failure("Servicio duplicado",
                new Dictionary<string, IEnumerable<string>> { { "nombre", ["Ya existe un servicio con ese nombre"] } }, HttpStatusCode.Conflict);
        var entity = current.Value;
        entity.Codigo = dto.Codigo;
        entity.Nombre = dto.Nombre;
        entity.Descripcion = dto.Descripcion;
        var updated = await _repository.UpdateAsync(entity);
        return HttpResult<ServicioDto>.Success(_mapper.Map<ServicioDto>(updated.Value));
    }

    public async Task<IHttpResult> DeleteAsync(int organizationId, Guid id)
    {
        var result = await _repository.DeleteAsync(s => s.OrganizationId == organizationId && s.Id == id);
        if (!result.IsSuccess)
            return CatalogErrors.DeleteFailed("servicio");
        return HttpResult.Success();
    }
}

// ─────────────────────────────── Responsable (por organización) ───────────────────────────────
public interface IResponsableService
{
    /// <param name="canManageCalendar">RF-LO-37: quien tiene <c>localities:calendar:manage</c> ve el vínculo GobID.</param>
    Task<IHttpResult<IEnumerable<ResponsableDto>>> GetAllAsync(int organizationId, bool canManageCalendar);
    Task<IHttpResult<ResponsableDto>> GetAsync(int organizationId, Guid id, bool canManageCalendar);
    Task<IHttpResult<ResponsableDto>> CreateAsync(int organizationId, CreateResponsableDto dto, bool canManageCalendar);
    Task<IHttpResult<ResponsableDto>> UpdateAsync(int organizationId, Guid id, UpdateResponsableDto dto, bool canManageCalendar);
    Task<IHttpResult> DeleteAsync(int organizationId, Guid id);
}

public class ResponsableService(
    ILogger<ResponsableService> logger,
    IMapper mapper,
    IRepository<Responsable> repository
) : IResponsableService
{
    private readonly ILogger<ResponsableService> _logger = logger;
    private readonly IMapper _mapper = mapper;
    private readonly IRepository<Responsable> _repository = repository;

    public async Task<IHttpResult<IEnumerable<ResponsableDto>>> GetAllAsync(int organizationId, bool canManageCalendar)
    {
        var result = await _repository.FilterAsync(r => r.OrganizationId == organizationId);
        var dtos = result.Value.OrderBy(r => r.Nombre).Select(r => Map(r, canManageCalendar)).ToList();
        return HttpResult<IEnumerable<ResponsableDto>>.Success(dtos);
    }

    public async Task<IHttpResult<ResponsableDto>> GetAsync(int organizationId, Guid id, bool canManageCalendar)
    {
        var result = await _repository.GetAsync(r => r.OrganizationId == organizationId && r.Id == id);
        if (result.Value is null)
            return CatalogErrors.NotFound<ResponsableDto>("responsable");
        return HttpResult<ResponsableDto>.Success(Map(result.Value, canManageCalendar));
    }

    public async Task<IHttpResult<ResponsableDto>> CreateAsync(int organizationId, CreateResponsableDto dto, bool canManageCalendar)
    {
        var vinculo = await ResolverVinculoAsync(organizationId, Guid.Empty, null, dto, canManageCalendar);
        if (vinculo.Falla is not null)
            return vinculo.Falla;

        var entity = _mapper.Map<Responsable>(dto);
        entity.OrganizationId = organizationId;
        entity.GobidUserId = vinculo.Aplicar ? vinculo.Valor : null;
        try
        {
            var created = await _repository.CreateAsync(entity);
            return HttpResult<ResponsableDto>.Success(Map(created.Value, canManageCalendar), HttpStatusCode.Created);
        }
        catch (DbUpdateException ex) when (EsViolacionIndiceGobid(ex))
        {
            return VinculoDuplicado<ResponsableDto>();
        }
    }

    public async Task<IHttpResult<ResponsableDto>> UpdateAsync(int organizationId, Guid id, UpdateResponsableDto dto, bool canManageCalendar)
    {
        var current = await _repository.GetAsync(r => r.OrganizationId == organizationId && r.Id == id);
        if (current.Value is null)
            return CatalogErrors.NotFound<ResponsableDto>("responsable");
        var entity = current.Value;

        var vinculo = await ResolverVinculoAsync(organizationId, id, entity.GobidUserId, dto, canManageCalendar);
        if (vinculo.Falla is not null)
            return vinculo.Falla;

        entity.Nombre = dto.Nombre;
        entity.Cargo = dto.Cargo;
        entity.Email = dto.Email;
        entity.Telefono = dto.Telefono;
        // Quien no administra el calendario nunca borra ni cambia un vínculo existente: aquí solo se toca si se resolvió aplicar.
        if (vinculo.Aplicar)
            entity.GobidUserId = vinculo.Valor;
        try
        {
            var updated = await _repository.UpdateAsync(entity);
            return HttpResult<ResponsableDto>.Success(Map(updated.Value, canManageCalendar));
        }
        catch (DbUpdateException ex) when (EsViolacionIndiceGobid(ex))
        {
            return VinculoDuplicado<ResponsableDto>();
        }
    }

    public async Task<IHttpResult> DeleteAsync(int organizationId, Guid id)
    {
        var result = await _repository.DeleteAsync(r => r.OrganizationId == organizationId && r.Id == id);
        if (!result.IsSuccess)
            return CatalogErrors.DeleteFailed("responsable");
        return HttpResult.Success();
    }

    private ResponsableDto Map(Responsable entity, bool canManageCalendar)
    {
        var dto = _mapper.Map<ResponsableDto>(entity);
        // RF-LO-37: el vínculo GobID solo se expone a los administradores de calendario.
        return canManageCalendar ? dto : dto with { GobidUserId = null };
    }

    private readonly record struct Vinculo(bool Aplicar, Guid? Valor, IHttpResult<ResponsableDto>? Falla);

    /// <summary>
    /// RF-LO-37 (§10.4): solo los administradores de calendario fijan o cambian <c>gobidUserId</c>. Ausente = sin cambios;
    /// vacío = desvincular; GUID inválido = 400; GUID ya vinculado a otro responsable de la organización = 409.
    /// </summary>
    private async Task<Vinculo> ResolverVinculoAsync(
        int organizationId, Guid selfId, Guid? actual, ResponsableWriteDto dto, bool canManageCalendar)
    {
        if (!dto.GobidUserIdProvided)
            return new(false, null, null);

        var raw = dto.GobidUserId?.Trim();
        var vacio = string.IsNullOrEmpty(raw);
        var parseado = !vacio && Guid.TryParse(raw, out var g) && g != Guid.Empty ? g : (Guid?)null;
        var valido = vacio || parseado is not null;

        if (!canManageCalendar)
        {
            // Reenviar el mismo valor (o vacío sin vínculo previo) no cambia nada; cualquier otro intento es una escalada.
            var sinCambio = vacio ? actual is null : parseado == actual;
            return sinCambio
                ? new(false, null, null)
                : new(false, null, Fallo("Permiso denegado", "permiso",
                    "Solo los administradores de calendario pueden vincular un usuario GobID.", HttpStatusCode.Forbidden));
        }

        if (!valido)
            return new(false, null, Fallo("Error de validación", "gobidUserId",
                "Ingrese un identificador de usuario GobID válido.", HttpStatusCode.BadRequest));

        if (parseado is { } guid)
        {
            var otro = await _repository.GetAsync(r =>
                r.OrganizationId == organizationId && r.GobidUserId == guid && r.Id != selfId);
            if (otro.Value is not null)
                return new(false, null, VinculoDuplicado<ResponsableDto>());
        }

        return new(true, parseado, null);
    }

    private static bool EsViolacionIndiceGobid(DbUpdateException ex) =>
        ex.ToString().Contains("GobidUserId", StringComparison.OrdinalIgnoreCase); // SQL Server nombra el índice; SQLite, las columnas.

    private static IHttpResult<T> VinculoDuplicado<T>() =>
        Fallo<T>("Usuario GobID duplicado", "gobidUserId",
            "Ese usuario GobID ya está vinculado a otro responsable.", HttpStatusCode.Conflict);

    private static IHttpResult<ResponsableDto> Fallo(string title, string key, string msg, HttpStatusCode code) =>
        Fallo<ResponsableDto>(title, key, msg, code);

    private static IHttpResult<T> Fallo<T>(string title, string key, string msg, HttpStatusCode code) =>
        HttpResult<T>.Failure(title, new Dictionary<string, IEnumerable<string>> { { key, [msg] } }, code);
}
