using System.Text.Json.Serialization;

namespace PDS.Domain.DTOs.Responsable;

public record ResponsableDto
{
    public Guid Id { get; init; }
    public required string Nombre { get; init; }
    public string? Cargo { get; init; }
    public string? Email { get; init; }
    public string? Telefono { get; init; }

    /// <summary>
    /// RF-LO-37: <c>sub</c> del usuario GobID vinculado. Solo se devuelve a quien tiene
    /// <c>localities:calendar:manage</c>; para los demás es <c>null</c>. El nombre JSON es exactamente <c>gobidUserId</c>.
    /// </summary>
    [JsonPropertyName("gobidUserId")]
    public Guid? GobidUserId { get; init; }
}

/// <summary>Campos comunes de alta y edición de un responsable.</summary>
public abstract record ResponsableWriteDto
{
    private string? _gobidUserId;

    public required string Nombre { get; init; }
    public string? Cargo { get; init; }
    public string? Email { get; init; }
    public string? Telefono { get; init; }

    /// <summary>
    /// RF-LO-37: GUID del usuario GobID como texto, para poder devolver 400 con la clave <c>gobidUserId</c> si es inválido.
    /// Ausente = no se toca; <c>null</c> o vacío = desvincular (solo administradores de calendario).
    /// </summary>
    [JsonPropertyName("gobidUserId")]
    public string? GobidUserId
    {
        get => _gobidUserId;
        init
        {
            _gobidUserId = value;
            GobidUserIdProvided = true;
        }
    }

    /// <summary>Verdadero si el cuerpo trajo la propiedad <c>gobidUserId</c> (incluso con <c>null</c>).</summary>
    [JsonIgnore]
    public bool GobidUserIdProvided { get; private init; }
}

public record CreateResponsableDto : ResponsableWriteDto;

public record UpdateResponsableDto : ResponsableWriteDto;
