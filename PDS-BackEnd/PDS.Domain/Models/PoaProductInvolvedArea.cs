using System.ComponentModel.DataAnnotations;
namespace PDS.Domain.Models;

/// <summary>
/// Área involucrada en un producto POA (M5). Snapshot del nombre.
/// </summary>
public class PoaProductInvolvedArea
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid AreaId { get; set; }

    [MaxLength(250)]
    public required string AreaName { get; set; }

    public virtual PoaProduct? Product { get; set; }
}
