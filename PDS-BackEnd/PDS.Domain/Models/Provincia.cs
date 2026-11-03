namespace PDS.Domain.Models;

/// <summary>
/// Catálogo territorial: provincia (incluye el Distrito Nacional). Dato de referencia nacional.
/// </summary>
public class Provincia
{
    public Guid Id { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public Guid PaisId { get; set; }
    public Guid? RegionId { get; set; }

    public Pais? Pais { get; set; }
    public Region? Region { get; set; }
    public ICollection<Municipio> Municipios { get; } = [];
}
