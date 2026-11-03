using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Catálogo de Plan de Cuentas (DIGECOB — M8).
/// </summary>
public class AccountingAccount : ISoftDeletable
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    [MaxLength(30)]
    public required string Code { get; set; }

    [MaxLength(300)]
    public required string Name { get; set; }

    /// <summary>Imputable S/N.</summary>
    public bool IsPostable { get; set; }

    public int Level { get; set; }

    [MaxLength(30)]
    public string? ParentCode { get; set; }

    public Guid? ParentId { get; set; }

    [MaxLength(4000)]
    public string? ExtendedDescription { get; set; }

    public int? SourcePdfPage { get; set; }
    public int? SourcePrintedPage { get; set; }

    [MaxLength(200)]
    public string? Source { get; set; }

    // ───────────── Auditoría y soft delete ─────────────

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // ───────────── Navegación ─────────────

    public virtual AccountingAccount? Parent { get; set; }
    public virtual ICollection<AccountingAccount> Children { get; } = [];
    public virtual ICollection<MastersoftDigecobMapping> MastersoftMappings { get; } = [];
}
