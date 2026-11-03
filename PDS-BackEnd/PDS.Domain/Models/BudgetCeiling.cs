using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PDS.Domain.Models;

/// <summary>
/// Techo Presupuestario Institucional (Módulo Techos Presupuestarios - M10):
/// Gobernado por el Auxiliar del Objeto del Gasto (DIGEPRES - nivel imputable IsDetail == true),
/// Año Fiscal, Fuente de Financiamiento y Organización.
/// </summary>
public class BudgetCeiling : ISoftDeletable
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    public int FiscalYear { get; set; }

    /// <summary>FK opcional al Ciclo de Planificación (M4). null = Configuración Maestra Base; valor = Techo activo instanciado por ciclo.</summary>
    public Guid? CycleId { get; set; }

    /// <summary>FK al Objeto del Gasto (Clasificador DIGEPRES, nivel auxiliar/imputable).</summary>
    public Guid ExpenseObjectId { get; set; }

    [MaxLength(20)]
    public required string ExpenseObjectCode { get; set; }

    [MaxLength(300)]
    public required string ExpenseObjectName { get; set; }

    /// <summary>Fuente de financiamiento (ej: "Fuente 10 - Recursos del Tesoro", "10", "Fondo General").</summary>
    [MaxLength(100)]
    public required string FundingSource { get; set; }

    /// <summary>Programa o estructura programática opcional.</summary>
    [MaxLength(100)]
    public string? BudgetProgram { get; set; }

    // ───────────── Ciclo Financiero (Decimal 18,2) ─────────────

    [Column(TypeName = "decimal(18,2)")]
    public decimal InitialAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ReservedAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PlannedAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CommittedAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ExecutedAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AvailableAmount { get; set; }

    /// <summary>
    /// Umbral porcentual de advertencia (amarillo). Default: 10.00%.
    /// Si Available / Current &lt; (ThresholdPercentage / 100), pasa a estado Yellow.
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal ThresholdPercentage { get; set; } = 10.00m;

    /// <summary>
    /// Comportamiento al exceder disponible: Block | Warning | AllowWithJustification
    /// </summary>
    [MaxLength(30)]
    public string ValidationBehavior { get; set; } = "Block";

    /// <summary>
    /// Estado del techo: Green (Normal) | Yellow (Cerca del techo) | Red (Excedido)
    /// </summary>
    [MaxLength(20)]
    public required string Status { get; set; } = "Green";

    // ───────────── Auditoría y soft delete ─────────────

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // ───────────── Navegación ─────────────

    public virtual PlanningCycle? Cycle { get; set; }
    public virtual ExpenseObject? ExpenseObject { get; set; }
    public virtual ICollection<BudgetAdjustment> Adjustments { get; } = [];
}
