namespace PDS.Domain.Models;

/// <summary>
/// Catálogo territorial: región de planificación. Dato de referencia nacional.
/// </summary>
public class Region
{
    public Guid Id { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public Guid PaisId { get; set; }

    public Pais? Pais { get; set; }
    public ICollection<Provincia> Provincias { get; } = [];
}
