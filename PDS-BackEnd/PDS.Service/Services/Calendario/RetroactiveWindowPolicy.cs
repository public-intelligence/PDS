using Microsoft.Extensions.Options;
using NodaTime;
using PDS.Service.Settings;

namespace PDS.Service.Services.Calendario;

/// <summary>Resultado de evaluar un cambio contra la ventana retroactiva (RF-LO-33).</summary>
public enum RetroactivoVeredicto
{
    /// <summary>Ninguna fecha afectada es anterior al día local de hoy.</summary>
    Permitido,

    /// <summary>Administrador con cambios sobre fechas pasadas y con motivo: permitido, pero queda marcado como retroactivo.</summary>
    PermitidoRetroactivo,

    /// <summary>Administrador que cambia fechas pasadas sin motivo (400, clave <c>motivo</c>).</summary>
    RequiereMotivo,

    /// <summary>No administrador con fechas anteriores a hoy − N (403).</summary>
    FueraDeVentana,
}

public readonly record struct RetroactivoEvaluacion(RetroactivoVeredicto Veredicto, DateOnly Hoy, DateOnly Limite)
{
    public bool Retroactivo => Veredicto is RetroactivoVeredicto.PermitidoRetroactivo or RetroactivoVeredicto.RequiereMotivo;
}

/// <summary>
/// RF-LO-33 / RF-LO-23: ventana retroactiva de los gestores. «Hoy» es la fecha LOCAL de la localidad (su zona horaria IANA) tomada
/// de un <see cref="TimeProvider"/> inyectado; sin zona (calendarios sin localidad gestora) se usa la fecha UTC.
/// </summary>
public interface IRetroactiveWindowPolicy
{
    /// <summary>Días hacia atrás permitidos a un gestor no administrador (<c>Calendario:VentanaRetroactivaDias</c>).</summary>
    int VentanaDias { get; }

    /// <summary>Fecha local de hoy en la zona dada (UTC si es nula o desconocida).</summary>
    DateOnly HoyLocal(string? zonaHoraria);

    /// <summary>
    /// Evalúa los cambios sobre <paramref name="fechasAfectadas"/>. Administrador: sin ventana, pero exige <paramref name="motivo"/> si
    /// alguna fecha es anterior a hoy. No administrador: todas las fechas deben ser ≥ hoy − N.
    /// </summary>
    RetroactivoEvaluacion Evaluar(bool esAdmin, string? zonaHoraria, IEnumerable<DateOnly> fechasAfectadas, string? motivo);
}

public sealed class RetroactiveWindowPolicy(TimeProvider clock, IOptions<CalendarioSettings> settings) : IRetroactiveWindowPolicy
{
    public int VentanaDias => Math.Max(0, settings.Value.VentanaRetroactivaDias);

    public DateOnly HoyLocal(string? zonaHoraria)
    {
        var zona = (zonaHoraria is null ? null : DateTimeZoneProviders.Tzdb.GetZoneOrNull(zonaHoraria)) ?? DateTimeZone.Utc;
        var fecha = Instant.FromDateTimeOffset(clock.GetUtcNow()).InZone(zona).Date;
        return new DateOnly(fecha.Year, fecha.Month, fecha.Day);
    }

    public RetroactivoEvaluacion Evaluar(bool esAdmin, string? zonaHoraria, IEnumerable<DateOnly> fechasAfectadas, string? motivo)
    {
        var hoy = HoyLocal(zonaHoraria);
        var limite = hoy.AddDays(-VentanaDias);
        var fechas = fechasAfectadas.ToList();

        if (esAdmin)
        {
            var retro = fechas.Any(f => f < hoy);
            if (!retro) return new(RetroactivoVeredicto.Permitido, hoy, limite);
            return string.IsNullOrWhiteSpace(motivo)
                ? new(RetroactivoVeredicto.RequiereMotivo, hoy, limite)
                : new(RetroactivoVeredicto.PermitidoRetroactivo, hoy, limite);
        }

        return fechas.Any(f => f < limite)
            ? new(RetroactivoVeredicto.FueraDeVentana, hoy, limite)
            : new(RetroactivoVeredicto.Permitido, hoy, limite);
    }
}
