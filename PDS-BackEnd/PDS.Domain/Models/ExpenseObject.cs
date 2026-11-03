using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Catálogo de Clasificador por Objeto del Gasto (DIGEPRES — M8).
/// </summary>
public class ExpenseObject : ISoftDeletable
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    [MaxLength(20)]
    public required string Code { get; set; }

    [MaxLength(300)]
    public required string Name { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public int Level { get; set; }

    [MaxLength(20)]
    public string? ParentCode { get; set; }

    public Guid? ParentId { get; set; }

    public bool IsDetail { get; set; }

    public int? SourcePdfPage { get; set; }
    public int? SourcePrintedPage { get; set; }

    [MaxLength(200)]
    public string? Source { get; set; }

    // ───────────── Auditoría y soft delete ─────────────

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // ───────────── Navegación ─────────────

    public virtual ExpenseObject? Parent { get; set; }
    public virtual ICollection<ExpenseObject> Children { get; } = [];
}
