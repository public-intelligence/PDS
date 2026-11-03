using System.ComponentModel.DataAnnotations;
namespace PDS.Domain.Models;

/// <summary>
/// Tarea de un producto POA (M5). El código se genera en servidor ("TR-01", "TR-02", etc.).
/// MonthsMask es un bitmask donde bit 0 = enero, bit 11 = diciembre.
/// </summary>
public class PoaTask
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }

    [MaxLength(20)]
    public required string Code { get; set; }

    [MaxLength(500)]
    public required string Name { get; set; }

    public bool IsContinuous { get; set; }

    /// <summary>Máscara de bits de meses (bit 0 = enero, bit 11 = diciembre).</summary>
    public short MonthsMask { get; set; }

    public virtual PoaProduct? Product { get; set; }
}
