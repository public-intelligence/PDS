namespace PDS.Domain.Models;

/// <summary>
/// Catálogo de ciclo de vida: estado de la localidad (activa, inactiva, suspendida temporalmente,
/// en apertura, en cierre). La lógica de negocio referencia los estados por <see cref="Codigo"/>
/// (estable), nunca por Id o nombre. RF-LO-09, RF-LO-10.
/// </summary>
public class EstadoLocalidad : ISoftDeletable
{
    public Guid Id { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }

    /// <summary>Color sugerido para los marcadores del mapa (RF-LO-16).</summary>
    public string? Color { get; set; }

    /// <summary>Indica si el estado es terminal (p. ej. inactiva / en cierre).</summary>
    public bool EsTerminal { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<Localidad> Localidades { get; } = [];
}
