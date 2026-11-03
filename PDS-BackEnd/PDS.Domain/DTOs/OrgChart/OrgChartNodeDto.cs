namespace PDS.Domain.DTOs.OrgChart;

/// <summary>
/// Nodo plano del organigrama. La lista plana (id/parentId lógicos) facilita el diff, el filtrado
/// y la reconstrucción del árbol en el frontend.
/// </summary>
public record OrgChartNodeDto
{
    /// <summary>Identificador lógico estable de la unidad (sirve como id para armar el árbol).</summary>
    public Guid SourceUnitId { get; init; }

    /// <summary>Identificador lógico del padre. NULL = raíz.</summary>
    public Guid? ParentSourceUnitId { get; init; }

    public required string Code { get; init; }
    public required string Name { get; init; }
    public Guid AreaTypeId { get; init; }
    public string? AreaTypeName { get; init; }
    public int Level { get; init; }

    /// <summary>
    /// True si la unidad está enlazada a un perfil de área consolidadora. Los sistemas que
    /// consumen el organigrama (M2M) lo usan para saber a quién enrutar requerimientos sin
    /// tener que replicar el catálogo de consolidadoras.
    /// </summary>
    public bool IsConsolidatorArea { get; init; }

    public ICollection<OrgChartPositionDto> Positions { get; init; } = [];
}
