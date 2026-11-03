namespace PDS.Domain.DTOs.Planning;

// ───────────────────────── Códigos unificados (UNSPSC / DGCP) ─────────────────────────
//
// Catálogo regulatorio de solo lectura desde M8: se carga con el importador de la
// Maestra Institucional y se consume tal cual. Los atributos de gestión (área
// consolidadora, precio, unidad, anticipación) viven en el artículo, no en el nodo.

/// <summary>Nodo del árbol UNSPSC para listados.</summary>
public record UnifiedCodeDto
{
    public required Guid Id { get; init; }
    public required string Level { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public Guid? ParentId { get; init; }
    public string? EnglishName { get; init; }
    public bool InstitutionallyEnabled { get; init; }
    public int ChildCount { get; init; }
}

/// <summary>Detalle de un nodo con su definición de origen y sus alias externos.</summary>
public record UnifiedCodeDetailDto
{
    public required Guid Id { get; init; }
    public required string Level { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public Guid? ParentId { get; init; }
    public string? EnglishName { get; init; }
    public string? Definition { get; init; }
    public string? SourceRole { get; init; }
    public bool? IncludedInMaster { get; init; }

    /// <summary>Objeto del gasto legado que traía el catálogo de compras (referencial).</summary>
    public string? LegacyExpenseObjectCode { get; init; }
    public string? LegacyExpenseObjectName { get; init; }

    public string? Source { get; init; }
    public bool InstitutionallyEnabled { get; init; }

    public required IReadOnlyCollection<UnifiedCodeAliasDto> Aliases { get; init; }
}

/// <summary>Alias de un código en un sistema externo (DIGEPRES, DIGECOB, inventario…).</summary>
public record UnifiedCodeAliasDto
{
    public required Guid Id { get; init; }
    public required string System { get; init; }
    public required string Code { get; init; }
    public string? Name { get; init; }
}
