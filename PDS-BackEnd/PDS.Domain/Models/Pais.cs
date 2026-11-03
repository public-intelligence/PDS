namespace PDS.Domain.Models;

/// <summary>
/// Catálogo territorial: país. Dato de referencia nacional (no se asocia a una organización).
/// </summary>
public class Pais
{
    public Guid Id { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }

    /// <summary>Código ISO 3166-1 alfa-3 (p. ej. DOM). <see cref="Codigo"/> conserva el alfa-2.</summary>
    public string? CodigoIso3 { get; set; }

    public ICollection<Region> Regiones { get; } = [];
    public ICollection<Provincia> Provincias { get; } = [];
}
