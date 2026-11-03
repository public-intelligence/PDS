using System;
using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Alias / sinónimo de un artículo (M8/M9): no lleva soft delete.
/// </summary>
public class MasterProductAlias
{
    public Guid Id { get; set; }
    public Guid MasterProductId { get; set; }
    public int OrganizationId { get; set; }

    /// <summary>Tipo de alias: Synonym | LegacyCode | External.</summary>
    [MaxLength(20)]
    public required string Kind { get; set; }

    [MaxLength(30)]
    public string? System { get; set; }

    [MaxLength(300)]
    public required string Text { get; set; }

    [MaxLength(300)]
    public required string NormalizedText { get; set; }

    // ───────────── Navegación ─────────────

    public virtual MasterProduct MasterProduct { get; set; } = null!;
}
