using System;
using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Regla de Ruteo a Área Consolidadora (Motor vivo — M8).
/// </summary>
public class ConsolidatorRoutingRule : ISoftDeletable
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    [MaxLength(30)]
    public required string RuleKey { get; set; }

    public int Version { get; set; } = 1;

    [MaxLength(250)]
    public string? Author { get; set; }

    public DateTime? RuleDate { get; set; }

    public int? SupersedesVersion { get; set; }

    public bool Reversible { get; set; } = true;

    public int Priority { get; set; } = 100;

    [MaxLength(2000)]
    public string? Keywords { get; set; }

    [MaxLength(2000)]
    public string? KeywordsNormalized { get; set; }

    [MaxLength(2000)]
    public string? ExclusionKeywords { get; set; }

    [MaxLength(2000)]
    public string? ExclusionKeywordsNormalized { get; set; }

    [MaxLength(500)]
    public string? UnspscPrefixes { get; set; }

    [MaxLength(20)]
    public string? ItemType { get; set; }

    [MaxLength(250)]
    public required string ConsolidatorAreaName { get; set; }

    public Guid? ConsolidatorAreaId { get; set; }

    [MaxLength(2000)]
    public string? Justification { get; set; }

    public bool IsActive { get; set; } = true;

    public bool InstitutionallyValidated { get; set; }

    public int BaseConfidence { get; set; } = 80;

    public bool ForceReview { get; set; }

    [MaxLength(60)]
    public string RuleState { get; set; } = "Active";

    // ───────────── Auditoría y soft delete ─────────────

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // ───────────── Navegación ─────────────

    public virtual OrganizationalStructure? ConsolidatorArea { get; set; }
}
