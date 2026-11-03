namespace PDS.Domain.DTOs.CatalogoLocalidad;

// ── Tipo de localidad ──
public record TipoLocalidadDto
{
    public Guid Id { get; init; }
    public required string Codigo { get; init; }
    public required string Nombre { get; init; }
    public string? Descripcion { get; init; }
    public string? Color { get; init; }
}

public record CreateTipoLocalidadDto
{
    public required string Codigo { get; init; }
    public required string Nombre { get; init; }
    public string? Descripcion { get; init; }
    public string? Color { get; init; }
}

public record UpdateTipoLocalidadDto
{
    public required string Codigo { get; init; }
    public required string Nombre { get; init; }
    public string? Descripcion { get; init; }
    public string? Color { get; init; }
}

// ── Naturaleza administrativa ──
public record NaturalezaAdministrativaDto
{
    public Guid Id { get; init; }
    public required string Codigo { get; init; }
    public required string Nombre { get; init; }
    public string? Descripcion { get; init; }
}

public record CreateNaturalezaAdministrativaDto
{
    public required string Codigo { get; init; }
    public required string Nombre { get; init; }
    public string? Descripcion { get; init; }
}

public record UpdateNaturalezaAdministrativaDto
{
    public required string Codigo { get; init; }
    public required string Nombre { get; init; }
    public string? Descripcion { get; init; }
}

// ── Estado de localidad ──
public record EstadoLocalidadDto
{
    public Guid Id { get; init; }
    public required string Codigo { get; init; }
    public required string Nombre { get; init; }
    public string? Descripcion { get; init; }
    public string? Color { get; init; }
    public bool EsTerminal { get; init; }
}

public record CreateEstadoLocalidadDto
{
    public required string Codigo { get; init; }
    public required string Nombre { get; init; }
    public string? Descripcion { get; init; }
    public string? Color { get; init; }
    public bool EsTerminal { get; init; }
}

public record UpdateEstadoLocalidadDto
{
    public required string Codigo { get; init; }
    public required string Nombre { get; init; }
    public string? Descripcion { get; init; }
    public string? Color { get; init; }
    public bool EsTerminal { get; init; }
}

/// <summary>Zona horaria IANA del catálogo (B05 / RF-LO-21).</summary>
public record ZonaHorariaDto
{
    public required string Id { get; init; }
    public required string PaisCodigo { get; init; }
    public string? Comentario { get; init; }

    /// <summary>Desplazamiento UTC vigente ahora, con formato <c>+hh:mm</c> / <c>-hh:mm</c> (RF-LO-21).</summary>
    public required string DesplazamientoActual { get; init; }

    /// <summary>Indica si la zona está en horario de verano en este momento (RF-LO-21).</summary>
    public bool EsHorarioVeranoActual { get; init; }
}

// ── Función de localidad (RF-LO-45, B30) ──
public record FuncionLocalidadDto
{
    public Guid Id { get; init; }
    public required string Codigo { get; init; }
    public required string Nombre { get; init; }

    /// <summary>Se serializa como texto: <c>Diplomatica</c>, <c>Consular</c> o <c>Multilateral</c>.</summary>
    public PDS.Domain.Enums.CategoriaFuncionLocalidad Categoria { get; init; }

    public string? Descripcion { get; init; }
}

/// <summary>
/// Cuerpo de POST y PUT. Todo es opcional a propósito: la validación (400 con la clave del campo) vive en el servicio,
/// así un campo faltante o una categoría desconocida devuelven el <c>ErrorDto</c> habitual y no el 400 genérico del binder.
/// </summary>
public record FuncionLocalidadUpsertDto
{
    public string? Codigo { get; init; }
    public string? Nombre { get; init; }
    public string? Categoria { get; init; }
    public string? Descripcion { get; init; }
}
