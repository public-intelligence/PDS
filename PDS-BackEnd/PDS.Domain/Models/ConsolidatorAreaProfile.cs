using System;
using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Perfil / Catálogo de Áreas Consolidadoras (TblCatalogoAreas — M8).
/// </summary>
public class ConsolidatorAreaProfile : ISoftDeletable
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    [MaxLength(10)]
    public required string AreaCode { get; set; }

    [MaxLength(150)]
    public string? OrganicGroup { get; set; }

    [MaxLength(250)]
    public required string AreaName { get; set; }

    [MaxLength(250)]
    public required string NormalizedAreaName { get; set; }

    public Guid? OrganizationalStructureId { get; set; }

    [MaxLength(2000)]
    public string? MandateSummary { get; set; }

    [MaxLength(60)]
    public string ConsolidatorKind { get; set; } = "AreaConsolidadora";

    [MaxLength(2000)]
    public string? ConsolidatorScope { get; set; }

    [MaxLength(2000)]
    public string? Limits { get; set; }

    [MaxLength(60)]
    public string? ValidationState { get; set; }

    [MaxLength(20)]
    public string? CatalogVersion { get; set; }

    [MaxLength(250)]
    public string? Author { get; set; }

    public DateTime? SourceUpdatedAt { get; set; }

    // ───────────── Auditoría y soft delete ─────────────

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // ───────────── Navegación ─────────────

    public virtual OrganizationalStructure? OrganizationalStructure { get; set; }
}
