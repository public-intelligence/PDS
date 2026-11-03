using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDS.Application.Data;
using PDS.Application.Interceptors;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.Localidad;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.DTOs.Position;
using PDS.Domain.Enums;
using PDS.Domain.Models;
using PDS.Domain.Response;
using PDS.Service.Services.Calendario;
using System.Net;
using System.Text.Json;

namespace PDS.Service.Services;

/// <summary>Códigos estables del catálogo de estados (RF-LO-09/10). La lógica nunca usa Id ni nombre.</summary>
internal static class EstadoLocalidadCodigos
{
    public const string Activa = "ACTIVA";
    public const string Inactiva = "INACTIVA";
    public const string SuspendidaTemp = "SUSPENDIDA_TEMP";
    public const string EnApertura = "EN_APERTURA";
    public const string EnCierre = "EN_CIERRE";
}

public class LocalidadService(
    ILogger<LocalidadService> logger,
    IUnitOfWork<PDSDbContext> unitOfWork,
    IPaginationService paginationService,
    IRepository<Localidad> repository,
    SoftDeleteInterceptor softDeleteInterceptor,
    ICalendarioCambioService calendarioCambios,
    IRetroactiveWindowPolicy ventanaRetroactiva
) : ILocalidadService
{
    private readonly ILogger<LocalidadService> _logger = logger;
    private readonly IRepository<Localidad> _repository = repository;
    private readonly IUnitOfWork<PDSDbContext> _uow = unitOfWork;
    private readonly IPaginationService _paginationService = paginationService;
    private readonly SoftDeleteInterceptor _softDelete = softDeleteInterceptor;
    private readonly ICalendarioCambioService _calendarioCambios = calendarioCambios;
    private readonly IRetroactiveWindowPolicy _ventanaRetroactiva = ventanaRetroactiva;

    /// <summary>RF-LO-30 / OQ-04: código del calendario nacional dominicano al que se suscriben por defecto las localidades dominicanas.</summary>
    public const string CalendarioNacionalDominicano = "DO-NAC";

    private PDSDbContext Ctx => _uow.Context;

    private const double DuplicateRadiusMeters = 150d;

    // ───────────────────────────── Listado / mapa / detalle ─────────────────────────────

    public async Task<IHttpResult<PagedResponseDto<LocalidadListItemDto>>> ListAsync(
        int organizationId, LocalidadFilterDto filter, HttpContext httpContext)
    {
        var baseQuery = ApplyFilters(organizationId, filter);
        var total = await baseQuery.CountAsync();

        var page = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var size = filter.PageSize is < 1 or > 100 ? 20 : filter.PageSize;

        var items = await baseQuery
            .Include(l => l.Tipo)
            .Include(l => l.Estado)
            .Include(l => l.Pais)
            .Include(l => l.Provincia)
            .Include(l => l.Municipio)
            .Include(l => l.Funciones).ThenInclude(x => x.FuncionLocalidad)
            .OrderBy(l => l.NombreOficial)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        var dtos = items.Select(MapListItem).ToList();
        var paged = _paginationService.BuildPaginationUrl(httpContext, page, size, total, dtos);
        return HttpResult<PagedResponseDto<LocalidadListItemDto>>.Success(paged);
    }

    /// <summary>
    /// Lista completa para integraciones M2M (RF-LO-52, D13): todas las localidades que cumplen los filtros, sin paginar y
    /// ordenadas por <c>nombreOficial</c>. Las eliminadas nunca se devuelven. Ignora <c>PageNumber</c> y <c>PageSize</c>.
    /// </summary>
    public async Task<IHttpResult<IEnumerable<LocalidadListItemDto>>> ListAllAsync(
        int organizationId, LocalidadFilterDto filter)
    {
        var items = await ApplyFilters(organizationId, filter)
            .Include(l => l.Tipo)
            .Include(l => l.Estado)
            .Include(l => l.Pais)
            .Include(l => l.Provincia)
            .Include(l => l.Municipio)
            .Include(l => l.Funciones).ThenInclude(x => x.FuncionLocalidad)
            .OrderBy(l => l.NombreOficial)
            .ToListAsync();

        return HttpResult<IEnumerable<LocalidadListItemDto>>.Success(items.Select(MapListItem).ToList());
    }

    public async Task<IHttpResult<IEnumerable<LocalidadListItemDto>>> GetByIdsAsync(
        IEnumerable<Guid> localidaIds, int? organizationId = null)
    {
        var localidadIdsList = localidaIds.ToList();
        if (localidadIdsList.Count == 0)
            return HttpResult<IEnumerable<LocalidadListItemDto>>.Success([]);

        // Las eliminadas nunca se devuelven; con organizationId se limita a esa organización (K-2).
        var query = Ctx.Localidades.AsNoTracking()
            .Where(l => localidadIdsList.Contains(l.Id) && l.DeletedAt == null);
        if (organizationId is { } orgId)
            query = query.Where(l => l.OrganizationId == orgId);

        var localidades = await query
            .Include(l => l.Tipo)
            .Include(l => l.Estado)
            .Include(l => l.Pais)
            .Include(l => l.Provincia)
            .Include(l => l.Municipio)
            .Include(l => l.Funciones).ThenInclude(x => x.FuncionLocalidad)
            .ToListAsync();

        return HttpResult<IEnumerable<LocalidadListItemDto>>.Success(localidades.Select(MapListItem).ToList());
    }

    /// <summary>
    /// Localidad para SDS (M2M). Responde 404 si no existe, está dada de baja o pertenece a otra organización
    /// (no se distingue el motivo para no filtrar información entre tenants).
    /// </summary>
    public async Task<IHttpResult<LocalidadM2MDto>> GetM2MAsync(int organizationId, Guid id)
    {
        var l = await Ctx.Localidades.AsNoTracking()
            .Include(x => x.Estado)
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId && x.DeletedAt == null);
        if (l is null)
            return Fail<LocalidadM2MDto>("Localidad no encontrada", "errors", "No se encontró la localidad", HttpStatusCode.NotFound);

        return HttpResult<LocalidadM2MDto>.Success(new LocalidadM2MDto
        {
            Id = l.Id,
            Codigo = l.Codigo,
            NombreOficial = l.NombreOficial,
            DireccionCompleta = l.DireccionCompleta,
            OrganizationId = l.OrganizationId,
            Activa = string.Equals(l.Estado?.Codigo, EstadoLocalidadCodigos.Activa, StringComparison.OrdinalIgnoreCase),
            IanaTimeZone = l.ZonaHoraria,
            Version = FormatVersion(l.UpdatedAt),
        });
    }

    /// <summary>ISO-8601 en UTC; los valores de la base se tratan como UTC (sin zona).</summary>
    internal static string FormatVersion(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return utc.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", System.Globalization.CultureInfo.InvariantCulture)
            .Replace(".Z", "Z", StringComparison.Ordinal);
    }

    public async Task<IHttpResult<IEnumerable<LocalidadMapItemDto>>> GetMapAsync(
        int organizationId, LocalidadFilterDto filter)
    {
        var items = await ApplyFilters(organizationId, filter)
            .Where(l => l.Latitud != null && l.Longitud != null)
            .Include(l => l.Tipo)
            .Include(l => l.Estado)
            .Include(l => l.UnidadResponsable)
            .OrderBy(l => l.NombreOficial)
            .ToListAsync();

        var dtos = items.Select(l => new LocalidadMapItemDto
        {
            Id = l.Id,
            NombreOficial = l.NombreOficial,
            Codigo = l.Codigo,
            Latitud = l.Latitud!.Value,
            Longitud = l.Longitud!.Value,
            TipoLocalidadId = l.TipoLocalidadId,
            TipoNombre = l.Tipo?.Nombre,
            TipoColor = l.Tipo?.Color,
            EstadoLocalidadId = l.EstadoLocalidadId,
            EstadoNombre = l.Estado?.Nombre,
            EstadoColor = l.Estado?.Color,
            UnidadResponsableId = l.UnidadResponsableId,
            UnidadResponsableNombre = l.UnidadResponsable?.Name,
            DireccionCompleta = l.DireccionCompleta,
        });
        return HttpResult<IEnumerable<LocalidadMapItemDto>>.Success(dtos);
    }

    public async Task<IHttpResult<LocalidadDetailDto>> GetDetailAsync(int organizationId, Guid id)
    {
        var localidad = await DetailQuery(organizationId)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (localidad is null)
            return Fail<LocalidadDetailDto>("Localidad no encontrada", "errors", "No se encontró la localidad", HttpStatusCode.NotFound);

        var historial = await GetHistorialEntitiesAsync(organizationId, id);
        return HttpResult<LocalidadDetailDto>.Success(MapDetail(localidad, historial));
    }

    // ───────────────────────────── Crear / modificar ─────────────────────────────

    public async Task<IHttpResult<LocalidadDetailDto>> CreateAsync(
        int organizationId, Guid userId, string? userName, CreateLocalidadDto dto, bool canManageCalendar = false)
    {
        var gestionaError = ValidateGestionaCalendario(dto.Responsables, canManageCalendar, null);
        if (gestionaError is not null)
            return Fail<LocalidadDetailDto>("Permiso insuficiente", "permiso", gestionaError, HttpStatusCode.Forbidden);

        var zonaHoraria = dto.ZonaHoraria is null ? IanaTimeZones.Default : dto.ZonaHoraria.Trim();
        if (dto.ZonaHoraria is not null)
        {
            // RF-LO-20: el permiso protege el CAMBIO de zona de una localidad existente; al crearla el campo es editable (UI-03).
            var zoneInvalid = ValidateTimeZoneId(zonaHoraria);
            if (zoneInvalid is not null)
                return Fail<LocalidadDetailDto>("Zona horaria inválida", "zonaHoraria", zoneInvalid, HttpStatusCode.BadRequest);
        }

        // 1) Código único (bloqueo duro, RF-LO-01).
        if (await Ctx.Localidades.AnyAsync(l => l.OrganizationId == organizationId && l.DeletedAt == null && l.Codigo == dto.Codigo))
            return Fail<LocalidadDetailDto>("Código duplicado", "codigo", "Ya existe una localidad con ese código", HttpStatusCode.BadRequest);

        // 1b) País y dirección en el exterior (B02 / RF-LO-18).
        var paisError = await ValidateCountryAddressAsync(dto.PaisId, dto.Ciudad, dto.RegionId, dto.ProvinciaId, dto.MunicipioId, dto.DistritoMunicipalId);
        if (paisError is not null)
            return Fail<LocalidadDetailDto>("Dirección inválida", paisError.Value.Campo, paisError.Value.Mensaje, HttpStatusCode.BadRequest);

        // 1c) Funciones de servicio exterior (RF-LO-45).
        var (funcionesCreateError, funcionesCreate) = await ResolveFuncionesAsync(dto.FuncionIds);
        if (funcionesCreateError is not null)
            return Fail<LocalidadDetailDto>("Funciones inválidas", "funciones", funcionesCreateError, HttpStatusCode.BadRequest);

        // 2) Posibles duplicados (advertencia, RF-LO-13). Bloquea salvo override con justificación.
        if (!dto.Override)
        {
            var warnings = (await RunDuplicateScanAsync(organizationId, new DuplicateCheckRequestDto
            {
                Codigo = dto.Codigo,
                NombreOficial = dto.NombreOficial,
                DireccionCompleta = dto.DireccionCompleta,
                Latitud = dto.Latitud,
                Longitud = dto.Longitud,
                PaisId = dto.PaisId,
                Ciudad = dto.Ciudad,
                ProvinciaId = dto.ProvinciaId,
                MunicipioId = dto.MunicipioId,
                TipoLocalidadId = dto.TipoLocalidadId,
            })).Where(w => w.Campo != "codigo").ToList();
            if (warnings.Count > 0)
                return DuplicateConflict<LocalidadDetailDto>(warnings);
        }

        // 3) Consistencia territorial.
        var territorialError = await ValidateTerritorialAsync(dto.PaisId, dto.RegionId, dto.ProvinciaId, dto.MunicipioId, dto.DistritoMunicipalId);
        if (territorialError is not null)
            return Fail<LocalidadDetailDto>("Ubicación territorial inconsistente", "territorio", territorialError, HttpStatusCode.BadRequest);

        // 4) Estado inicial.
        var estadoId = dto.EstadoLocalidadId is { } e && e != Guid.Empty
            ? e
            : await GetEstadoIdByCodigoAsync(EstadoLocalidadCodigos.Activa);
        if (estadoId is null)
            return Fail<LocalidadDetailDto>("Catálogo incompleto", "estado", "No se encontró el estado inicial de la localidad", HttpStatusCode.BadRequest);

        var coordError = ValidateCoordinates(dto.Latitud, dto.Longitud);
        if (coordError is not null)
            return Fail<LocalidadDetailDto>("Coordenadas inválidas", "coordenadas", coordError, HttpStatusCode.BadRequest);

        var entity = new Localidad
        {
            // El Id se asigna aquí (la columna tiene NEWID() por defecto) para que las filas del calendario inicial lo referencien en la misma unidad de trabajo.
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Codigo = dto.Codigo,
            NombreOficial = dto.NombreOficial,
            NombreCorto = dto.NombreCorto,
            Sigla = dto.Sigla,
            TipoLocalidadId = dto.TipoLocalidadId,
            NaturalezaAdministrativaId = dto.NaturalezaAdministrativaId,
            EstadoLocalidadId = estadoId.Value,
            UnidadResponsableId = dto.UnidadResponsableId,
            UnidadVinculadaId = dto.UnidadVinculadaId,
            LocalidadPadreId = dto.LocalidadPadreId,
            PaisId = dto.PaisId,
            RegionId = dto.RegionId,
            ProvinciaId = dto.ProvinciaId,
            MunicipioId = dto.MunicipioId,
            DistritoMunicipalId = dto.DistritoMunicipalId,
            Ciudad = NormalizeText(dto.Ciudad),
            SubdivisionAdministrativa = NormalizeText(dto.SubdivisionAdministrativa),
            SectorBarrioParaje = dto.SectorBarrioParaje,
            DireccionCompleta = dto.DireccionCompleta,
            EdificioPisoLocalReferenciaInterna = dto.EdificioPisoLocalReferenciaInterna,
            CodigoPostal = dto.CodigoPostal,
            Latitud = dto.Latitud,
            Longitud = dto.Longitud,
            ZonaHoraria = zonaHoraria,
            NotasAcceso = dto.NotasAcceso,
            HorarioRegular = dto.HorarioRegular,
            Observaciones = dto.Observaciones,
            FechaApertura = dto.FechaApertura,
            JustificacionDuplicado = dto.Override ? dto.JustificacionDuplicado : null,
        };

        AddChildren(entity, dto.Servicios, dto.Responsables, dto.Documentos, dto.UnidadesPresencia,
            dto.HorariosExcepcion, dto.Coberturas, dto.InstrumentosLegales, userName);
        foreach (var funcion in funcionesCreate)
            entity.Funciones.Add(new LocalidadFuncion { FuncionLocalidadId = funcion.Id });

        entity.Auditorias.Add(BuildAudit(organizationId, LocalidadAccion.Created, userId, userName,
            "Registro inicial de la localidad"));

        Ctx.Localidades.Add(entity);

        // RF-LO-26 / RF-LO-30: horario y calendario de feriados iniciales, en la misma unidad de trabajo que la localidad.
        await AplicarCalendarioInicialAsync(entity, userId, userName);

        await Ctx.SaveChangesAsync();

        return await GetDetailAsync(organizationId, entity.Id);
    }

    /// <summary>
    /// RF-LO-26: una localidad nueva recibe una copia de la jornada vigente en la localidad predeterminada del calendario de su
    /// organización, con <c>VigenteDesde</c> = <c>FechaApertura</c> o la fecha local de hoy; sin predeterminada (o sin jornada)
    /// se usa <see cref="JornadaPorDefecto"/>. Las localidades dominicanas se suscriben además al calendario <c>DO-NAC</c> activo de la
    /// organización, si existe (OQ-04). Versión, feed y bitácora quedan registrados por los servicios del calendario.
    /// </summary>
    private async Task AplicarCalendarioInicialAsync(Localidad entity, Guid userId, string? userName)
    {
        var vigenteDesde = entity.FechaApertura ?? _ventanaRetroactiva.HoyLocal(entity.ZonaHoraria);

        // Línea base de la localidad nueva = 1 (igual que el valor por defecto de la columna); cada cambio inicial la incrementa.
        if (entity.CalendarioVersion < 1) entity.CalendarioVersion = 1;

        List<JornadaDia>? semanaOrigen = null;
        var predeterminadaId = await Ctx.Localidades
            .Where(l => l.OrganizationId == entity.OrganizationId && l.DeletedAt == null && l.EsLocalidadCalendarioPredeterminada && l.Id != entity.Id)
            .Select(l => (Guid?)l.Id)
            .FirstOrDefaultAsync();
        if (predeterminadaId is { } origenId)
        {
            semanaOrigen = (await Ctx.LocalidadJornadas.AsNoTracking()
                .Where(j => j.LocalidadId == origenId && j.VigenteDesde <= vigenteDesde)
                .OrderByDescending(j => j.VigenteDesde)
                .FirstOrDefaultAsync())?.Semana;
        }

        var copiada = semanaOrigen is { Count: > 0 };
        Ctx.LocalidadJornadas.Add(new LocalidadJornada
        {
            LocalidadId = entity.Id,
            VigenteDesde = vigenteDesde,
            Semana = copiada ? JornadaPorDefecto.Copiar(semanaOrigen!) : JornadaPorDefecto.LunesAViernes(),
            Motivo = copiada
                ? "Horario laboral inicial copiado de la localidad predeterminada del calendario"
                : $"Horario laboral inicial por defecto ({JornadaPorDefecto.Descripcion})",
            CreadoPorId = userId,
            CreadoPor = userName,
        });
        await _calendarioCambios.RegistrarAsync(entity.Id, MotivoCambioCalendario.Jornada, vigenteDesde, null);
        var auditoriaJornada = BuildAudit(entity.OrganizationId, LocalidadAccion.ScheduleChanged, userId, userName,
            copiada ? "Horario laboral copiado de la localidad predeterminada" : $"Horario laboral por defecto ({JornadaPorDefecto.Descripcion})");
        auditoriaJornada.Canal = CanalCambio.Sistema;
        entity.Auditorias.Add(auditoriaJornada);

        var esDominicana = await Ctx.Paises.AnyAsync(p => p.Id == entity.PaisId && p.Codigo == "DO");
        if (!esDominicana) return;

        var calendarioId = await Ctx.CalendariosFeriados
            .Where(c => c.OrganizationId == entity.OrganizationId && c.Codigo == CalendarioNacionalDominicano && c.DeletedAt == null && c.Activo)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync();
        if (calendarioId is not { } calId) return;

        Ctx.LocalidadCalendariosFeriados.Add(new LocalidadCalendarioFeriados
        {
            LocalidadId = entity.Id,
            CalendarioFeriadosId = calId,
            CreadoPorId = userId,
            CreadoPor = userName,
        });
        await _calendarioCambios.RegistrarAsync(entity.Id, MotivoCambioCalendario.Suscripcion);
        var auditoriaSuscripcion = BuildAudit(entity.OrganizationId, LocalidadAccion.HolidaySubscriptionsChanged, userId, userName,
            $"Suscripción inicial al calendario {CalendarioNacionalDominicano}");
        auditoriaSuscripcion.Canal = CanalCambio.Sistema;
        entity.Auditorias.Add(auditoriaSuscripcion);
    }

    public async Task<IHttpResult<LocalidadDetailDto>> UpdateAsync(
        int organizationId, Guid id, Guid userId, string? userName, UpdateLocalidadDto dto, bool canManageCalendar = false)
    {
        _softDelete.ApplySoftDelete = false; // los reemplazos de colecciones hijas son borrados físicos.

        var entity = await Ctx.Localidades
            .Include(l => l.Servicios)
            .Include(l => l.Responsables)
            .Include(l => l.Funciones).ThenInclude(x => x.FuncionLocalidad)
            .Include(l => l.Documentos)
            .Include(l => l.UnidadesPresencia)
            .Include(l => l.HorariosExcepcion)
            .Include(l => l.Coberturas)
            .Include(l => l.InstrumentosLegales)
            .FirstOrDefaultAsync(l => l.OrganizationId == organizationId && l.DeletedAt == null && l.Id == id);
        if (entity is null)
            return Fail<LocalidadDetailDto>("Localidad no encontrada", "errors", "No se encontró la localidad", HttpStatusCode.NotFound);

        if (await Ctx.Localidades.AnyAsync(l => l.OrganizationId == organizationId && l.DeletedAt == null && l.Codigo == dto.Codigo && l.Id != id))
            return Fail<LocalidadDetailDto>("Código duplicado", "codigo", "Ya existe una localidad con ese código", HttpStatusCode.BadRequest);

        var paisError = await ValidateCountryAddressAsync(dto.PaisId, dto.Ciudad, dto.RegionId, dto.ProvinciaId, dto.MunicipioId, dto.DistritoMunicipalId);
        if (paisError is not null)
            return Fail<LocalidadDetailDto>("Dirección inválida", paisError.Value.Campo, paisError.Value.Mensaje, HttpStatusCode.BadRequest);

        if (!dto.Override)
        {
            var warnings = (await RunDuplicateScanAsync(organizationId, new DuplicateCheckRequestDto
            {
                NombreOficial = dto.NombreOficial,
                DireccionCompleta = dto.DireccionCompleta,
                Latitud = dto.Latitud,
                Longitud = dto.Longitud,
                PaisId = dto.PaisId,
                Ciudad = dto.Ciudad,
                ProvinciaId = dto.ProvinciaId,
                MunicipioId = dto.MunicipioId,
                TipoLocalidadId = dto.TipoLocalidadId,
                ExcludeId = id,
            })).Where(w => w.Campo != "codigo").ToList();
            if (warnings.Count > 0)
                return DuplicateConflict<LocalidadDetailDto>(warnings);
        }

        if (dto.LocalidadPadreId is { } parentId && parentId != Guid.Empty)
        {
            if (parentId == id || await IsCyclicHierarchyAsync(organizationId, id, parentId))
                return Fail<LocalidadDetailDto>("Jerarquía inválida", "localidadPadreId",
                    "La localidad no puede depender de sí misma ni de una de sus descendientes", HttpStatusCode.BadRequest);
        }

        var territorialError = await ValidateTerritorialAsync(dto.PaisId, dto.RegionId, dto.ProvinciaId, dto.MunicipioId, dto.DistritoMunicipalId);
        if (territorialError is not null)
            return Fail<LocalidadDetailDto>("Ubicación territorial inconsistente", "territorio", territorialError, HttpStatusCode.BadRequest);

        var coordError = ValidateCoordinates(dto.Latitud, dto.Longitud);
        if (coordError is not null)
            return Fail<LocalidadDetailDto>("Coordenadas inválidas", "coordenadas", coordError, HttpStatusCode.BadRequest);

        var gestionaError = ValidateGestionaCalendario(dto.Responsables, canManageCalendar, entity.Responsables);
        if (gestionaError is not null)
            return Fail<LocalidadDetailDto>("Permiso insuficiente", "permiso", gestionaError, HttpStatusCode.Forbidden);

        var nuevaZona = dto.ZonaHoraria?.Trim();
        var zonaCambia = nuevaZona is not null && nuevaZona != entity.ZonaHoraria;
        if (zonaCambia)
        {
            var zoneError = ValidateTimeZoneChange(nuevaZona!, canManageCalendar);
            if (zoneError is not null)
                return zoneError.Value.Forbidden
                    ? Fail<LocalidadDetailDto>("Permiso insuficiente", "permiso", zoneError.Value.Message, HttpStatusCode.Forbidden)
                    : Fail<LocalidadDetailDto>("Zona horaria inválida", "zonaHoraria", zoneError.Value.Message, HttpStatusCode.BadRequest);
        }

        // RF-LO-45: funciones (null = no tocar). Se valida antes de cambiar nada.
        List<FuncionLocalidad>? funcionesNuevas = null;
        (string[] From, string[] To)? funcionesDiff = null;
        if (dto.FuncionIds is not null)
        {
            var (funcionesError, resueltas) = await ResolveFuncionesAsync(dto.FuncionIds);
            if (funcionesError is not null)
                return Fail<LocalidadDetailDto>("Funciones inválidas", "funciones", funcionesError, HttpStatusCode.BadRequest);
            funcionesNuevas = resueltas;
            funcionesDiff = (
                entity.Funciones.Where(x => x.FuncionLocalidad is { DeletedAt: null }).Select(x => x.FuncionLocalidad!.Codigo).Order(StringComparer.Ordinal).ToArray(),
                resueltas.Select(x => x.Codigo).Order(StringComparer.Ordinal).ToArray());
        }

        // Diff de campos modificados para la bitácora (RF-LO-06).
        var diff = BuildScalarDiff(entity, dto, funcionesDiff);
        if (zonaCambia)
        {
            entity.ZonaHoraria = nuevaZona!;
            // RF-LO-20 / RF-LO-43: un cambio de zona altera el calendario efectivo; versión y feed en la misma unidad de trabajo.
            await _calendarioCambios.RegistrarAsync(id, MotivoCambioCalendario.ZonaHoraria);
        }

        entity.Codigo = dto.Codigo;
        entity.NombreOficial = dto.NombreOficial;
        entity.NombreCorto = dto.NombreCorto;
        entity.Sigla = dto.Sigla;
        entity.TipoLocalidadId = dto.TipoLocalidadId;
        entity.NaturalezaAdministrativaId = dto.NaturalezaAdministrativaId;
        entity.UnidadResponsableId = dto.UnidadResponsableId;
        entity.UnidadVinculadaId = dto.UnidadVinculadaId;
        entity.LocalidadPadreId = dto.LocalidadPadreId;
        entity.PaisId = dto.PaisId;
        entity.RegionId = dto.RegionId;
        entity.ProvinciaId = dto.ProvinciaId;
        entity.MunicipioId = dto.MunicipioId;
        entity.DistritoMunicipalId = dto.DistritoMunicipalId;
        entity.Ciudad = NormalizeText(dto.Ciudad);
        entity.SubdivisionAdministrativa = NormalizeText(dto.SubdivisionAdministrativa);
        entity.SectorBarrioParaje = dto.SectorBarrioParaje;
        entity.DireccionCompleta = dto.DireccionCompleta;
        entity.EdificioPisoLocalReferenciaInterna = dto.EdificioPisoLocalReferenciaInterna;
        entity.CodigoPostal = dto.CodigoPostal;
        entity.Latitud = dto.Latitud;
        entity.Longitud = dto.Longitud;
        entity.NotasAcceso = dto.NotasAcceso;
        entity.HorarioRegular = dto.HorarioRegular;
        entity.Observaciones = dto.Observaciones;
        var aperturaAnterior = entity.FechaApertura;
        entity.FechaApertura = dto.FechaApertura;
        // RF-LO-32 / RF-LO-43: antes de la apertura el día es no laborable (fuente CicloVida), así que mover la apertura cambia el calendario efectivo.
        if (RangoCambioApertura(aperturaAnterior, dto.FechaApertura) is { } rangoApertura)
            await _calendarioCambios.RegistrarAsync(id, MotivoCambioCalendario.CicloVida, rangoApertura.Desde, rangoApertura.Hasta);
        if (dto.Override && !string.IsNullOrWhiteSpace(dto.JustificacionDuplicado))
            entity.JustificacionDuplicado = dto.JustificacionDuplicado;

        if (funcionesNuevas is not null)
        {
            var nuevasIds = funcionesNuevas.Select(x => x.Id).ToHashSet();
            foreach (var actual in entity.Funciones.Where(x => !nuevasIds.Contains(x.FuncionLocalidadId)).ToList())
                Ctx.LocalidadFunciones.Remove(actual);
            var actualesIds = entity.Funciones.Select(x => x.FuncionLocalidadId).ToHashSet();
            foreach (var id2 in nuevasIds.Where(x => !actualesIds.Contains(x)))
                Ctx.LocalidadFunciones.Add(new LocalidadFuncion { LocalidadId = entity.Id, FuncionLocalidadId = id2 });
        }

        // Reemplazo de colecciones hijas (null = no tocar).
        ReplaceChildren(entity, dto.Servicios, dto.Responsables, dto.Documentos, dto.UnidadesPresencia,
            dto.HorariosExcepcion, dto.Coberturas, dto.InstrumentosLegales, userName);

        Ctx.LocalidadAuditorias.Add(BuildAudit(organizationId, LocalidadAccion.Modified, userId, userName,
            dto.Motivo, dto.InstrumentoLegalId, diff, id));

        await Ctx.SaveChangesAsync();
        return await GetDetailAsync(organizationId, id);
    }

    // ───────────────────────────── Ciclo de vida ─────────────────────────────

    public async Task<IHttpResult> SuspenderAsync(
        int organizationId, Guid id, Guid userId, string? userName, SuspenderLocalidadDto dto)
    {
        var entity = await GetTrackedAsync(organizationId, id);
        if (entity is null)
            return Fail("Localidad no encontrada", "errors", "No se encontró la localidad", HttpStatusCode.NotFound);

        var estadoActual = await GetEstadoCodigoAsync(entity.EstadoLocalidadId);
        if (estadoActual is EstadoLocalidadCodigos.EnCierre or EstadoLocalidadCodigos.Inactiva)
            return Fail("Operación no permitida", "estado", "No se puede suspender una localidad cerrada o inactiva", HttpStatusCode.Conflict);

        if (dto.LocalidadAlternaId is { } alt && alt == id)
            return Fail("Localidad alterna inválida", "localidadAlternaId", "La localidad alterna no puede ser la misma localidad", HttpStatusCode.BadRequest);

        var estadoId = await GetEstadoIdByCodigoAsync(EstadoLocalidadCodigos.SuspendidaTemp);
        if (estadoId is null)
            return Fail("Catálogo incompleto", "estado", "No existe el estado 'suspendida temporalmente'", HttpStatusCode.BadRequest);

        var inicio = dto.FechaInicio ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (dto.FechaFin is { } fin && fin < inicio)
            return Fail("Fechas inválidas", "fechaFin", "La fecha de fin de la suspensión no puede ser anterior a la de inicio.", HttpStatusCode.BadRequest);

        entity.EstadoLocalidadId = estadoId.Value;
        entity.SuspensionMotivo = dto.Motivo;
        entity.SuspensionFechaInicio = inicio;
        entity.SuspensionFechaFin = dto.FechaFin;
        entity.SuspensionInstrumentoLegalId = dto.InstrumentoLegalId;
        entity.LocalidadAlternaId = dto.LocalidadAlternaId;

        // RF-LO-32: la suspensión es una excepción de sistema (Cierre). Si ya había una vigente, se actualiza en vez de duplicarla.
        var suspension = estadoActual == EstadoLocalidadCodigos.SuspendidaTemp
            ? await UltimaExcepcionSistemaAsync(id, CategoriaExcepcionCalendario.Suspension)
            : null;
        var antes = suspension is null ? default((DateOnly Inicio, DateOnly? Fin)?) : (suspension.FechaInicio, suspension.FechaFin);
        if (suspension is null)
        {
            suspension = NuevaExcepcionSistema(id, CategoriaExcepcionCalendario.Suspension, inicio, dto.FechaFin,
                dto.Motivo, "Suspensión de la localidad", dto.InstrumentoLegalId, userId, userName);
            Ctx.LocalidadExcepcionesCalendario.Add(suspension);
        }
        else
        {
            suspension.FechaInicio = inicio;
            suspension.FechaFin = dto.FechaFin;
            suspension.Motivo = MotivoSistema(dto.Motivo, "Suspensión de la localidad");
            suspension.InstrumentoLegalId = dto.InstrumentoLegalId;
            TocarExcepcionSistema(suspension, userId, userName);
        }
        var rango = UnirRangos(antes, (inicio, dto.FechaFin));
        await _calendarioCambios.RegistrarAsync(id, MotivoCambioCalendario.CicloVida, rango.Desde, rango.Hasta);

        Ctx.LocalidadAuditorias.Add(BuildAudit(organizationId, LocalidadAccion.Suspended, userId, userName,
            dto.Motivo, dto.InstrumentoLegalId, null, id));
        await Ctx.SaveChangesAsync();
        return HttpResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<IHttpResult> ReactivarAsync(
        int organizationId, Guid id, Guid userId, string? userName, ReactivarLocalidadDto dto)
    {
        var entity = await GetTrackedAsync(organizationId, id);
        if (entity is null)
            return Fail("Localidad no encontrada", "errors", "No se encontró la localidad", HttpStatusCode.NotFound);

        var estadoActual = await GetEstadoCodigoAsync(entity.EstadoLocalidadId);
        if (estadoActual != EstadoLocalidadCodigos.SuspendidaTemp)
            return Fail("Operación no permitida", "estado", "Solo se puede reactivar una localidad suspendida temporalmente", HttpStatusCode.Conflict);

        var estadoId = await GetEstadoIdByCodigoAsync(EstadoLocalidadCodigos.Activa);
        if (estadoId is null)
            return Fail("Catálogo incompleto", "estado", "No existe el estado 'activa'", HttpStatusCode.BadRequest);

        entity.EstadoLocalidadId = estadoId.Value;
        entity.SuspensionMotivo = null;
        entity.SuspensionFechaInicio = null;
        entity.SuspensionFechaFin = null;
        entity.SuspensionInstrumentoLegalId = null;
        entity.LocalidadAlternaId = null;

        // RF-LO-32: la excepción de sistema termina el día local anterior a la reactivación (el día de reactivación sigue la jornada);
        // el historial persiste. Si todavía no había empezado, se descarta. Si ya había terminado, el calendario efectivo no cambia.
        var suspension = await UltimaExcepcionSistemaAsync(id, CategoriaExcepcionCalendario.Suspension);
        if (suspension is not null)
        {
            var hoy = _ventanaRetroactiva.HoyLocal(entity.ZonaHoraria);
            var finAnterior = suspension.FechaFin;
            if (finAnterior is null || finAnterior >= hoy)
            {
                var desde = suspension.FechaInicio > hoy ? suspension.FechaInicio : hoy;
                if (suspension.FechaInicio >= hoy)
                    suspension.DeletedAt = DateTime.UtcNow;
                else
                    suspension.FechaFin = hoy.AddDays(-1);
                TocarExcepcionSistema(suspension, userId, userName);
                await _calendarioCambios.RegistrarAsync(id, MotivoCambioCalendario.CicloVida, desde, finAnterior);
            }
        }

        Ctx.LocalidadAuditorias.Add(BuildAudit(organizationId, LocalidadAccion.Reactivated, userId, userName,
            dto.Motivo, null, null, id));
        await Ctx.SaveChangesAsync();
        return HttpResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<IHttpResult> CerrarAsync(
        int organizationId, Guid id, Guid userId, string? userName, CerrarLocalidadDto dto)
    {
        var entity = await GetTrackedAsync(organizationId, id);
        if (entity is null)
            return Fail("Localidad no encontrada", "errors", "No se encontró la localidad", HttpStatusCode.NotFound);

        if (dto.LocalidadSucesoraId is { } suc && suc == id)
            return Fail("Localidad sucesora inválida", "localidadSucesoraId", "La localidad sucesora no puede ser la misma localidad", HttpStatusCode.BadRequest);

        var estadoId = await GetEstadoIdByCodigoAsync(EstadoLocalidadCodigos.EnCierre);
        if (estadoId is null)
            return Fail("Catálogo incompleto", "estado", "No existe el estado 'en cierre'", HttpStatusCode.BadRequest);

        entity.EstadoLocalidadId = estadoId.Value;
        entity.CierreFecha = dto.CierreFecha ?? DateOnly.FromDateTime(DateTime.UtcNow);
        entity.CierreMotivo = dto.Motivo;
        entity.CierreInstrumentoLegalId = dto.InstrumentoLegalId;
        entity.LocalidadSucesoraId = dto.LocalidadSucesoraId;
        entity.ServiciosAfectados = dto.ServiciosAfectados;
        // No se establece DeletedAt: el cierre es de ciclo de vida, no eliminación física (RF-LO-10).

        // RF-LO-32: el cierre definitivo es una excepción de sistema abierta desde CierreFecha. Un segundo cierre reubica la existente.
        var cierre = await UltimaExcepcionSistemaAsync(id, CategoriaExcepcionCalendario.CierreDefinitivo);
        var cierreFecha = entity.CierreFecha.Value;
        var desdeCierre = cierreFecha;
        if (cierre is null)
        {
            Ctx.LocalidadExcepcionesCalendario.Add(NuevaExcepcionSistema(id, CategoriaExcepcionCalendario.CierreDefinitivo, cierreFecha, null,
                dto.Motivo, "Cierre definitivo de la localidad", dto.InstrumentoLegalId, userId, userName));
        }
        else
        {
            if (cierre.FechaInicio < desdeCierre) desdeCierre = cierre.FechaInicio;
            cierre.FechaInicio = cierreFecha;
            cierre.Motivo = MotivoSistema(dto.Motivo, "Cierre definitivo de la localidad");
            cierre.InstrumentoLegalId = dto.InstrumentoLegalId;
            TocarExcepcionSistema(cierre, userId, userName);
        }
        await _calendarioCambios.RegistrarAsync(id, MotivoCambioCalendario.CicloVida, desdeCierre, null);

        Ctx.LocalidadAuditorias.Add(BuildAudit(organizationId, LocalidadAccion.Closed, userId, userName,
            dto.Motivo, dto.InstrumentoLegalId, null, id));
        await Ctx.SaveChangesAsync();
        return HttpResult.Success(HttpStatusCode.NoContent);
    }

    // ───────────────────────────── Excepciones de sistema (RF-LO-32) ─────────────────────────────

    private const int MaxMotivoExcepcion = 1000;

    private static string MotivoSistema(string? motivo, string porDefecto)
    {
        var texto = string.IsNullOrWhiteSpace(motivo) ? porDefecto : motivo.Trim();
        return texto.Length > MaxMotivoExcepcion ? texto[..MaxMotivoExcepcion] : texto;
    }

    private static LocalidadExcepcionCalendario NuevaExcepcionSistema(
        Guid localidadId, CategoriaExcepcionCalendario categoria, DateOnly inicio, DateOnly? fin,
        string? motivo, string motivoPorDefecto, Guid? instrumentoLegalId, Guid userId, string? userName)
    {
        var ahora = DateTime.UtcNow;
        return new LocalidadExcepcionCalendario
        {
            Id = Guid.NewGuid(),
            LocalidadId = localidadId,
            FechaInicio = inicio,
            FechaFin = fin,
            Tipo = TipoExcepcionCalendario.Cierre,
            Categoria = categoria,
            Origen = OrigenExcepcionCalendario.Institucional,
            Motivo = MotivoSistema(motivo, motivoPorDefecto),
            InstrumentoLegalId = instrumentoLegalId,
            EsSistema = true,
            CreatedAt = ahora,
            UpdatedAt = ahora,
            CreadoPorId = userId,
            CreadoPor = userName,
        };
    }

    private static void TocarExcepcionSistema(LocalidadExcepcionCalendario e, Guid userId, string? userName)
    {
        e.UpdatedAt = DateTime.UtcNow;
        e.ModificadoPorId = userId;
        e.ModificadoPor = userName;
    }

    /// <summary>La excepción de sistema vigente (no eliminada) más reciente de la categoría, rastreada para poder modificarla.</summary>
    private async Task<LocalidadExcepcionCalendario?> UltimaExcepcionSistemaAsync(Guid localidadId, CategoriaExcepcionCalendario categoria)
    {
        var filas = await Ctx.LocalidadExcepcionesCalendario
            .Where(e => e.LocalidadId == localidadId && e.EsSistema && e.Categoria == categoria && e.DeletedAt == null)
            .ToListAsync();
        return filas.OrderByDescending(e => e.FechaInicio).ThenByDescending(e => e.CreatedAt).FirstOrDefault();
    }

    /// <summary>Unión de dos rangos de fechas; <c>null</c> en un extremo = sin límite.</summary>
    private static (DateOnly? Desde, DateOnly? Hasta) UnirRangos((DateOnly Inicio, DateOnly? Fin)? a, (DateOnly Inicio, DateOnly? Fin) b)
    {
        if (a is not { } x) return (b.Inicio, b.Fin);
        var desde = x.Inicio < b.Inicio ? x.Inicio : b.Inicio;
        DateOnly? hasta = x.Fin is { } fa && b.Fin is { } fb ? (fa > fb ? fa : fb) : null;
        return (desde, hasta);
    }

    /// <summary>
    /// Días cuyo resultado cambia al mover <c>FechaApertura</c>: son no laborables los anteriores a ella. Sin límite inferior,
    /// <c>Desde</c> es <c>null</c>. Devuelve <c>null</c> si no cambió.
    /// </summary>
    private static (DateOnly? Desde, DateOnly? Hasta)? RangoCambioApertura(DateOnly? antes, DateOnly? despues)
    {
        if (antes == despues) return null;
        static DateOnly? Previo(DateOnly d) => d == DateOnly.MinValue ? null : d.AddDays(-1);
        return (antes, despues) switch
        {
            ({ } a, { } d) => a < d ? (a, Previo(d)) : (d, Previo(a)),
            ({ } a, null) => (null, Previo(a)),
            (null, { } d) => (null, Previo(d)),
            _ => null,
        };
    }

    // ───────────────────────────── Duplicados / historial ─────────────────────────────

    public async Task<IHttpResult<DuplicateCheckResultDto>> ValidarDuplicadoAsync(
        int organizationId, DuplicateCheckRequestDto request)
    {
        var warnings = await RunDuplicateScanAsync(organizationId, request);
        return HttpResult<DuplicateCheckResultDto>.Success(new DuplicateCheckResultDto
        {
            HasWarnings = warnings.Count > 0,
            Warnings = warnings,
        });
    }

    public async Task<IHttpResult<IEnumerable<LocalidadAuditoriaDto>>> GetHistorialAsync(int organizationId, Guid id)
    {
        var rows = await GetHistorialEntitiesAsync(organizationId, id);
        var dtos = rows.Select(MapAuditoria);
        return HttpResult<IEnumerable<LocalidadAuditoriaDto>>.Success(dtos);
    }

    // ───────────────────────────── Sub-recursos ─────────────────────────────

    public async Task<IHttpResult<IEnumerable<LocalidadServicioDto>>> GetServiciosAsync(int organizationId, Guid localidadId)
    {
        if (!await OwnsAsync(organizationId, localidadId))
            return Fail<IEnumerable<LocalidadServicioDto>>("Localidad no encontrada", "errors", "No se encontró la localidad", HttpStatusCode.NotFound);
        var rows = await Ctx.LocalidadServicios.AsNoTracking()
            .Include(s => s.Servicio)
            .Where(s => s.LocalidadId == localidadId && s.DeletedAt == null)
            .ToListAsync();
        return HttpResult<IEnumerable<LocalidadServicioDto>>.Success(rows.Select(MapServicio));
    }

    public async Task<IHttpResult<LocalidadServicioDto>> AddServicioAsync(int organizationId, Guid localidadId, LocalidadServicioInputDto dto)
    {
        if (!await OwnsAsync(organizationId, localidadId))
            return Fail<LocalidadServicioDto>("Localidad no encontrada", "errors", "No se encontró la localidad", HttpStatusCode.NotFound);
        var entity = new LocalidadServicio
        {
            LocalidadId = localidadId,
            ServicioId = dto.ServicioId,
            Habilitado = dto.Habilitado,
            FechaHabilitacion = dto.FechaHabilitacion ?? DateOnly.FromDateTime(DateTime.UtcNow),
        };
        Ctx.LocalidadServicios.Add(entity);
        await Ctx.SaveChangesAsync();
        var saved = await Ctx.LocalidadServicios.AsNoTracking().Include(s => s.Servicio).FirstAsync(s => s.Id == entity.Id);
        return HttpResult<LocalidadServicioDto>.Success(MapServicio(saved), HttpStatusCode.Created);
    }

    public async Task<IHttpResult> RemoveServicioAsync(int organizationId, Guid localidadId, Guid linkId)
    {
        if (!await OwnsAsync(organizationId, localidadId))
            return Fail("Localidad no encontrada", "errors", "No se encontró la localidad", HttpStatusCode.NotFound);
        _softDelete.ApplySoftDelete = false;
        var row = await Ctx.LocalidadServicios.FirstOrDefaultAsync(s => s.Id == linkId && s.LocalidadId == localidadId);
        if (row is null) return HttpResult.Success(HttpStatusCode.NoContent);
        Ctx.LocalidadServicios.Remove(row);
        await Ctx.SaveChangesAsync();
        return HttpResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<IHttpResult<IEnumerable<LocalidadResponsableDto>>> GetResponsablesAsync(int organizationId, Guid localidadId)
    {
        if (!await OwnsAsync(organizationId, localidadId))
            return Fail<IEnumerable<LocalidadResponsableDto>>("Localidad no encontrada", "errors", "No se encontró la localidad", HttpStatusCode.NotFound);
        var rows = await Ctx.LocalidadResponsables.AsNoTracking()
            .Include(r => r.Responsable)
            .Where(r => r.LocalidadId == localidadId && r.DeletedAt == null)
            .ToListAsync();
        return HttpResult<IEnumerable<LocalidadResponsableDto>>.Success(rows.Select(MapResponsable));
    }

    public async Task<IHttpResult<LocalidadResponsableDto>> AddResponsableAsync(
        int organizationId, Guid localidadId, LocalidadResponsableInputDto dto, bool canManageCalendar = false)
    {
        var gestionaError = ValidateGestionaCalendario([dto], canManageCalendar, null);
        if (gestionaError is not null)
            return Fail<LocalidadResponsableDto>("Permiso insuficiente", "permiso", gestionaError, HttpStatusCode.Forbidden);
        if (!await OwnsAsync(organizationId, localidadId))
            return Fail<LocalidadResponsableDto>("Localidad no encontrada", "errors", "No se encontró la localidad", HttpStatusCode.NotFound);
        var entity = new LocalidadResponsable
        {
            LocalidadId = localidadId,
            ResponsableId = dto.ResponsableId,
            Rol = dto.Rol,
            Principal = dto.Principal,
            GestionaCalendario = dto.GestionaCalendario ?? false,
            FechaAsignacion = dto.FechaAsignacion ?? DateOnly.FromDateTime(DateTime.UtcNow),
        };
        Ctx.LocalidadResponsables.Add(entity);
        await Ctx.SaveChangesAsync();
        var saved = await Ctx.LocalidadResponsables.AsNoTracking().Include(r => r.Responsable).FirstAsync(r => r.Id == entity.Id);
        return HttpResult<LocalidadResponsableDto>.Success(MapResponsable(saved), HttpStatusCode.Created);
    }

    public async Task<IHttpResult> RemoveResponsableAsync(int organizationId, Guid localidadId, Guid linkId)
    {
        if (!await OwnsAsync(organizationId, localidadId))
            return Fail("Localidad no encontrada", "errors", "No se encontró la localidad", HttpStatusCode.NotFound);
        _softDelete.ApplySoftDelete = false;
        var row = await Ctx.LocalidadResponsables.FirstOrDefaultAsync(r => r.Id == linkId && r.LocalidadId == localidadId);
        if (row is null) return HttpResult.Success(HttpStatusCode.NoContent);
        Ctx.LocalidadResponsables.Remove(row);
        await Ctx.SaveChangesAsync();
        return HttpResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<IHttpResult<IEnumerable<LocalidadDocumentoDto>>> GetDocumentosAsync(int organizationId, Guid localidadId)
    {
        if (!await OwnsAsync(organizationId, localidadId))
            return Fail<IEnumerable<LocalidadDocumentoDto>>("Localidad no encontrada", "errors", "No se encontró la localidad", HttpStatusCode.NotFound);
        var rows = await Ctx.LocalidadDocumentos.AsNoTracking()
            .Where(d => d.LocalidadId == localidadId && d.DeletedAt == null)
            .ToListAsync();
        return HttpResult<IEnumerable<LocalidadDocumentoDto>>.Success(rows.Select(MapDocumento));
    }

    public async Task<IHttpResult<LocalidadDocumentoDto>> AddDocumentoAsync(int organizationId, Guid localidadId, string? userName, LocalidadDocumentoInputDto dto)
    {
        if (!await OwnsAsync(organizationId, localidadId))
            return Fail<LocalidadDocumentoDto>("Localidad no encontrada", "errors", "No se encontró la localidad", HttpStatusCode.NotFound);
        var entity = new LocalidadDocumento
        {
            LocalidadId = localidadId,
            TipoDocumento = dto.TipoDocumento,
            Descripcion = dto.Descripcion,
            Url = dto.Url,
            FechaRegistro = DateTime.UtcNow,
            RegistradoPor = userName,
        };
        Ctx.LocalidadDocumentos.Add(entity);
        await Ctx.SaveChangesAsync();
        return HttpResult<LocalidadDocumentoDto>.Success(MapDocumento(entity), HttpStatusCode.Created);
    }

    public async Task<IHttpResult> RemoveDocumentoAsync(int organizationId, Guid localidadId, Guid linkId)
    {
        if (!await OwnsAsync(organizationId, localidadId))
            return Fail("Localidad no encontrada", "errors", "No se encontró la localidad", HttpStatusCode.NotFound);
        _softDelete.ApplySoftDelete = false;
        var row = await Ctx.LocalidadDocumentos.FirstOrDefaultAsync(d => d.Id == linkId && d.LocalidadId == localidadId);
        if (row is null) return HttpResult.Success(HttpStatusCode.NoContent);
        Ctx.LocalidadDocumentos.Remove(row);
        await Ctx.SaveChangesAsync();
        return HttpResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<IHttpResult<IEnumerable<LocalidadUnidadPresenciaDto>>> GetUnidadesPresenciaAsync(int organizationId, Guid localidadId)
    {
        if (!await OwnsAsync(organizationId, localidadId))
            return Fail<IEnumerable<LocalidadUnidadPresenciaDto>>("Localidad no encontrada", "errors", "No se encontró la localidad", HttpStatusCode.NotFound);
        var rows = await Ctx.LocalidadUnidadesPresencia.AsNoTracking()
            .Include(u => u.UnidadOrganizativa)
            .Where(u => u.LocalidadId == localidadId)
            .ToListAsync();
        return HttpResult<IEnumerable<LocalidadUnidadPresenciaDto>>.Success(rows.Select(MapUnidadPresencia));
    }

    public async Task<IHttpResult<LocalidadUnidadPresenciaDto>> AddUnidadPresenciaAsync(int organizationId, Guid localidadId, LocalidadUnidadPresenciaInputDto dto)
    {
        if (!await OwnsAsync(organizationId, localidadId))
            return Fail<LocalidadUnidadPresenciaDto>("Localidad no encontrada", "errors", "No se encontró la localidad", HttpStatusCode.NotFound);
        var entity = new LocalidadUnidadPresencia
        {
            LocalidadId = localidadId,
            UnidadOrganizativaId = dto.UnidadOrganizativaId,
            Notas = dto.Notas,
        };
        Ctx.LocalidadUnidadesPresencia.Add(entity);
        await Ctx.SaveChangesAsync();
        var saved = await Ctx.LocalidadUnidadesPresencia.AsNoTracking().Include(u => u.UnidadOrganizativa).FirstAsync(u => u.Id == entity.Id);
        return HttpResult<LocalidadUnidadPresenciaDto>.Success(MapUnidadPresencia(saved), HttpStatusCode.Created);
    }

    public async Task<IHttpResult> RemoveUnidadPresenciaAsync(int organizationId, Guid localidadId, Guid linkId)
    {
        if (!await OwnsAsync(organizationId, localidadId))
            return Fail("Localidad no encontrada", "errors", "No se encontró la localidad", HttpStatusCode.NotFound);
        var row = await Ctx.LocalidadUnidadesPresencia.FirstOrDefaultAsync(u => u.Id == linkId && u.LocalidadId == localidadId);
        if (row is null) return HttpResult.Success(HttpStatusCode.NoContent);
        Ctx.LocalidadUnidadesPresencia.Remove(row);
        await Ctx.SaveChangesAsync();
        return HttpResult.Success(HttpStatusCode.NoContent);
    }

    // ───────────────────────────── Helpers de consulta ─────────────────────────────

    private IQueryable<Localidad> ApplyFilters(int organizationId, LocalidadFilterDto f)
    {
        var q = Ctx.Localidades.AsNoTracking().Where(l => l.OrganizationId == organizationId && l.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(f.Nombre))
            q = q.Where(l => l.NombreOficial.Contains(f.Nombre) || (l.NombreCorto != null && l.NombreCorto.Contains(f.Nombre)));
        if (!string.IsNullOrWhiteSpace(f.Codigo))
            q = q.Where(l => l.Codigo.Contains(f.Codigo));
        if (f.TipoId is { } tipoId) q = q.Where(l => l.TipoLocalidadId == tipoId);
        if (f.EstadoId is { } estadoId) q = q.Where(l => l.EstadoLocalidadId == estadoId);
        if (f.NaturalezaId is { } natId) q = q.Where(l => l.NaturalezaAdministrativaId == natId);
        if (f.RegionId is { } regionId) q = q.Where(l => l.RegionId == regionId);
        if (f.ProvinciaId is { } provId) q = q.Where(l => l.ProvinciaId == provId);
        if (f.MunicipioId is { } munId) q = q.Where(l => l.MunicipioId == munId);
        if (f.UnidadId is { } unidadId)
            q = q.Where(l => l.UnidadResponsableId == unidadId || l.UnidadVinculadaId == unidadId
                || l.UnidadesPresencia.Any(u => u.UnidadOrganizativaId == unidadId));
        if (f.ServicioId is { } servicioId)
            q = q.Where(l => l.Servicios.Any(s => s.ServicioId == servicioId && s.DeletedAt == null));
        if (f.InstrumentoLegalId is { } instId)
            q = q.Where(l => l.InstrumentosLegales.Any(i => i.InstrumentoLegalId == instId)
                || l.SuspensionInstrumentoLegalId == instId || l.CierreInstrumentoLegalId == instId);

        // RF-LO-19 / RF-LO-45. Los códigos se comparan sin distinguir mayúsculas.
        if (f.PaisId is { } paisId) q = q.Where(l => l.PaisId == paisId);
        if (!string.IsNullOrWhiteSpace(f.PaisCodigo))
        {
            var pais = f.PaisCodigo.Trim().ToUpper();
            q = q.Where(l => l.Pais!.Codigo.ToUpper() == pais);
        }
        if (!string.IsNullOrWhiteSpace(f.EstadoCodigo))
        {
            var estado = f.EstadoCodigo.Trim().ToUpper();
            q = q.Where(l => l.Estado!.Codigo.ToUpper() == estado);
        }
        if (f.FuncionId is { } funcionId)
            q = q.Where(l => l.Funciones.Any(x => x.FuncionLocalidadId == funcionId));
        if (!string.IsNullOrWhiteSpace(f.FuncionCodigo))
        {
            var funcion = f.FuncionCodigo.Trim().ToUpper();
            q = q.Where(l => l.Funciones.Any(x => x.FuncionLocalidad!.Codigo.ToUpper() == funcion && x.FuncionLocalidad.DeletedAt == null));
        }
        return q;
    }

    private IQueryable<Localidad> DetailQuery(int organizationId) =>
        Ctx.Localidades.AsNoTracking()
            .Where(l => l.OrganizationId == organizationId && l.DeletedAt == null)
            .Include(l => l.Tipo)
            .Include(l => l.Naturaleza)
            .Include(l => l.Estado)
            .Include(l => l.Pais)
            .Include(l => l.Region)
            .Include(l => l.Provincia)
            .Include(l => l.Municipio)
            .Include(l => l.DistritoMunicipal)
            .Include(l => l.UnidadResponsable)
            .Include(l => l.UnidadVinculada)
            .Include(l => l.Padre)
            .Include(l => l.Alterna)
            .Include(l => l.Sucesora)
            .Include(l => l.Servicios.Where(s => s.DeletedAt == null)).ThenInclude(s => s.Servicio)
            .Include(l => l.Responsables.Where(r => r.DeletedAt == null)).ThenInclude(r => r.Responsable)
            .Include(l => l.Funciones).ThenInclude(x => x.FuncionLocalidad)
            .Include(l => l.Documentos.Where(d => d.DeletedAt == null))
            .Include(l => l.UnidadesPresencia).ThenInclude(u => u.UnidadOrganizativa)
            .Include(l => l.HorariosExcepcion)
            .Include(l => l.Coberturas).ThenInclude(c => c.Region)
            .Include(l => l.Coberturas).ThenInclude(c => c.Provincia)
            .Include(l => l.Coberturas).ThenInclude(c => c.Municipio)
            .Include(l => l.Coberturas).ThenInclude(c => c.DistritoMunicipal)
            .Include(l => l.InstrumentosLegales).ThenInclude(i => i.InstrumentoLegal);

    private Task<List<LocalidadAuditoria>> GetHistorialEntitiesAsync(int organizationId, Guid id) =>
        Ctx.LocalidadAuditorias.AsNoTracking()
            .Where(a => a.OrganizationId == organizationId && a.LocalidadId == id)
            .OrderByDescending(a => a.Fecha)
            .ToListAsync();

    private Task<Localidad?> GetTrackedAsync(int organizationId, Guid id) =>
        Ctx.Localidades.FirstOrDefaultAsync(l => l.OrganizationId == organizationId && l.DeletedAt == null && l.Id == id);

    private Task<bool> OwnsAsync(int organizationId, Guid id) =>
        Ctx.Localidades.AnyAsync(l => l.OrganizationId == organizationId && l.DeletedAt == null && l.Id == id);

    private async Task<Guid?> GetEstadoIdByCodigoAsync(string codigo)
    {
        var estado = await Ctx.EstadosLocalidad.AsNoTracking().FirstOrDefaultAsync(e => e.Codigo == codigo && e.DeletedAt == null);
        return estado?.Id;
    }

    private async Task<string?> GetEstadoCodigoAsync(Guid estadoId)
    {
        var estado = await Ctx.EstadosLocalidad.AsNoTracking().FirstOrDefaultAsync(e => e.Id == estadoId);
        return estado?.Codigo;
    }

    // ───────────────────────────── Validaciones ─────────────────────────────

    /// <summary>Detecta ciclos al asignar un padre territorial (RF-LO-04). Reutiliza el patrón del organigrama.</summary>
    private async Task<bool> IsCyclicHierarchyAsync(int organizationId, Guid currentId, Guid proposedParentId)
    {
        if (currentId == proposedParentId) return true;
        Guid? cursor = proposedParentId;
        var visited = new HashSet<Guid> { proposedParentId };
        while (cursor.HasValue && cursor.Value != Guid.Empty)
        {
            if (cursor.Value == currentId) return true;
            var parent = await Ctx.Localidades.AsNoTracking()
                .Where(l => l.OrganizationId == organizationId && l.Id == cursor.Value)
                .Select(l => new { l.LocalidadPadreId })
                .FirstOrDefaultAsync();
            if (parent is null) break;
            cursor = parent.LocalidadPadreId;
            if (cursor.HasValue && !visited.Add(cursor.Value)) return true;
        }
        return false;
    }

    /// <summary>Código del país local (República Dominicana): usa la jerarquía territorial dominicana.</summary>
    private const string PaisLocalCodigo = "DO";

    /// <summary>
    /// B02: valida el país de la dirección. El país debe existir. Para República Dominicana aplica la jerarquía
    /// territorial vigente; para un país en el exterior la ciudad es obligatoria y no se admite región, provincia,
    /// municipio ni distrito municipal dominicanos.
    /// </summary>
    private async Task<(string Campo, string Mensaje)?> ValidateCountryAddressAsync(
        Guid paisId, string? ciudad, Guid? regionId, Guid? provinciaId, Guid? municipioId, Guid? distritoId)
    {
        var pais = paisId == Guid.Empty ? null : await Ctx.Paises.AsNoTracking().FirstOrDefaultAsync(p => p.Id == paisId);
        if (pais is null)
            return ("paisId", "El país indicado no existe");

        if (string.Equals(pais.Codigo, PaisLocalCodigo, StringComparison.OrdinalIgnoreCase))
            return null;

        if (string.IsNullOrWhiteSpace(ciudad))
            return ("ciudad", "La ciudad es obligatoria para direcciones en el exterior");
        if (regionId is not null || provinciaId is not null || municipioId is not null || distritoId is not null)
            return ("territorio", "Las localidades en el exterior no pueden usar la división territorial de República Dominicana");
        return null;
    }

    private static string? NormalizeText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Clave de comparación de ciudades: sin acentos, sin mayúsculas y con espacios colapsados.</summary>
    internal static string CityKey(string value)
    {
        var decomposed = value.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        return string.Join(' ', sb.ToString().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    /// <summary>
    /// Consistencia territorial (RF-LO-18): los ids dominicanos deben existir, ser coherentes entre sí y pertenecer al
    /// país indicado en <paramref name="paisId"/> (municipio y distrito heredan el país de su provincia).
    /// </summary>
    private async Task<string?> ValidateTerritorialAsync(Guid paisId, Guid? regionId, Guid? provinciaId, Guid? municipioId, Guid? distritoId)
    {
        if (regionId is { } rid)
        {
            var region = await Ctx.Regiones.AsNoTracking().FirstOrDefaultAsync(r => r.Id == rid);
            if (region is null) return "La región indicada no existe";
            if (region.PaisId != paisId) return "La región no pertenece al país indicado";
        }
        if (provinciaId is { } pid)
        {
            var prov = await Ctx.Provincias.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pid);
            if (prov is null) return "La provincia indicada no existe";
            if (prov.PaisId != paisId) return "La provincia no pertenece al país indicado";
            if (regionId is { } rid2 && prov.RegionId != rid2)
                return "La provincia no pertenece a la región indicada";
        }
        if (municipioId is { } mid)
        {
            var mun = await Ctx.Municipios.AsNoTracking().Include(m => m.Provincia).FirstOrDefaultAsync(m => m.Id == mid);
            if (mun is null) return "El municipio indicado no existe";
            if (provinciaId is { } pid2 && mun.ProvinciaId != pid2)
                return "El municipio no pertenece a la provincia indicada";
            if (mun.Provincia is { } munProv && munProv.PaisId != paisId)
                return "El municipio no pertenece al país indicado";
        }
        if (distritoId is { } did)
        {
            var dm = await Ctx.DistritosMunicipales.AsNoTracking()
                .Include(d => d.Municipio!).ThenInclude(m => m.Provincia)
                .FirstOrDefaultAsync(d => d.Id == did);
            if (dm is null) return "El distrito municipal indicado no existe";
            if (municipioId is { } mid2 && dm.MunicipioId != mid2)
                return "El distrito municipal no pertenece al municipio indicado";
            if (dm.Municipio?.Provincia is { } dmProv && dmProv.PaisId != paisId)
                return "El distrito municipal no pertenece al país indicado";
        }
        return null;
    }

    private static string? ValidateCoordinates(double? lat, double? lng)
    {
        if (lat is null && lng is null) return null;
        if (lat is null || lng is null) return "Debe indicar latitud y longitud, o ninguna";
        if (lat is < -90 or > 90) return "La latitud debe estar entre -90 y 90";
        if (lng is < -180 or > 180) return "La longitud debe estar entre -180 y 180";
        return null;
    }

    // ───────────────────────────── Duplicados ─────────────────────────────

    private async Task<List<DuplicateWarningDto>> RunDuplicateScanAsync(int organizationId, DuplicateCheckRequestDto r)
    {
        var warnings = new List<DuplicateWarningDto>();
        Guid? exclude = r.ExcludeId;

        if (!string.IsNullOrWhiteSpace(r.Codigo))
        {
            var m = await Ctx.Localidades.AsNoTracking().FirstOrDefaultAsync(l =>
                l.OrganizationId == organizationId && l.DeletedAt == null && l.Codigo == r.Codigo && (exclude == null || l.Id != exclude));
            if (m is not null) warnings.Add(Warn("codigo", m, "Ya existe una localidad con el mismo código"));
        }

        if (!string.IsNullOrWhiteSpace(r.NombreOficial))
        {
            var nombre = r.NombreOficial.Trim().ToLower();
            var m = await Ctx.Localidades.AsNoTracking().FirstOrDefaultAsync(l =>
                l.OrganizationId == organizationId && l.DeletedAt == null && l.NombreOficial.ToLower() == nombre && (exclude == null || l.Id != exclude));
            if (m is not null) warnings.Add(Warn("nombreOficial", m, "Ya existe una localidad con el mismo nombre oficial"));
        }

        if (!string.IsNullOrWhiteSpace(r.DireccionCompleta))
        {
            var dir = r.DireccionCompleta.Trim().ToLower();
            var m = await Ctx.Localidades.AsNoTracking().FirstOrDefaultAsync(l =>
                l.OrganizationId == organizationId && l.DeletedAt == null && l.DireccionCompleta.ToLower() == dir && (exclude == null || l.Id != exclude));
            if (m is not null) warnings.Add(Warn("direccionCompleta", m, "Ya existe una localidad con la misma dirección"));
        }

        if (r.ProvinciaId is { } pid && r.MunicipioId is { } mid && r.TipoLocalidadId is { } tid)
        {
            var m = await Ctx.Localidades.AsNoTracking().FirstOrDefaultAsync(l =>
                l.OrganizationId == organizationId && l.DeletedAt == null && l.ProvinciaId == pid && l.MunicipioId == mid
                && l.TipoLocalidadId == tid && (exclude == null || l.Id != exclude));
            if (m is not null) warnings.Add(Warn("provinciaMunicipioTipo", m, "Ya existe una localidad del mismo tipo en esa provincia y municipio"));
        }

        if (r.PaisId is { } paisId && paisId != Guid.Empty && !string.IsNullOrWhiteSpace(r.Ciudad) && r.TipoLocalidadId is { } tipoId)
        {
            var ciudadKey = CityKey(r.Ciudad);
            var mismaTipoPais = await Ctx.Localidades.AsNoTracking()
                .Where(l => l.OrganizationId == organizationId && l.DeletedAt == null && l.PaisId == paisId
                    && l.TipoLocalidadId == tipoId && l.Ciudad != null && (exclude == null || l.Id != exclude))
                .Select(l => new { l.Id, l.Codigo, l.NombreOficial, l.Ciudad })
                .ToListAsync();
            var coincidencia = mismaTipoPais.FirstOrDefault(l => CityKey(l.Ciudad!) == ciudadKey);
            if (coincidencia is not null)
                warnings.Add(new DuplicateWarningDto
                {
                    Campo = "paisCiudadTipo",
                    LocalidadId = coincidencia.Id,
                    Codigo = coincidencia.Codigo,
                    NombreOficial = coincidencia.NombreOficial,
                    Motivo = "Ya existe una localidad del mismo tipo en esa ciudad y país",
                });
        }

        if (r.Latitud is { } lat && r.Longitud is { } lng && ValidateCoordinates(lat, lng) is null)
        {
            var candidatos = await Ctx.Localidades.AsNoTracking()
                .Where(l => l.OrganizationId == organizationId && l.DeletedAt == null && l.Latitud != null && l.Longitud != null
                    && (exclude == null || l.Id != exclude))
                .Select(l => new { l.Id, l.Codigo, l.NombreOficial, Lat = l.Latitud!.Value, Lng = l.Longitud!.Value })
                .ToListAsync();
            foreach (var c in candidatos)
            {
                if (HaversineMeters(lat, lng, c.Lat, c.Lng) <= DuplicateRadiusMeters
                    && warnings.All(w => w.LocalidadId != c.Id || w.Campo != "coordenadas"))
                {
                    warnings.Add(new DuplicateWarningDto
                    {
                        Campo = "coordenadas",
                        LocalidadId = c.Id,
                        Codigo = c.Codigo,
                        NombreOficial = c.NombreOficial,
                        Motivo = "Existe una localidad a menos de 150 m de esas coordenadas",
                    });
                }
            }
        }

        return warnings;

        static DuplicateWarningDto Warn(string campo, Localidad l, string motivo) => new()
        {
            Campo = campo,
            LocalidadId = l.Id,
            Codigo = l.Codigo,
            NombreOficial = l.NombreOficial,
            Motivo = motivo,
        };
    }

    private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double r = 6371000d;
        double dLat = (lat2 - lat1) * Math.PI / 180d;
        double dLon = (lon2 - lon1) * Math.PI / 180d;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(lat1 * Math.PI / 180d) * Math.Cos(lat2 * Math.PI / 180d) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return r * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    // ───────────────────────────── Mutación de colecciones hijas ─────────────────────────────

    private static void AddChildren(
        Localidad entity,
        List<LocalidadServicioInputDto>? servicios,
        List<LocalidadResponsableInputDto>? responsables,
        List<LocalidadDocumentoInputDto>? documentos,
        List<LocalidadUnidadPresenciaInputDto>? unidades,
        List<LocalidadHorarioExcepcionInputDto>? horarios,
        List<LocalidadCoberturaInputDto>? coberturas,
        List<LocalidadInstrumentoLegalInputDto>? instrumentos,
        string? userName)
    {
        if (servicios is not null)
            foreach (var s in servicios)
                entity.Servicios.Add(new LocalidadServicio { ServicioId = s.ServicioId, Habilitado = s.Habilitado, FechaHabilitacion = s.FechaHabilitacion ?? DateOnly.FromDateTime(DateTime.UtcNow) });
        if (responsables is not null)
            foreach (var r in responsables)
                entity.Responsables.Add(new LocalidadResponsable { ResponsableId = r.ResponsableId, Rol = r.Rol, Principal = r.Principal, GestionaCalendario = r.GestionaCalendario ?? false, FechaAsignacion = r.FechaAsignacion ?? DateOnly.FromDateTime(DateTime.UtcNow) });
        if (documentos is not null)
            foreach (var d in documentos)
                entity.Documentos.Add(new LocalidadDocumento { TipoDocumento = d.TipoDocumento, Descripcion = d.Descripcion, Url = d.Url, FechaRegistro = DateTime.UtcNow, RegistradoPor = userName });
        if (unidades is not null)
            foreach (var u in unidades)
                entity.UnidadesPresencia.Add(new LocalidadUnidadPresencia { UnidadOrganizativaId = u.UnidadOrganizativaId, Notas = u.Notas });
        if (horarios is not null)
            foreach (var h in horarios)
                entity.HorariosExcepcion.Add(new LocalidadHorarioExcepcion { Descripcion = h.Descripcion, FechaInicio = h.FechaInicio, FechaFin = h.FechaFin, Horario = h.Horario });
        if (coberturas is not null)
            foreach (var c in coberturas)
                entity.Coberturas.Add(new LocalidadCoberturaTerritorial { RegionId = c.RegionId, ProvinciaId = c.ProvinciaId, MunicipioId = c.MunicipioId, DistritoMunicipalId = c.DistritoMunicipalId });
        if (instrumentos is not null)
            foreach (var i in instrumentos)
                entity.InstrumentosLegales.Add(new LocalidadInstrumentoLegal { InstrumentoLegalId = i.InstrumentoLegalId, Proposito = i.Proposito });
    }

    private void ReplaceChildren(
        Localidad entity,
        List<LocalidadServicioInputDto>? servicios,
        List<LocalidadResponsableInputDto>? responsables,
        List<LocalidadDocumentoInputDto>? documentos,
        List<LocalidadUnidadPresenciaInputDto>? unidades,
        List<LocalidadHorarioExcepcionInputDto>? horarios,
        List<LocalidadCoberturaInputDto>? coberturas,
        List<LocalidadInstrumentoLegalInputDto>? instrumentos,
        string? userName)
    {
        if (servicios is not null)
        {
            Ctx.LocalidadServicios.RemoveRange(entity.Servicios);
            foreach (var s in servicios)
                Ctx.LocalidadServicios.Add(new LocalidadServicio { LocalidadId = entity.Id, ServicioId = s.ServicioId, Habilitado = s.Habilitado, FechaHabilitacion = s.FechaHabilitacion ?? DateOnly.FromDateTime(DateTime.UtcNow) });
        }
        if (responsables is not null)
        {
            // RF-LO-38: el reemplazo no debe borrar la delegación; si el campo no viene se conserva el valor del mismo responsable.
            var gestionaPrevio = entity.Responsables
                .GroupBy(x => x.ResponsableId)
                .ToDictionary(g => g.Key, g => g.Any(x => x.GestionaCalendario));
            Ctx.LocalidadResponsables.RemoveRange(entity.Responsables);
            foreach (var r in responsables)
                Ctx.LocalidadResponsables.Add(new LocalidadResponsable
                {
                    LocalidadId = entity.Id, ResponsableId = r.ResponsableId, Rol = r.Rol, Principal = r.Principal,
                    GestionaCalendario = r.GestionaCalendario ?? (gestionaPrevio.TryGetValue(r.ResponsableId, out var previo) && previo),
                    FechaAsignacion = r.FechaAsignacion ?? DateOnly.FromDateTime(DateTime.UtcNow),
                });
        }
        if (documentos is not null)
        {
            Ctx.LocalidadDocumentos.RemoveRange(entity.Documentos);
            foreach (var d in documentos)
                Ctx.LocalidadDocumentos.Add(new LocalidadDocumento { LocalidadId = entity.Id, TipoDocumento = d.TipoDocumento, Descripcion = d.Descripcion, Url = d.Url, FechaRegistro = DateTime.UtcNow, RegistradoPor = userName });
        }
        if (unidades is not null)
        {
            Ctx.LocalidadUnidadesPresencia.RemoveRange(entity.UnidadesPresencia);
            foreach (var u in unidades)
                Ctx.LocalidadUnidadesPresencia.Add(new LocalidadUnidadPresencia { LocalidadId = entity.Id, UnidadOrganizativaId = u.UnidadOrganizativaId, Notas = u.Notas });
        }
        if (horarios is not null)
        {
            Ctx.LocalidadHorariosExcepcion.RemoveRange(entity.HorariosExcepcion);
            foreach (var h in horarios)
                Ctx.LocalidadHorariosExcepcion.Add(new LocalidadHorarioExcepcion { LocalidadId = entity.Id, Descripcion = h.Descripcion, FechaInicio = h.FechaInicio, FechaFin = h.FechaFin, Horario = h.Horario });
        }
        if (coberturas is not null)
        {
            Ctx.LocalidadCoberturas.RemoveRange(entity.Coberturas);
            foreach (var c in coberturas)
                Ctx.LocalidadCoberturas.Add(new LocalidadCoberturaTerritorial { LocalidadId = entity.Id, RegionId = c.RegionId, ProvinciaId = c.ProvinciaId, MunicipioId = c.MunicipioId, DistritoMunicipalId = c.DistritoMunicipalId });
        }
        if (instrumentos is not null)
        {
            Ctx.LocalidadInstrumentosLegales.RemoveRange(entity.InstrumentosLegales);
            foreach (var i in instrumentos)
                Ctx.LocalidadInstrumentosLegales.Add(new LocalidadInstrumentoLegal { LocalidadId = entity.Id, InstrumentoLegalId = i.InstrumentoLegalId, Proposito = i.Proposito });
        }
    }

    // ───────────────────────────── Bitácora ─────────────────────────────

    private static LocalidadAuditoria BuildAudit(int organizationId, LocalidadAccion accion, Guid userId, string? userName,
        string? motivo, Guid? instrumentoLegalId = null, string? detalle = null, Guid? localidadId = null)
        => new()
        {
            LocalidadId = localidadId ?? Guid.Empty,
            OrganizationId = organizationId,
            Accion = accion,
            UsuarioId = userId,
            Usuario = userName,
            Fecha = DateTime.UtcNow,
            Motivo = motivo,
            InstrumentoLegalId = instrumentoLegalId,
            Detalle = detalle,
        };

    /// <summary>
    /// RF-LO-38 / RF-LO-37: solo quien tiene <c>localities:calendar:manage</c> fija <c>gestionaCalendario</c> (evita la
    /// autoescalada, K-4). Sin el campo nunca se cambia nada; un no administrador que reenvía el valor que ya tiene tampoco
    /// cambia nada y se acepta. Devuelve el mensaje de 403 (clave <c>permiso</c>) o null.
    /// </summary>
    private static string? ValidateGestionaCalendario(
        IEnumerable<LocalidadResponsableInputDto>? entradas, bool canManageCalendar, IEnumerable<LocalidadResponsable>? actuales)
    {
        if (canManageCalendar || entradas is null) return null;
        var previo = (actuales ?? []).GroupBy(x => x.ResponsableId).ToDictionary(g => g.Key, g => g.Any(x => x.GestionaCalendario));
        foreach (var e in entradas)
        {
            if (e.GestionaCalendario is not { } pedido) continue;
            var actual = previo.TryGetValue(e.ResponsableId, out var v) && v;
            if (pedido != actual)
                return "Solo los administradores de calendario pueden definir qué responsables gestionan el calendario.";
        }
        return null;
    }

    /// <summary>
    /// RF-LO-20: cambiar la zona horaria exige <c>localities:calendar:manage</c> (403, clave <c>permiso</c>) y un id IANA válido
    /// (400, clave <c>zonaHoraria</c>).
    /// </summary>
    private static (string Message, bool Forbidden)? ValidateTimeZoneChange(string zoneId, bool canManageCalendar)
    {
        if (!canManageCalendar)
            return ("Solo quien administra calendarios (localities:calendar:manage) puede cambiar la zona horaria de una localidad", true);
        var invalid = ValidateTimeZoneId(zoneId);
        return invalid is null ? null : (invalid, false);
    }

    /// <summary>Valida un id IANA; un id de Windows se rechaza sugiriendo su equivalente IANA (RF-LO-20 AC2/AC3).</summary>
    private static string? ValidateTimeZoneId(string zoneId)
    {
        if (IanaTimeZones.IsValid(zoneId))
            return null;
        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(zoneId, out var iana) && !string.IsNullOrEmpty(iana))
            return $"'{zoneId}' es un identificador de Windows, no IANA. Use '{iana}'";
        return $"'{zoneId}' no es un identificador de zona horaria IANA válido";
    }

    private const string FuncionEmbajada = "EMBAJADA";
    private const string FuncionSeccionConsular = "SECCION_CONSULAR";

    /// <summary>
    /// RF-LO-45: resuelve los ids de función contra el catálogo global (no eliminadas). Devuelve el mensaje de error (400, clave
    /// <c>funciones</c>) si algún id no existe o si <c>SECCION_CONSULAR</c> viene sin <c>EMBAJADA</c>.
    /// </summary>
    private async Task<(string? Error, List<FuncionLocalidad> Funciones)> ResolveFuncionesAsync(IEnumerable<Guid>? ids)
    {
        var distintos = (ids ?? []).Distinct().ToList();
        if (distintos.Count == 0) return (null, []);

        var encontradas = await Ctx.FuncionesLocalidad.AsNoTracking()
            .Where(f => distintos.Contains(f.Id) && f.DeletedAt == null)
            .ToListAsync();
        if (encontradas.Count != distintos.Count)
            return ("Una o más funciones no existen en el catálogo.", []);

        var codigos = encontradas.Select(f => f.Codigo).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (codigos.Contains(FuncionSeccionConsular) && !codigos.Contains(FuncionEmbajada))
            return ("Una sección consular debe pertenecer a una embajada.", []);
        return (null, encontradas);
    }

    private static string? BuildScalarDiff(Localidad before, UpdateLocalidadDto after, (string[] From, string[] To)? funciones = null)
    {
        var changes = new Dictionary<string, object>();
        void Cmp(string field, object? oldVal, object? newVal)
        {
            if (!Equals(oldVal, newVal))
                changes[field] = new { from = oldVal, to = newVal };
        }
        Cmp("codigo", before.Codigo, after.Codigo);
        Cmp("nombreOficial", before.NombreOficial, after.NombreOficial);
        Cmp("nombreCorto", before.NombreCorto, after.NombreCorto);
        Cmp("sigla", before.Sigla, after.Sigla);
        Cmp("tipoLocalidadId", before.TipoLocalidadId, after.TipoLocalidadId);
        Cmp("naturalezaAdministrativaId", before.NaturalezaAdministrativaId, after.NaturalezaAdministrativaId);
        Cmp("unidadResponsableId", before.UnidadResponsableId, after.UnidadResponsableId);
        Cmp("unidadVinculadaId", before.UnidadVinculadaId, after.UnidadVinculadaId);
        Cmp("localidadPadreId", before.LocalidadPadreId, after.LocalidadPadreId);
        Cmp("regionId", before.RegionId, after.RegionId);
        Cmp("provinciaId", before.ProvinciaId, after.ProvinciaId);
        Cmp("municipioId", before.MunicipioId, after.MunicipioId);
        Cmp("distritoMunicipalId", before.DistritoMunicipalId, after.DistritoMunicipalId);
        Cmp("ciudad", before.Ciudad, NormalizeText(after.Ciudad));
        Cmp("subdivisionAdministrativa", before.SubdivisionAdministrativa, NormalizeText(after.SubdivisionAdministrativa));
        Cmp("direccionCompleta", before.DireccionCompleta, after.DireccionCompleta);
        Cmp("sectorBarrioParaje", before.SectorBarrioParaje, after.SectorBarrioParaje);
        Cmp("codigoPostal", before.CodigoPostal, after.CodigoPostal);
        Cmp("latitud", before.Latitud, after.Latitud);
        Cmp("longitud", before.Longitud, after.Longitud);
        Cmp("zonaHoraria", before.ZonaHoraria, after.ZonaHoraria?.Trim() ?? before.ZonaHoraria);
        Cmp("horarioRegular", before.HorarioRegular, after.HorarioRegular);
        Cmp("observaciones", before.Observaciones, after.Observaciones);
        Cmp("fechaApertura", before.FechaApertura, after.FechaApertura);
        if (funciones is { } f && !f.From.SequenceEqual(f.To))
            changes["funciones"] = new { from = f.From, to = f.To };
        return changes.Count == 0 ? null : JsonSerializer.Serialize(changes);
    }

    // ───────────────────────────── Mapeos ─────────────────────────────

    private static LocalidadListItemDto MapListItem(Localidad l) => new()
    {
        Id = l.Id,
        Codigo = l.Codigo,
        NombreOficial = l.NombreOficial,
        TipoLocalidadId = l.TipoLocalidadId,
        TipoNombre = l.Tipo?.Nombre,
        EstadoLocalidadId = l.EstadoLocalidadId,
        EstadoNombre = l.Estado?.Nombre,
        EstadoColor = l.Estado?.Color,
        ProvinciaId = l.ProvinciaId,
        ProvinciaNombre = l.Provincia?.Nombre,
        MunicipioId = l.MunicipioId,
        MunicipioNombre = l.Municipio?.Nombre,
        Latitud = l.Latitud,
        Longitud = l.Longitud,
        PaisId = l.PaisId,
        PaisCodigo = l.Pais?.Codigo,
        PaisNombre = l.Pais?.Nombre,
        Ciudad = l.Ciudad,
        ZonaHoraria = l.ZonaHoraria,
        EstadoCodigo = l.Estado?.Codigo,
        Funciones = l.Funciones
            .Where(x => x.FuncionLocalidad is { DeletedAt: null })
            .Select(x => x.FuncionLocalidad!.Codigo)
            .Order(StringComparer.Ordinal)
            .ToList(),
    };

    private static LocalidadServicioDto MapServicio(LocalidadServicio s) => new()
    {
        Id = s.Id,
        ServicioId = s.ServicioId,
        ServicioNombre = s.Servicio?.Nombre,
        Habilitado = s.Habilitado,
        FechaHabilitacion = s.FechaHabilitacion,
    };

    private static LocalidadResponsableDto MapResponsable(LocalidadResponsable r) => new()
    {
        Id = r.Id,
        ResponsableId = r.ResponsableId,
        ResponsableNombre = r.Responsable?.Nombre,
        Rol = r.Rol,
        Principal = r.Principal,
        FechaAsignacion = r.FechaAsignacion,
        GestionaCalendario = r.GestionaCalendario,
    };

    private static LocalidadDocumentoDto MapDocumento(LocalidadDocumento d) => new()
    {
        Id = d.Id,
        TipoDocumento = d.TipoDocumento,
        Descripcion = d.Descripcion,
        Url = d.Url,
        FechaRegistro = d.FechaRegistro,
        RegistradoPor = d.RegistradoPor,
    };

    private static LocalidadUnidadPresenciaDto MapUnidadPresencia(LocalidadUnidadPresencia u) => new()
    {
        Id = u.Id,
        UnidadOrganizativaId = u.UnidadOrganizativaId,
        UnidadNombre = u.UnidadOrganizativa?.Name,
        Notas = u.Notas,
    };

    private static LocalidadAuditoriaDto MapAuditoria(LocalidadAuditoria a) => new()
    {
        Id = a.Id,
        Accion = a.Accion,
        Motivo = a.Motivo,
        Usuario = a.Usuario,
        Fecha = a.Fecha,
        InstrumentoLegalId = a.InstrumentoLegalId,
        Detalle = a.Detalle,
        Canal = a.Canal,
    };

    private static LocalidadDetailDto MapDetail(Localidad l, List<LocalidadAuditoria> historial) => new()
    {
        Id = l.Id,
        Codigo = l.Codigo,
        NombreOficial = l.NombreOficial,
        NombreCorto = l.NombreCorto,
        Sigla = l.Sigla,
        TipoLocalidadId = l.TipoLocalidadId,
        TipoNombre = l.Tipo?.Nombre,
        NaturalezaAdministrativaId = l.NaturalezaAdministrativaId,
        NaturalezaNombre = l.Naturaleza?.Nombre,
        EstadoLocalidadId = l.EstadoLocalidadId,
        EstadoNombre = l.Estado?.Nombre,
        EstadoColor = l.Estado?.Color,
        UnidadResponsableId = l.UnidadResponsableId,
        UnidadResponsableNombre = l.UnidadResponsable?.Name,
        UnidadVinculadaId = l.UnidadVinculadaId,
        UnidadVinculadaNombre = l.UnidadVinculada?.Name,
        LocalidadPadreId = l.LocalidadPadreId,
        LocalidadPadreNombre = l.Padre?.NombreOficial,
        PaisId = l.PaisId,
        PaisNombre = l.Pais?.Nombre,
        RegionId = l.RegionId,
        RegionNombre = l.Region?.Nombre,
        ProvinciaId = l.ProvinciaId,
        ProvinciaNombre = l.Provincia?.Nombre,
        MunicipioId = l.MunicipioId,
        MunicipioNombre = l.Municipio?.Nombre,
        DistritoMunicipalId = l.DistritoMunicipalId,
        DistritoMunicipalNombre = l.DistritoMunicipal?.Nombre,
        Ciudad = l.Ciudad,
        SubdivisionAdministrativa = l.SubdivisionAdministrativa,
        SectorBarrioParaje = l.SectorBarrioParaje,
        DireccionCompleta = l.DireccionCompleta,
        EdificioPisoLocalReferenciaInterna = l.EdificioPisoLocalReferenciaInterna,
        CodigoPostal = l.CodigoPostal,
        Latitud = l.Latitud,
        Longitud = l.Longitud,
        ZonaHoraria = l.ZonaHoraria,
        EsLocalidadCalendarioPredeterminada = l.EsLocalidadCalendarioPredeterminada,
        CalendarioVersion = l.CalendarioVersion,
        NotasAcceso = l.NotasAcceso,
        HorarioRegular = l.HorarioRegular,
        Observaciones = l.Observaciones,
        FechaApertura = l.FechaApertura,
        SuspensionMotivo = l.SuspensionMotivo,
        SuspensionFechaInicio = l.SuspensionFechaInicio,
        SuspensionFechaFin = l.SuspensionFechaFin,
        LocalidadAlternaId = l.LocalidadAlternaId,
        LocalidadAlternaNombre = l.Alterna?.NombreOficial,
        CierreFecha = l.CierreFecha,
        CierreMotivo = l.CierreMotivo,
        LocalidadSucesoraId = l.LocalidadSucesoraId,
        LocalidadSucesoraNombre = l.Sucesora?.NombreOficial,
        ServiciosAfectados = l.ServiciosAfectados,
        JustificacionDuplicado = l.JustificacionDuplicado,
        CreatedAt = l.CreatedAt,
        UpdatedAt = l.UpdatedAt,
        Servicios = l.Servicios.Select(MapServicio).ToList(),
        Responsables = l.Responsables.Select(MapResponsable).ToList(),
        Funciones = l.Funciones
            .Where(x => x.FuncionLocalidad is { DeletedAt: null })
            .OrderBy(x => x.FuncionLocalidad!.Codigo, StringComparer.Ordinal)
            .Select(x => new LocalidadFuncionDto
            {
                Id = x.FuncionLocalidadId, Codigo = x.FuncionLocalidad!.Codigo, Nombre = x.FuncionLocalidad.Nombre,
                Categoria = x.FuncionLocalidad.Categoria.ToString(),
            })
            .ToList(),
        Documentos = l.Documentos.Select(MapDocumento).ToList(),
        UnidadesPresencia = l.UnidadesPresencia.Select(MapUnidadPresencia).ToList(),
        HorariosExcepcion = l.HorariosExcepcion.Select(h => new LocalidadHorarioExcepcionDto
        {
            Id = h.Id,
            Descripcion = h.Descripcion,
            FechaInicio = h.FechaInicio,
            FechaFin = h.FechaFin,
            Horario = h.Horario,
        }).ToList(),
        Coberturas = l.Coberturas.Select(c => new LocalidadCoberturaDto
        {
            Id = c.Id,
            RegionId = c.RegionId,
            RegionNombre = c.Region?.Nombre,
            ProvinciaId = c.ProvinciaId,
            ProvinciaNombre = c.Provincia?.Nombre,
            MunicipioId = c.MunicipioId,
            MunicipioNombre = c.Municipio?.Nombre,
            DistritoMunicipalId = c.DistritoMunicipalId,
            DistritoMunicipalNombre = c.DistritoMunicipal?.Nombre,
        }).ToList(),
        InstrumentosLegales = l.InstrumentosLegales.Select(i => new LocalidadInstrumentoLegalDto
        {
            Id = i.Id,
            InstrumentoLegalId = i.InstrumentoLegalId,
            Proposito = i.Proposito,
            Numero = i.InstrumentoLegal?.Number,
            Tipo = i.InstrumentoLegal?.Type,
            Fecha = i.InstrumentoLegal?.Date,
            DocumentUrl = i.InstrumentoLegal?.DocumentUrl,
        }).ToList(),
        Historial = historial.Select(MapAuditoria).ToList(),
    };

    // ───────────────────────────── Errores ─────────────────────────────

    private static IHttpResult<T> Fail<T>(string title, string key, string msg, HttpStatusCode code) =>
        HttpResult<T>.Failure(title, new Dictionary<string, IEnumerable<string>> { { key, [msg] } }, code);

    private static IHttpResult Fail(string title, string key, string msg, HttpStatusCode code) =>
        HttpResult.Failure(title, new Dictionary<string, IEnumerable<string>> { { key, [msg] } }, code);

    private static IHttpResult<T> DuplicateConflict<T>(IEnumerable<DuplicateWarningDto> warnings)
    {
        var detail = new Dictionary<string, IEnumerable<string>>
        {
            { "duplicados", warnings.Select(w => $"{w.Motivo}: {w.NombreOficial} ({w.Codigo})") },
        };
        return HttpResult<T>.Failure("Posibles localidades duplicadas", detail, HttpStatusCode.Conflict);
    }
}
