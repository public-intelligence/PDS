using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PDS.Application.Data;
using PDS.Application.Repositories;
using PDS.Domain.Enums;
using PDS.Domain.Models;
using PDS.Service.Settings;

namespace PDS.Service.Services.Calendario;

/// <summary>Una página del feed de cambios (RF-LO-43). <see cref="CursorExpirado"/> indica que el consumidor debe resincronizar (410).</summary>
public sealed record CambiosPagina(IReadOnlyList<CalendarioCambio> Cambios, bool CursorExpirado, bool HayMas = false)
{
    /// <summary>Cursor para la siguiente consulta: el Id de la última fila, o el cursor recibido si no hay nada nuevo.</summary>
    public long SiguienteCursor(long cursorActual) => Cambios.Count > 0 ? Cambios[^1].Id : cursorActual;
}

/// <summary>
/// RF-LO-43: versión del calendario efectivo de cada localidad (<see cref="Localidad.CalendarioVersion"/>) y feed de cambios.
/// <para>
/// <c>Registrar*</c> NO llama a <c>SaveChanges</c>: deja el incremento de versión y la fila de <see cref="CalendarioCambio"/>
/// en el change tracker para que el mismo <c>SaveChanges</c> (misma transacción) del servicio que muta el calendario los persista
/// junto con la mutación. La versión es token de concurrencia: dos escritores simultáneos no pueden emitir la misma versión
/// (el segundo recibe <see cref="DbUpdateConcurrencyException"/> y debe reintentar).
/// </para>
/// </summary>
public interface ICalendarioCambioService
{
    /// <summary>Incrementa la versión de la localidad y registra el cambio. Devuelve la nueva versión.</summary>
    Task<long> RegistrarAsync(Guid localidadId, MotivoCambioCalendario motivo, DateOnly? fechaDesde = null, DateOnly? fechaHasta = null, CancellationToken ct = default);

    /// <summary>
    /// RF-LO-43 AC1: un cambio en las entradas de un calendario de feriados genera UNA fila y UN incremento por localidad suscrita
    /// (no eliminada). Lee las suscripciones ya persistidas. Devuelve localidad → nueva versión.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, long>> RegistrarSuscriptoresAsync(Guid calendarioFeriadosId, DateOnly? fechaDesde = null, DateOnly? fechaHasta = null, CancellationToken ct = default);

    /// <summary>Filas posteriores a <paramref name="cursor"/> de la organización (límite 1..1000), en orden de cursor.</summary>
    Task<CambiosPagina> ObtenerCambiosAsync(int organizationId, long cursor, int limite = 200, CancellationToken ct = default);

    /// <summary>Elimina filas anteriores a la retención (<c>Calendario:RetencionCambiosDias</c>). Devuelve cuántas.</summary>
    Task<int> PurgarExpiradosAsync(CancellationToken ct = default);
}

public class CalendarioCambioService(
    IUnitOfWork<PDSDbContext> uow,
    TimeProvider clock,
    IOptions<CalendarioSettings> settings) : ICalendarioCambioService
{
    public const int LimiteMaximo = 1000;

    private PDSDbContext Ctx => uow.Context;

    public async Task<long> RegistrarAsync(Guid localidadId, MotivoCambioCalendario motivo, DateOnly? fechaDesde = null, DateOnly? fechaHasta = null, CancellationToken ct = default)
    {
        // FindAsync reutiliza la instancia rastreada: varios cambios en la misma unidad de trabajo emiten versiones consecutivas.
        var loc = await Ctx.Localidades.FindAsync([localidadId], ct)
                  ?? throw new InvalidOperationException($"La localidad {localidadId} no existe.");
        loc.CalendarioVersion++;

        Ctx.CalendarioCambios.Add(new CalendarioCambio
        {
            OrganizationId = loc.OrganizationId,
            LocalidadId = loc.Id,
            CalendarioVersion = loc.CalendarioVersion,
            Motivo = motivo,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            OcurridoEn = clock.GetUtcNow().UtcDateTime,
        });
        return loc.CalendarioVersion;
    }

    public async Task<IReadOnlyDictionary<Guid, long>> RegistrarSuscriptoresAsync(
        Guid calendarioFeriadosId, DateOnly? fechaDesde = null, DateOnly? fechaHasta = null, CancellationToken ct = default)
    {
        var localidades = await Ctx.LocalidadCalendariosFeriados.AsNoTracking()
            .Where(s => s.CalendarioFeriadosId == calendarioFeriadosId && s.Localidad!.DeletedAt == null)
            .Select(s => s.LocalidadId)
            .Distinct()
            .ToListAsync(ct);

        var resultado = new Dictionary<Guid, long>(localidades.Count);
        foreach (var id in localidades)
            resultado[id] = await RegistrarAsync(id, MotivoCambioCalendario.Feriado, fechaDesde, fechaHasta, ct);
        return resultado;
    }

    public async Task<CambiosPagina> ObtenerCambiosAsync(int organizationId, long cursor, int limite = 200, CancellationToken ct = default)
    {
        limite = Math.Clamp(limite, 1, LimiteMaximo);
        var org = Ctx.CalendarioCambios.AsNoTracking().Where(c => c.OrganizationId == organizationId);

        // Un cursor cuya fila ya fue purgada (hay filas más nuevas pero el cursor quedó antes de la más antigua que se conserva)
        // pudo perder cambios: el consumidor debe resincronizar. Cursor 0 = desde el principio.
        if (cursor > 0 && !await org.AnyAsync(c => c.Id == cursor, ct))
        {
            var minimo = await org.Select(c => (long?)c.Id).MinAsync(ct);
            if (minimo is not null && minimo.Value > cursor)
                return new CambiosPagina([], CursorExpirado: true);
        }

        // Se pide una fila de más para saber si queda algo después de la página (§11.3.7 hayMas).
        var cambios = await org.Where(c => c.Id > cursor).OrderBy(c => c.Id).Take(limite + 1).ToListAsync(ct);
        var hayMas = cambios.Count > limite;
        return new CambiosPagina(hayMas ? cambios.Take(limite).ToList() : cambios, CursorExpirado: false, HayMas: hayMas);
    }

    public async Task<int> PurgarExpiradosAsync(CancellationToken ct = default)
    {
        var limite = clock.GetUtcNow().UtcDateTime.AddDays(-Math.Max(1, settings.Value.RetencionCambiosDias));
        return await Ctx.CalendarioCambios.Where(c => c.OcurridoEn < limite).ExecuteDeleteAsync(ct);
    }
}
