namespace PDS.Domain.Models;

/// <summary>
/// Catálogo territorial: distrito municipal. Dato de referencia nacional.
/// </summary>
public class DistritoMunicipal
{
    public Guid Id { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public Guid MunicipioId { get; set; }

    public Municipio? Municipio { get; set; }
}
