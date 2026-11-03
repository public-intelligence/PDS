using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PDS.Domain.Models;

/// <summary>
/// Requerimiento (M7). Nace obligatoriamente de una tarea del POA (PoaTaskId).
/// El área consolidadora la determina el artículo de la Maestra (MasterProduct), no el árbol UNSPSC.
/// Secuencial por ciclo ("RQ-{Year}-{NNN}").
/// </summary>
public class Requirement
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    /// <summary>Código secuencial por ciclo (ej. "RQ-2027-001"). Generado en servidor.</summary>
    [MaxLength(30)]
    public required string Code { get; set; }

    /// <summary>Ciclo al que pertenece (denormalizado para consultas canónicas).</summary>
    public Guid CycleId { get; set; }

    /// <summary>Plan Anual del área origen.</summary>
    public Guid PlanId { get; set; }

    /// <summary>Producto POA de donde nace.</summary>
    public Guid PoaProductId { get; set; }

    /// <summary>Tarea POA obligatoria de origen.</summary>
    public Guid PoaTaskId { get; set; }

    /// <summary>Área que genera la necesidad (snapshot).</summary>
    public Guid OriginAreaId { get; set; }
    [MaxLength(250)]
    public required string OriginAreaName { get; set; }

    /// <summary>Área que atiende/consolida la compra/servicio (snapshot resuelto al crear/editar).</summary>
    public Guid ConsolidatorAreaId { get; set; }
    [MaxLength(250)]
    public required string ConsolidatorAreaName { get; set; }

    /// <summary>Texto explicativo del ruteo resuelto por la matriz.</summary>
    [MaxLength(500)]
    public string? AppliedRule { get; set; }

    // ───────────── Vínculo a Maestra de Artículos (M8/M9/M10) ─────────────
    public Guid MasterProductId { get; set; }

    [MaxLength(30)]
    public string? MasterProductCode { get; set; }

    [MaxLength(300)]
    public required string MasterProductName { get; set; }

    // ───────────── Snapshots Regulatorios (al crear/editar) ─────────────
    [MaxLength(20)]
    public string? UnspscCode { get; set; }

    [MaxLength(500)]
    public string? UnspscName { get; set; }

    [MaxLength(20)]
    public string? ExpenseObjectCode { get; set; }

    [MaxLength(300)]
    public string? ExpenseObjectName { get; set; }

    [MaxLength(30)]
    public string? AccountingAccountCode { get; set; }

    [MaxLength(300)]
    public string? AccountingAccountName { get; set; }

    [MaxLength(20)]
    public string? ItemType { get; set; }

    [MaxLength(1000)]
    public required string Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; }

    [MaxLength(50)]
    public required string UnitOfMeasure { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal EstimatedUnitPrice { get; set; }

    public bool PriceConfirmed { get; set; }

    /// <summary>Q1 | Q2 | Q3 | Q4</summary>
    [MaxLength(10)]
    public required string Quarter { get; set; }

    /// <summary>Fecha requerida (para necesidades sin costo directo).</summary>
    public DateTime? RequiredDate { get; set; }

    [MaxLength(100)]
    public string? FundingSource { get; set; }

    /// <summary>High | Medium | Low</summary>
    [MaxLength(20)]
    public required string Priority { get; set; }

    /// <summary>New | InReview | Approved | Reduced | Returned | Merged | Rescheduled</summary>
    [MaxLength(30)]
    public required string Status { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ApprovedAmount { get; set; }

    /// <summary>Si fue fusionado, apunta al nuevo requerimiento consolidado.</summary>
    public Guid? MergedIntoId { get; set; }

    public Guid CreatedBy { get; set; }
    [MaxLength(250)]
    public string? CreatedByName { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual PlanningCycle? Cycle { get; set; }
    public virtual AnnualPlan? Plan { get; set; }
    public virtual PoaProduct? PoaProduct { get; set; }
    public virtual PoaTask? PoaTask { get; set; }
    public virtual MasterProduct? MasterProduct { get; set; }
    public virtual Requirement? MergedInto { get; set; }

    public virtual ICollection<RequirementComment> Comments { get; } = [];
    public virtual ICollection<PrioritizationDecision> Decisions { get; } = [];
}
