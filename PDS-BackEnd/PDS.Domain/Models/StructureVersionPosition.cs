namespace PDS.Domain.Models;

/// <summary>
/// Fotografía inmutable de un cargo asignado a una unidad dentro de una versión publicada.
/// AuthorizedPosts (plazas autorizadas) vive solo aquí: es atributo de definición, no de ocupación
/// real (no se manejan plazas ocupadas/vacantes).
/// </summary>
public class StructureVersionPosition
{
    public Guid Id { get; set; }
    public Guid StructureVersionId { get; set; }

    /// <summary>Identificador lógico de la unidad a la que pertenece el cargo (Id operativo de la unidad).</summary>
    public Guid SourceOrganizationalStructureId { get; set; }

    /// <summary>Id operativo del cargo (Position).</summary>
    public Guid PositionId { get; set; }

    public required string PositionName { get; set; }

    /// <summary>Cantidad de plazas autorizadas (definición). No es ocupación real.</summary>
    public int AuthorizedPosts { get; set; } = 1;

    /// <summary>Cargo supervisor (proviene de Position.IsManager).</summary>
    public bool IsSupervisor { get; set; }

    public Guid OccupationalGroupId { get; set; }
    public string? OccupationalGroupName { get; set; }

    // RF-RE-03/RF-RE-04: salario y grado se fotografían al publicar para que el costo estimado y la
    // categoría salarial (= grado) sean fieles a cada versión histórica, sin depender de la entidad
    // operativa Position (que puede cambiar o borrarse después). No es ocupación ni nómina real.

    /// <summary>Grado salarial (categoría salarial) fotografiado desde Position.Grade.</summary>
    public int Grade { get; set; }

    /// <summary>Salario mínimo del rango, fotografiado desde Position.MinSalary.</summary>
    public decimal MinSalary { get; set; }

    /// <summary>Salario máximo del rango, fotografiado desde Position.MaxSalary.</summary>
    public decimal MaxSalary { get; set; }

    public virtual StructureVersion? StructureVersion { get; set; }
}
