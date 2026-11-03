using System.ComponentModel.DataAnnotations;
namespace PDS.Domain.Models;

/// <summary>
/// Ciclo de planificación (M4). Vincula una versión del PEI, una plantilla del plan anual,
/// un conjunto de áreas y un calendario de hitos. Publicar el ciclo crea los planes anuales
/// de las áreas alcanzadas en una transacción atómica.
/// </summary>
public class PlanningCycle : ISoftDeletable
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    [MaxLength(250)]
    public required string Name { get; set; }

    public int Year { get; set; }
    public int? EndYear { get; set; }

    /// <summary>Estado: Draft, Published, Closed.</summary>
    [MaxLength(20)]
    public required string Status { get; set; }

    /// <summary>PEI que fija la versión estratégica del ciclo (preselecciona la Vigente).</summary>
    public Guid StrategicPlanId { get; set; }

    /// <summary>Plantilla que define las columnas del plan anual.</summary>
    public Guid TemplateId { get; set; }

    public DateTime? PublishedAt { get; set; }
    public Guid? PublishedBy { get; set; }
    [MaxLength(250)]
    public string? PublishedByName { get; set; }

    public Guid CreatedBy { get; set; }
    [MaxLength(250)]
    public string? CreatedByName { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public virtual StrategicPlan? StrategicPlan { get; set; }
    public virtual PlanningTemplate? Template { get; set; }
    public virtual ICollection<PlanningCycleMilestone> Milestones { get; } = [];
    public virtual ICollection<PlanningCycleArea> Areas { get; } = [];
    public virtual ICollection<AnnualPlan> Plans { get; } = [];
    public virtual ICollection<BudgetCeiling> BudgetCeilings { get; } = [];
    public virtual ICollection<PlanningCycleAmendmentLog> AmendmentLogs { get; } = [];
}
