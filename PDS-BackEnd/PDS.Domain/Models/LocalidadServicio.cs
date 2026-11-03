namespace PDS.Domain.Models;

/// <summary>
/// Servicio habilitado en una localidad (RF-LO-14, RF-LO-15). N:M con <see cref="Servicio"/>.
/// </summary>
public class LocalidadServicio : ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid LocalidadId { get; set; }
    public Guid ServicioId { get; set; }
    public bool Habilitado { get; set; } = true;
    public DateOnly? FechaHabilitacion { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Localidad? Localidad { get; set; }
    public Servicio? Servicio { get; set; }
}
