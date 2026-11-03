using System.Text.Json.Serialization;
using PDS.Domain.Enums;

namespace PDS.Domain.DTOs.Calendario;

/// <summary>
/// Excepción del calendario de una localidad (RF-LO-31): cierre, jornada especial o día laborable adicional. Las de sistema
/// (<see cref="EsSistema"/>: suspensión y cierre definitivo) las genera el ciclo de vida de la localidad y no se editan por la API.
/// </summary>
public class ExcepcionCalendarioDto
{
    public Guid Id { get; set; }
    public DateOnly FechaInicio { get; set; }

    /// <summary>Nulo solo en excepciones de sistema abiertas (p. ej. suspensión sin fecha de fin).</summary>
    public DateOnly? FechaFin { get; set; }
    public TipoExcepcionCalendario Tipo { get; set; }
    public CategoriaExcepcionCalendario Categoria { get; set; }
    public OrigenExcepcionCalendario Origen { get; set; }

    /// <summary>Horario que se trabaja ese día; nulo en un cierre.</summary>
    public IReadOnlyList<IntervaloDto>? Intervalos { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? ReferenciaNormativa { get; set; }
    public Guid? InstrumentoLegalId { get; set; }
    public bool EsSistema { get; set; }
    public string? CreadoPor { get; set; }
    public DateTime CreadoEn { get; set; }
    public string? ModificadoPor { get; set; }
    public DateTime? ModificadoEn { get; set; }

    /// <summary>Versión del calendario tras el cambio; solo en las respuestas de escritura (§11.3.3).</summary>
    public long? CalendarioVersion { get; set; }

    [JsonIgnore]
    public string? ETag { get; set; }
}

/// <summary>Cuerpo de alta y edición de una excepción (§11.3.3).</summary>
public class UpsertExcepcionDto
{
    public DateOnly? FechaInicio { get; set; }

    /// <summary>Nulo = igual a <see cref="FechaInicio"/> (un solo día). Máximo 366 días de extensión.</summary>
    public DateOnly? FechaFin { get; set; }
    public TipoExcepcionCalendario? Tipo { get; set; }
    public CategoriaExcepcionCalendario? Categoria { get; set; }
    public OrigenExcepcionCalendario? Origen { get; set; }

    /// <summary>Prohibido para <c>Cierre</c>; obligatorio para <c>JornadaEspecial</c> y <c>DiaLaborableAdicional</c>.</summary>
    public List<IntervaloDto>? Intervalos { get; set; }
    public string? Motivo { get; set; }
    public string? ReferenciaNormativa { get; set; }
    public Guid? InstrumentoLegalId { get; set; }
}
