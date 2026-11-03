using System.Text.Json.Serialization;

namespace PDS.Domain.DTOs.Calendario;

// SPEC-LOC-CAL-01 RF-LO-35 / §10.3 / §11.3.6: aritmética de tiempo laborable (convención v1).

/// <summary>Un cálculo de vencimiento: <c>unidad</c> vale <c>min</c>, <c>hours</c>, <c>bizdays</c> o <c>caldays</c>.</summary>
public class CalcularVencimientoDto
{
    public Guid? LocalidadId { get; set; }

    /// <summary>Instante de inicio del plazo (ISO 8601 con <c>Z</c> u offset explícito).</summary>
    public DateTimeOffset? Inicio { get; set; }
    public int? Cantidad { get; set; }
    public string? Unidad { get; set; }
}

/// <summary>Cuerpo de <c>calcular-vencimiento</c>: un cálculo suelto o, con <see cref="Items"/>, un lote (máximo 100).</summary>
public class CalcularVencimientoRequestDto : CalcularVencimientoDto
{
    public List<CalcularVencimientoDto>? Items { get; set; }
}

public class VencimientoDto
{
    /// <summary>Instante de vencimiento en UTC (ISO 8601 con <c>Z</c>).</summary>
    public DateTime Vencimiento { get; set; }

    /// <summary>El mismo instante en hora local de la localidad (sin offset).</summary>
    public DateTime VencimientoLocal { get; set; }
    public string ZonaHoraria { get; set; } = string.Empty;

    /// <summary>Versión del calendario con la que se calculó; es la misma que publica el feed de cambios.</summary>
    public long CalendarioVersion { get; set; }
    public string Convencion { get; set; } = "v1";
}

/// <summary>Error de un ítem de lote: mismo estado y clave que tendría la respuesta del cálculo suelto.</summary>
public sealed record ErrorItemDto(int Estado, string Clave, string Mensaje);

/// <summary>Resultado de un ítem de lote: o los campos del cálculo o <see cref="Error"/>, nunca ambos.</summary>
public class VencimientoItemDto
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? Vencimiento { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? VencimientoLocal { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ZonaHoraria { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? CalendarioVersion { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Convencion { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ErrorItemDto? Error { get; set; }
}

public class VencimientoLoteDto
{
    public List<VencimientoItemDto> Items { get; set; } = new();
}

/// <summary>Un cálculo de tiempo laborable entre dos instantes.</summary>
public class TiempoLaborableDto
{
    public Guid? LocalidadId { get; set; }
    public DateTimeOffset? Desde { get; set; }
    public DateTimeOffset? Hasta { get; set; }
}

public class TiempoLaborableRequestDto : TiempoLaborableDto
{
    public List<TiempoLaborableDto>? Items { get; set; }
}

public class MinutosLaborablesDto
{
    public long MinutosLaborables { get; set; }
}

public class MinutosLaborablesItemDto
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? MinutosLaborables { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ErrorItemDto? Error { get; set; }
}

public class MinutosLaborablesLoteDto
{
    public List<MinutosLaborablesItemDto> Items { get; set; } = new();
}
