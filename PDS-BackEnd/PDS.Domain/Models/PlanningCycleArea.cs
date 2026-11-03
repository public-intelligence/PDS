using System.ComponentModel.DataAnnotations;
namespace PDS.Domain.Models;

/// <summary>
/// Área organizacional alcanzada por un ciclo de planificación (M4).
/// Snapshot del nombre para que la bitácora sobreviva a cambios del organigrama.
/// </summary>
public class PlanningCycleArea
{
    public Guid Id { get; set; }
    public Guid CycleId { get; set; }

    /// <summary>FK a OrganizationalStructures (nivel 1 del organigrama vigente).</summary>
    public Guid AreaId { get; set; }

    [MaxLength(250)]
    public required string AreaName { get; set; }

    public virtual PlanningCycle? Cycle { get; set; }
}
