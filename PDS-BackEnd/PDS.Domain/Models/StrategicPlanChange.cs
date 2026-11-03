using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Registro de cambios de una versión del PEI (M3), append-only: cada mutación de la
/// enmienda en curso anota qué se hizo, a qué nivel y quién. Es el "Registro de cambios"
/// visible en la pantalla del PEI (además de la bitácora general PlanningActivityLog).
/// </summary>
public class StrategicPlanChange
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }

    public DateTime Date { get; set; }

    public Guid UserId { get; set; }

    [MaxLength(250)]
    public string? UserName { get; set; }

    /// <summary>Tipo de cambio: Added, Edited, Discontinued.</summary>
    [MaxLength(20)]
    public required string ChangeType { get; set; }

    /// <summary>Nivel afectado: Focus, Objective, Product, MeasureUnit.</summary>
    [MaxLength(30)]
    public required string Level { get; set; }

    [MaxLength(1000)]
    public required string Description { get; set; }

    public virtual StrategicPlan? Plan { get; set; }
}
