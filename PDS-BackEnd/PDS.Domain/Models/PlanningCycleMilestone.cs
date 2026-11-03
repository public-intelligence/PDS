using System.ComponentModel.DataAnnotations;
namespace PDS.Domain.Models;

/// <summary>
/// Hito del calendario del ciclo de planificación (M4). El hito con Kind = "FormulationDeadline"
/// alimenta el DueDate de los planes anuales generados al publicar.
/// </summary>
public class PlanningCycleMilestone
{
    public Guid Id { get; set; }
    public Guid CycleId { get; set; }

    [MaxLength(250)]
    public required string Name { get; set; }

    public DateTime Date { get; set; }

    /// <summary>Tipo de hito: FormulationDeadline, Review, Approval, Other.</summary>
    [MaxLength(30)]
    public required string Kind { get; set; }

    public virtual PlanningCycle? Cycle { get; set; }
}
