using System.Text.Json.Serialization;

namespace PDS.Domain.DTOs.Calendario;

/// <summary>Estado de una versión de jornada respecto de «hoy» (fecha local de la localidad).</summary>
public enum EstadoJornada
{
    /// <summary>Ya fue vigente y fue reemplazada por otra versión.</summary>
    Pasada,

    /// <summary>Es la versión en vigor hoy (la de mayor <c>vigenteDesde</c> ≤ hoy).</summary>
    Vigente,

    /// <summary>Empieza después de hoy: todavía se puede eliminar.</summary>
    Programada,
}

/// <summary>
/// Versión de la jornada semanal (RF-LO-24, RF-LO-25). <see cref="Dias"/> trae siempre los siete días (<c>mon</c>…<c>sun</c>);
/// un día cerrado tiene la lista vacía.
/// </summary>
public class JornadaVersionDto
{
    public Guid Id { get; set; }
    public DateOnly VigenteDesde { get; set; }

    /// <summary>Último día de vigencia (el día anterior a la siguiente versión); nulo si es la última.</summary>
    public DateOnly? VigenteHasta { get; set; }
    public EstadoJornada Estado { get; set; }
    public Dictionary<string, List<IntervaloDto>> Dias { get; set; } = new();
    public string? Motivo { get; set; }
    public string? CreadoPor { get; set; }
    public DateTime CreadoEn { get; set; }
}

/// <summary>Suscripción de la localidad a un calendario de feriados (RF-LO-30).</summary>
public class SuscripcionDto
{
    public Guid Id { get; set; }
    public Guid CalendarioFeriadosId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public DateOnly? VigenteDesde { get; set; }
    public DateOnly? VigenteHasta { get; set; }
}

/// <summary>Definición del calendario laboral de una localidad: zona horaria, jornadas versionadas y suscripciones (UI-05).</summary>
public class LocalidadCalendarioDto
{
    public Guid LocalidadId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string NombreOficial { get; set; } = string.Empty;
    public string ZonaHoraria { get; set; } = string.Empty;
    public string TzdbVersion { get; set; } = string.Empty;
    public long CalendarioVersion { get; set; }
    public DateOnly? FechaApertura { get; set; }
    public bool EsLocalidadCalendarioPredeterminada { get; set; }

    /// <summary>«Hoy» en la zona horaria de la localidad (RF-LO-23).</summary>
    public DateOnly HoyLocal { get; set; }
    public int VentanaRetroactivaDias { get; set; }

    /// <summary>True si quien consulta puede mantener el calendario de la localidad (administrador o gestor, RF-LO-38).</summary>
    public bool PuedeGestionar { get; set; }
    public IReadOnlyList<JornadaVersionDto> Jornadas { get; set; } = [];
    public IReadOnlyList<SuscripcionDto> Suscripciones { get; set; } = [];

    [JsonIgnore]
    public string? ETag { get; set; }
}

/// <summary>Cuerpo de <c>POST …/calendario/jornadas</c> (§11.3.2).</summary>
public class CrearJornadaDto
{
    public DateOnly? VigenteDesde { get; set; }

    /// <summary>Opcional; obligatorio cuando un administrador retrodata la versión (RF-LO-25).</summary>
    public string? Motivo { get; set; }

    /// <summary>Intervalos por día (<c>mon</c>…<c>sun</c>); un día ausente o con lista vacía queda cerrado.</summary>
    public Dictionary<string, List<IntervaloDto>>? Dias { get; set; }
}

/// <summary>Respuesta 201 de <c>POST …/calendario/jornadas</c>.</summary>
public class JornadaCreadaDto
{
    public Guid Id { get; set; }
    public DateOnly VigenteDesde { get; set; }
    public Dictionary<string, List<IntervaloDto>> Dias { get; set; } = new();
    public long CalendarioVersion { get; set; }
}

/// <summary>Elemento del cuerpo de <c>PUT …/calendario/suscripciones</c> (RF-LO-30): un calendario con vigencia opcional.</summary>
public class SuscripcionInputDto
{
    public Guid? CalendarioFeriadosId { get; set; }

    /// <summary>Nulo = sin límite inferior.</summary>
    public DateOnly? VigenteDesde { get; set; }

    /// <summary>Nulo = sin límite superior.</summary>
    public DateOnly? VigenteHasta { get; set; }
}

/// <summary>Respuesta de <c>PUT …/calendario/suscripciones</c>: el conjunto resultante y la versión del calendario tras el cambio.</summary>
public class SuscripcionesActualizadasDto
{
    public IReadOnlyList<SuscripcionDto> Suscripciones { get; set; } = [];
    public long CalendarioVersion { get; set; }
}
