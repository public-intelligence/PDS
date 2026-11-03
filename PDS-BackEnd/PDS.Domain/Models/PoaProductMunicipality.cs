using System.ComponentModel.DataAnnotations;
namespace PDS.Domain.Models;

/// <summary>
/// Municipio vinculado a un producto POA (M5). Snapshot del nombre para independencia del catálogo.
/// </summary>
public class PoaProductMunicipality
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid MunicipioId { get; set; }

    [MaxLength(250)]
    public required string MunicipioName { get; set; }

    public virtual PoaProduct? Product { get; set; }
}
