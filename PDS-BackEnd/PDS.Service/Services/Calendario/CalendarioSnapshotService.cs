using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PDS.Application.Data;
using PDS.Application.Repositories;
using PDS.Service.Services.Calendario.Motor;

namespace PDS.Service.Services.Calendario;

/// <summary>
/// Datos de la localidad que no se cachean porque son baratos de leer (una fila) y deciden la clave de la caché:
/// su <see cref="CalendarioVersion"/> es la versión del calendario efectivo (RF-LO-43).
/// </summary>
public sealed record LocalidadCalendarioBase(
    Guid Id, int OrganizationId, string ZonaHoraria, Guid PaisId, DateOnly? FechaApertura, long CalendarioVersion);

/// <summary>
/// <see cref="CalendarioSnapshot"/> listo para el motor, más lo que los consumidores necesitan para derivar la fuente
/// host/home/mission de cada cierre (país de la localidad y código ISO de los países de sus calendarios).
/// </summary>
public sealed record SnapshotCargado(
    CalendarioSnapshot Snapshot, Guid LocalidadPaisId, IReadOnlyDictionary<Guid, string> PaisCodigos);

/// <summary>
/// RF-LO-34 / RNF-LO-02: construye el snapshot del calendario de una localidad (una consulta por agregado) y lo cachea en memoria
/// con clave (<c>localidadId</c>, <c>CalendarioVersion</c>). La caché solo se invalida por cambio de versión, nunca por tiempo
/// (la expiración deslizante únicamente libera memoria de localidades sin consultas).
/// </summary>
public interface ICalendarioSnapshotService
{
    /// <summary>Fila ligera de la localidad (versión, zona, país, apertura); nulo si no existe, está eliminada o es de otra organización.</summary>
    Task<LocalidadCalendarioBase?> GetBaseAsync(int organizationId, Guid localidadId, CancellationToken ct = default);

    Task<SnapshotCargado> CargarAsync(LocalidadCalendarioBase localidad, CancellationToken ct = default);
}

public class CalendarioSnapshotService(IUnitOfWork<PDSDbContext> uow, IMemoryCache cache) : ICalendarioSnapshotService
{
    private static readonly TimeSpan Inactividad = TimeSpan.FromMinutes(30);

    private PDSDbContext Ctx => uow.Context;

    public Task<LocalidadCalendarioBase?> GetBaseAsync(int organizationId, Guid localidadId, CancellationToken ct = default) =>
        Ctx.Localidades.AsNoTracking()
            .Where(l => l.Id == localidadId && l.OrganizationId == organizationId && l.DeletedAt == null)
            .Select(l => new LocalidadCalendarioBase(l.Id, l.OrganizationId, l.ZonaHoraria, l.PaisId, l.FechaApertura, l.CalendarioVersion))
            .FirstOrDefaultAsync(ct);

    public async Task<SnapshotCargado> CargarAsync(LocalidadCalendarioBase localidad, CancellationToken ct = default)
    {
        var key = Clave(localidad.Id, localidad.CalendarioVersion);
        if (!cache.TryGetValue(key, out DatosCalendario? datos) || datos is null)
        {
            datos = await LeerAsync(localidad, ct);
            cache.Set(key, datos, new MemoryCacheEntryOptions { SlidingExpiration = Inactividad });
        }

        // Zona, apertura y país se leen siempre frescos: viajan en la fila ligera, no en la caché.
        var snapshot = new CalendarioSnapshot(
            localidad.Id, localidad.ZonaHoraria, localidad.CalendarioVersion, localidad.FechaApertura,
            datos.Jornadas, datos.Feriados, datos.Excepciones);
        return new SnapshotCargado(snapshot, localidad.PaisId, datos.PaisCodigos);
    }

    public static string Clave(Guid localidadId, long version) => $"calendario:snapshot:{localidadId:N}:{version}";

    private async Task<DatosCalendario> LeerAsync(LocalidadCalendarioBase localidad, CancellationToken ct)
    {
        var id = localidad.Id;

        // 1) Versiones de la jornada semanal.
        var jornadas = (await Ctx.LocalidadJornadas.AsNoTracking()
                .Where(j => j.LocalidadId == id).OrderBy(j => j.VigenteDesde).ToListAsync(ct))
            .Select(JornadaVersion.From).ToList();

        // 2) Suscripciones a calendarios de feriados (no eliminados) y 3) sus entradas.
        var suscripciones = await Ctx.LocalidadCalendariosFeriados.AsNoTracking()
            .Where(s => s.LocalidadId == id && s.CalendarioFeriados!.DeletedAt == null)
            .Select(s => new
            {
                s.CalendarioFeriadosId,
                s.VigenteDesde,
                s.VigenteHasta,
                Codigo = s.CalendarioFeriados!.Codigo,
                s.CalendarioFeriados.PaisId,
                s.CalendarioFeriados.Ambito,
            })
            .ToListAsync(ct);

        var calendarioIds = suscripciones.Select(s => s.CalendarioFeriadosId).Distinct().ToList();
        var dias = calendarioIds.Count == 0
            ? []
            : await Ctx.CalendarioFeriadosDias.AsNoTracking()
                .Where(d => calendarioIds.Contains(d.CalendarioFeriadosId) && d.DeletedAt == null).ToListAsync(ct);
        var diasPorCalendario = dias.ToLookup(d => d.CalendarioFeriadosId);

        var feriados = suscripciones
            .SelectMany(s => diasPorCalendario[s.CalendarioFeriadosId].Select(d => new FeriadoAplicable(
                s.CalendarioFeriadosId, s.Codigo, s.PaisId, s.Ambito, d.Fecha, d.Nombre, d.Alcance,
                d.Intervalos ?? [], s.VigenteDesde, s.VigenteHasta, d.Id)))
            .ToList();

        // 4) Excepciones de usuario y de sistema.
        var excepciones = (await Ctx.LocalidadExcepcionesCalendario.AsNoTracking()
                .Where(e => e.LocalidadId == id && e.DeletedAt == null).ToListAsync(ct))
            .Select(e => new ExcepcionAplicable(
                e.Id, e.Tipo, e.Categoria, e.Origen, e.FechaInicio, e.FechaFin, e.Intervalos ?? [], e.Motivo, e.EsSistema,
                e.ReferenciaNormativa))
            .ToList();

        // 5) Códigos ISO de los países implicados (para la fuente host/home de los cierres).
        var paisIds = suscripciones.Where(s => s.PaisId is not null).Select(s => s.PaisId!.Value)
            .Append(localidad.PaisId).Distinct().ToList();
        var paises = await Ctx.Paises.AsNoTracking().Where(p => paisIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Codigo, ct);

        return new DatosCalendario(jornadas, feriados, excepciones, paises);
    }

    private sealed record DatosCalendario(
        IReadOnlyList<JornadaVersion> Jornadas,
        IReadOnlyList<FeriadoAplicable> Feriados,
        IReadOnlyList<ExcepcionAplicable> Excepciones,
        IReadOnlyDictionary<Guid, string> PaisCodigos);
}
