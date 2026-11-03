using System.Text.Json.Serialization;
using PDS.Domain.Enums;

namespace PDS.Domain.DTOs.Calendario;

// SPEC-LOC-CAL-01 §11.3.5 / §11.3.7 (lectura del calendario efectivo, estado y feed de cambios) y spec M2M de SDS (#2, #3).

/// <summary>Intervalo laboral en UTC (ISO 8601 con <c>Z</c>).</summary>
public sealed record IntervaloUtcDto(DateTime Inicio, DateTime Fin);

/// <summary>Por qué un día es como es: nombre del feriado o motivo de la excepción, con los identificadores de origen.</summary>
public class MotivoDiaDto
{
    /// <summary><c>Feriado</c>, el tipo de excepción (<c>Cierre</c>, <c>JornadaEspecial</c>…) o, en el ciclo de vida, <c>Suspension</c>/<c>CierreDefinitivo</c>/<c>Apertura</c>.</summary>
    public string Tipo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? CalendarioFeriadosId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ExcepcionId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReferenciaNormativa { get; set; }
}

public class DiaEfectivoDto
{
    public DateOnly Fecha { get; set; }

    /// <summary><c>mon</c>…<c>sun</c>.</summary>
    public string DiaSemana { get; set; } = string.Empty;
    public bool Laborable { get; set; }

    /// <summary>Intervalos en hora local de la localidad (<c>HH:mm</c>).</summary>
    public List<IntervaloDto> Intervalos { get; set; } = new();

    /// <summary>Los mismos intervalos en UTC: los consumidores deberían usar estos y no necesitan lógica de zona horaria.</summary>
    public List<IntervaloUtcDto> IntervalosUtc { get; set; } = new();
    public FuenteDiaCalendario Fuente { get; set; }
    public List<MotivoDiaDto> Motivos { get; set; } = new();
}

/// <summary>Calendario efectivo de una localidad en un rango de fechas locales (RF-LO-34).</summary>
public class CalendarioEfectivoDto
{
    public Guid LocalidadId { get; set; }
    public string ZonaHoraria { get; set; } = string.Empty;
    public string TzdbVersion { get; set; } = string.Empty;
    public long CalendarioVersion { get; set; }
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public List<DiaEfectivoDto> Dias { get; set; } = new();
}

/// <summary>Si la localidad trabaja en un instante, su intervalo actual y el próximo instante laborable (RF-LO-34 <c>estado</c>).</summary>
public class EstadoCalendarioDto
{
    public Guid LocalidadId { get; set; }
    public string ZonaHoraria { get; set; } = string.Empty;
    public string TzdbVersion { get; set; } = string.Empty;
    public long CalendarioVersion { get; set; }
    public DateTime Instante { get; set; }

    /// <summary>Fecha local de la localidad en <see cref="Instante"/>.</summary>
    public DateOnly FechaLocal { get; set; }
    public bool Laborando { get; set; }
    public IntervaloUtcDto? IntervaloActual { get; set; }
    public DateTime? ProximoInstanteLaborable { get; set; }
}

/// <summary>Una fila del feed de cambios del calendario (RF-LO-43, §11.3.7).</summary>
public class CalendarioCambioDto
{
    public long Cursor { get; set; }
    public Guid LocalidadId { get; set; }
    public long CalendarioVersion { get; set; }
    public MotivoCambioCalendario Motivo { get; set; }
    public DateOnly? FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public DateTime OcurridoEn { get; set; }
}

public class CalendarioCambiosDto
{
    public List<CalendarioCambioDto> Items { get; set; } = new();

    /// <summary>Cursor a usar en la siguiente consulta (el de la última fila, o el recibido si no hay nada nuevo).</summary>
    public long SiguienteCursor { get; set; }
    public bool HayMas { get; set; }
}

// ───────────────────────────── Spec M2M de SDS (#2 y #3) ─────────────────────────────

/// <summary>Un día de la semana del horario vigente (spec M2M #2): <c>weekday</c> 1 = lunes … 7 = domingo.</summary>
public class WorkingHoursDayDto
{
    public int Weekday { get; set; }

    /// <summary>Intervalos locales <c>["HH:mm","HH:mm"]</c>; un descanso de almuerzo son dos intervalos. Vacío = cerrado.</summary>
    public List<List<string>> Intervals { get; set; } = new();
}

/// <summary>Horario semanal vigente en una fecha (spec M2M #2).</summary>
public class WorkingHoursDto
{
    public Guid LocalityId { get; set; }
    public DateOnly ValidFrom { get; set; }

    /// <summary>Último día de vigencia (el día anterior a la siguiente versión); nulo si no tiene fin.</summary>
    public DateOnly? ValidTo { get; set; }

    /// <summary><c>CalendarioVersion</c> de la localidad, como texto, para cachear y revalidar.</summary>
    public string Version { get; set; } = string.Empty;
    public List<WorkingHoursDayDto> Days { get; set; } = new();
}

/// <summary>Cierre del calendario (spec M2M #3).</summary>
public class LocalityClosureDto
{
    public DateOnly Date { get; set; }

    /// <summary><c>full</c> o <c>partial</c>.</summary>
    public string Kind { get; set; } = "full";

    /// <summary>Solo en <c>partial</c>: ventana local cerrada <c>HH:mm</c>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? From { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? To { get; set; }

    /// <summary>Legible para el ciudadano (se muestra en la fecha deshabilitada).</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary><c>host</c>, <c>home</c> o <c>mission</c>.</summary>
    public string Source { get; set; } = string.Empty;
}

public class LocalityClosuresDto
{
    public Guid LocalityId { get; set; }
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public string Version { get; set; } = string.Empty;
    public List<LocalityClosureDto> Closures { get; set; } = new();
}
