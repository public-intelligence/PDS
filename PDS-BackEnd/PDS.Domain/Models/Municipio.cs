namespace PDS.Domain.Models;

/// <summary>
/// Catálogo territorial: municipio. Dato de referencia nacional.
/// </summary>
public class Municipio
{
    public Guid Id { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public Guid ProvinciaId { get; set; }

    public Provincia? Provincia { get; set; }
    public ICollection<DistritoMunicipal> DistritosMunicipales { get; } = [];
}
