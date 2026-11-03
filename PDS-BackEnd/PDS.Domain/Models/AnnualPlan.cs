using System.ComponentModel.DataAnnotations;
namespace PDS.Domain.Models;

/// <summary>
/// Plan anual operativo (POA) de un área dentro de un ciclo (M4 crea la shell, M5 agrega la lógica completa).
/// Creado transaccionalmente al publicar el ciclo. Sin soft delete.
/// </summary>
public class AnnualPlan
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    public Guid CycleId { get; set; }

    public Guid AreaId { get; set; }
    [MaxLength(250)]
    public required string AreaName { get; set; }

    [MaxLength(500)]
    public required string Name { get; set; }

    /// <summary>
    /// Estados en español (vocabulario del contrato funcional, es lo que persiste PublishAsync y
    /// consume la UI): "Sin iniciar", "Borrador", "Listo para revisión", "Observado",
    /// "En ajustes", "Aprobado", "En ejecución", "Cerrado". Ver AnnualPlanStatuses (M5).
    /// </summary>
    [MaxLength(30)]
    public required string Status { get; set; }

    public int Version { get; set; } = 1;
    public DateTime? DueDate { get; set; }

    [MaxLength(2000)]
    public string? GeneralObservation { get; set; }

    /// <summary>Concurrency token para M5 (edición concurrente del POA).</summary>
    public byte[] RowVersion { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual PlanningCycle? Cycle { get; set; }
    public virtual ICollection<PoaProduct> Products { get; } = [];
}
