namespace PDS.Domain.Models;

/// <summary>
/// Responsable asignado a una localidad (RF-LO-14, RF-LO-15). N:M con <see cref="Responsable"/>.
/// </summary>
public class LocalidadResponsable : ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid LocalidadId { get; set; }
    public Guid ResponsableId { get; set; }
    public string? Rol { get; set; }
    public bool Principal { get; set; }
    public DateOnly? FechaAsignacion { get; set; }

    /// <summary>RF-LO-38: este responsable puede mantener el calendario de la localidad (solo lo fija un administrador de calendario).</summary>
    public bool GestionaCalendario { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Localidad? Localidad { get; set; }
    public Responsable? Responsable { get; set; }
}
