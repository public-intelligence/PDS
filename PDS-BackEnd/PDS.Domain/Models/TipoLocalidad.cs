namespace PDS.Domain.Models;

/// <summary>
/// Catálogo de clasificación: tipo de localidad (sede central, oficina regional, almacén, etc.).
/// Dato de referencia compartido (no se asocia a una organización). RF-LO-01.
/// </summary>
public class TipoLocalidad : ISoftDeletable
{
    public Guid Id { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }

    /// <summary>Color sugerido para los marcadores del mapa (RF-LO-16).</summary>
    public string? Color { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<Localidad> Localidades { get; } = [];
}
