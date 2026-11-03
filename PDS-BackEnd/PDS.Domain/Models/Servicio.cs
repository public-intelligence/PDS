namespace PDS.Domain.Models;

/// <summary>
/// Catálogo de servicios habilitables en una localidad (RF-LO-14, RF-LO-15). Por organización.
/// </summary>
public class Servicio : ISoftDeletable
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }
    public string? Codigo { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<LocalidadServicio> LocalidadServicios { get; } = [];
}
