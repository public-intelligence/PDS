using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Artículo (Maestra de Productos — M8/M9): entidad maestra que articula el catálogo UNSPSC,
/// Objeto del Gasto (DIGEPRES), Plan de Cuentas (DIGECOB) y Área Consolidadora.
/// </summary>
public class MasterProduct : ISoftDeletable
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    /// <summary>UUIDv5 del id_articulo del archivo institucional (clave de idempotencia de importación).</summary>
    public Guid? SourceId { get; set; }

    /// <summary>Código secuencial único por organización (ej: "ART-000001").</summary>
    [MaxLength(30)]
    public required string Code { get; set; }

    [MaxLength(300)]
    public required string Name { get; set; }

    [MaxLength(500)]
    public string? OriginalDescription { get; set; }

    [MaxLength(300)]
    public string? EnglishName { get; set; }

    [MaxLength(2000)]
    public string? Definition { get; set; }

    /// <summary>
    /// Texto normalizado concatenado para el autocomplete (nombre + descripción +
    /// sinónimos + definición). Los sinónimos de la fuente llegan a ~2,400 caracteres
    /// por artículo, así que el campo se dimensiona holgado y el importador trunca.
    /// </summary>
    [MaxLength(4000)]
    public required string SearchText { get; set; }

    // ───────────── Clasificación ─────────────

    public Guid? UnifiedCodeId { get; set; }

    [MaxLength(20)]
    public string? UnspscCode { get; set; }

    [MaxLength(500)]
    public string? UnspscName { get; set; }

    [MaxLength(20)]
    public string? ItemType { get; set; } // Bien | Servicio | Obra

    [MaxLength(30)]
    public string? ItemSubtype { get; set; }

    [MaxLength(30)]
    public string? AssetTreatment { get; set; }

    [MaxLength(50)]
    public string? MeasureUnit { get; set; }

    public decimal? ReferencePrice { get; set; }

    [MaxLength(3)]
    public string? Currency { get; set; }

    [MaxLength(100)]
    public string? PriceSource { get; set; }

    // ───────────── Mapeo regulatorio ─────────────

    public Guid? ExpenseObjectId { get; set; }

    [MaxLength(20)]
    public string? ExpenseObjectCode { get; set; }

    [MaxLength(300)]
    public string? ExpenseObjectName { get; set; }

    public Guid? AccountingAccountId { get; set; }

    [MaxLength(30)]
    public string? AccountingAccountCode { get; set; }

    [MaxLength(300)]
    public string? AccountingAccountName { get; set; }

    // ───────────── Ruteo Consolidador ─────────────

    public Guid? ConsolidatorAreaId { get; set; }

    [MaxLength(250)]
    public string? ConsolidatorAreaName { get; set; }

    /// <summary>Origen de la asignación: Manual | Rule | Imported | None.</summary>
    [MaxLength(20)]
    public required string ConsolidatorAreaSource { get; set; } = "None";

    public Guid? ConsolidatorRuleId { get; set; }

    [MaxLength(30)]
    public string? ConsolidatorRuleKey { get; set; }

    public int? ConsolidatorRuleVersion { get; set; }

    public int? ConsolidatorAreaConfidence { get; set; }
    public int? ExpenseObjectConfidence { get; set; }
    public int? AccountingAccountConfidence { get; set; }

    [MaxLength(30)]
    public string? MatchMethod { get; set; }

    public bool AreaAmbiguity { get; set; }

    [MaxLength(1000)]
    public string? AlternativeAreasJson { get; set; }

    public bool RequiresReview { get; set; }

    [MaxLength(500)]
    public string? ReviewReason { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // ───────────── Gobernanza ─────────────

    /// <summary>Estado del artículo: Pending | Active | Requested | Disabled.</summary>
    [MaxLength(20)]
    public required string State { get; set; } = "Pending";

    public Guid? DuplicateOfMasterProductId { get; set; }

    public Guid? RequestedBy { get; set; }

    [MaxLength(250)]
    public string? RequestedByName { get; set; }

    public Guid? RequestedAreaId { get; set; }

    public DateTime? RequestedAt { get; set; }

    [MaxLength(1000)]
    public string? RequestJustification { get; set; }

    public int Version { get; set; } = 1;

    public DateTime? SourceUpdatedAt { get; set; }

    // ───────────── Auditoría y soft delete ─────────────

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // ───────────── Navegación ─────────────

    public virtual UnifiedCode? UnifiedCode { get; set; }
    public virtual ExpenseObject? ExpenseObject { get; set; }
    public virtual AccountingAccount? AccountingAccount { get; set; }
    public virtual OrganizationalStructure? ConsolidatorArea { get; set; }
    public virtual ConsolidatorRoutingRule? ConsolidatorRule { get; set; }
    public virtual MasterProduct? DuplicateOfMasterProduct { get; set; }
    public virtual ICollection<MasterProductAlias> Aliases { get; } = [];
}
