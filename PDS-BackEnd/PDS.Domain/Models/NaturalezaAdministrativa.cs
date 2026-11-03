namespace PDS.Domain.Models;

/// <summary>
/// Catálogo de clasificación: naturaleza administrativa de la localidad
/// (sede administrativa, órgano desconcentrado, punto de atención ciudadana, etc.). RF-LO-01.
/// </summary>
public class NaturalezaAdministrativa : ISoftDeletable
{
    public Guid Id { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<Localidad> Localidades { get; } = [];
}
