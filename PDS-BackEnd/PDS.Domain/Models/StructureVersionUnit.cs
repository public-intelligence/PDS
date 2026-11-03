namespace PDS.Domain.Models;

/// <summary>
/// Fotografía inmutable de una unidad organizativa dentro de una versión publicada.
/// </summary>
public class StructureVersionUnit
{
    public Guid Id { get; set; }
    public Guid StructureVersionId { get; set; }

    /// <summary>
    /// Identificador lógico estable de la unidad (el Id operativo de OrganizationalStructure).
    /// Persiste entre versiones y es la clave para detectar "la misma unidad" al comparar (RF-VH-03).
    /// </summary>
    public Guid SourceOrganizationalStructureId { get; set; }

    public required string Code { get; set; }
    public required string Name { get; set; }

    public Guid AreaTypeId { get; set; }
    public string? AreaTypeName { get; set; }

    /// <summary>Profundidad jerárquica calculada (raíz = 1).</summary>
    public int Level { get; set; }

    /// <summary>
    /// Referencia al identificador lógico (SourceOrganizationalStructureId) del padre dentro de
    /// la misma versión. NULL para la raíz. No es una FK: la fotografía debe sobrevivir aunque la
    /// unidad operativa se elimine después.
    /// </summary>
    public Guid? ParentSourceUnitId { get; set; }

    public string? Status { get; set; }

    public virtual StructureVersion? StructureVersion { get; set; }
}
