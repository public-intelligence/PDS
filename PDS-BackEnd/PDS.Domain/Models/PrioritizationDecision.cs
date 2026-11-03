using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PDS.Domain.Models;

/// <summary>
/// Decisión de priorización/ajuste append-only de un requerimiento (M7).
/// Registra reducciones, devoluciones, reprogramaciones y aprobaciones con su motivo.
/// </summary>
public class PrioritizationDecision
{
    public Guid Id { get; set; }
    public Guid RequirementId { get; set; }

    [MaxLength(30)]
    public required string RequirementCode { get; set; }

    public Guid? MasterProductId { get; set; }

    /// <summary>Type: Reduction | Return | Reschedule | Prioritization</summary>
    [MaxLength(30)]
    public required string Type { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal RequestedAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ApprovedAmount { get; set; }

    [MaxLength(2000)]
    public required string Reason { get; set; }

    public Guid DecidedBy { get; set; }
    [MaxLength(250)]
    public string? DecidedByName { get; set; }

    public Guid DecidingAreaId { get; set; }

    public DateTime Date { get; set; }

    /// <summary>Consolidator | Authority</summary>
    [MaxLength(20)]
    public required string Level { get; set; }

    public virtual Requirement? Requirement { get; set; }
}
