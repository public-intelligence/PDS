namespace PDS.Domain.Models;

/// <summary>
/// Catálogo de responsables (personas) asignables a una localidad (RF-LO-14, RF-LO-15). Por organización.
/// </summary>
public class Responsable : ISoftDeletable
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }
    public required string Nombre { get; set; }
    public string? Cargo { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }

    /// <summary>RF-LO-37: <c>sub</c> del usuario GobID vinculado. Único por organización entre responsables no eliminados.</summary>
    public Guid? GobidUserId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<LocalidadResponsable> LocalidadResponsables { get; } = [];
}
