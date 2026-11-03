using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace PDS.Domain.Models;

/// <summary>
/// Unidad de medida de un producto POA (M5). Programación trimestral con Q1-Q4 como columnas fijas.
/// </summary>
public class PoaMeasureUnit
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }

    [MaxLength(500)]
    public required string Name { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TargetValue { get; set; }

    /// <summary>Absolute | Percentage</summary>
    [MaxLength(20)]
    public required string TargetType { get; set; }

    [MaxLength(1000)]
    public string? VerificationMeans { get; set; }

    public Guid? ResponsibleAreaId { get; set; }
    [MaxLength(250)]
    public string? ResponsibleAreaName { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Q1 { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Q2 { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Q3 { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Q4 { get; set; }

    public virtual PoaProduct? Product { get; set; }
}
