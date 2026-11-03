using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Nodo del catálogo UNSPSC de la DGCP: árbol de 4 niveles
/// (Segment → Family → Class → Material). Es **dato regulatorio de referencia**,
/// importado y de solo lectura.
///
/// Los atributos de gestión (área consolidadora, precio, unidad, anticipación) vivían
/// aquí hasta M8 y se heredaban por el árbol; ahora pertenecen al artículo
/// (<see cref="MasterProduct"/>), porque una misma clase UNSPSC agrupa artículos con
/// objeto del gasto y dueño distintos.
/// </summary>
public class UnifiedCode : ISoftDeletable
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    /// <summary>Nivel en la jerarquía UNSPSC: Segment, Family, Class, Material.</summary>
    [MaxLength(20)]
    public required string Level { get; set; }

    /// <summary>Código UNSPSC (ej: "43" para segmento, "43211501" para material).</summary>
    [MaxLength(20)]
    public required string Code { get; set; }

    [MaxLength(500)]
    public required string Name { get; set; }

    [MaxLength(500)]
    public string? EnglishName { get; set; }

    [MaxLength(2000)]
    public string? Definition { get; set; }

    [MaxLength(30)]
    public string? SourceRole { get; set; }

    public bool? IncludedInMaster { get; set; }

    [MaxLength(20)]
    public string? LegacyExpenseObjectCode { get; set; }

    /// <summary>
    /// Denominación del objeto del gasto legado. El archivo trae aquí la descripción
    /// completa del clasificador (hasta ~1,100 caracteres), no solo el nombre corto.
    /// </summary>
    [MaxLength(2000)]
    public string? LegacyExpenseObjectName { get; set; }

    [MaxLength(200)]
    public string? Source { get; set; }

    /// <summary>Referencia al nodo padre (self-FK). Null para segmentos raíz.</summary>
    public Guid? ParentId { get; set; }

    /// <summary>Habilitado para uso institucional. Default true.</summary>
    public bool InstitutionallyEnabled { get; set; } = true;

    // ───────────── Auditoría y soft delete ─────────────

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // ───────────── Navegación ─────────────

    public virtual UnifiedCode? Parent { get; set; }
    public virtual ICollection<UnifiedCode> Children { get; } = [];
    public virtual ICollection<UnifiedCodeAlias> Aliases { get; } = [];
}
