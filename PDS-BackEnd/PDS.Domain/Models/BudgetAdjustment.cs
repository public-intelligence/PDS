using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PDS.Domain.Models;

/// <summary>
/// Ajuste de Techo Presupuestario (Incremento o Reducción con justificación y auditoría).
/// </summary>
public class BudgetAdjustment
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    public Guid BudgetCeilingId { get; set; }

    /// <summary>Increase | Reduction</summary>
    [MaxLength(20)]
    public required string AdjustmentType { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(2000)]
    public required string Reason { get; set; }

    [MaxLength(1000)]
    public string? DocumentUrl { get; set; }

    public Guid CreatedBy { get; set; }

    [MaxLength(250)]
    public string? CreatedByName { get; set; }

    public DateTime CreatedAt { get; set; }

    // ───────────── Navegación ─────────────

    public virtual BudgetCeiling? BudgetCeiling { get; set; }
}
