using System.ComponentModel.DataAnnotations;
namespace PDS.Domain.Models;

/// <summary>
/// Secuencia atómica para generar códigos únicos en backend (M5).
/// UPDATE ... OUTPUT bajo la transacción del insert resuelve colisiones.
/// </summary>
public class PlanningSequence
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    /// <summary>Ámbito: POA_PRODUCT, POA_TASK, REQUIREMENT.</summary>
    [MaxLength(30)]
    public required string Scope { get; set; }

    /// <summary>Clave de alcance (ej. PlanId para POA_PRODUCT).</summary>
    [MaxLength(80)]
    public required string ScopeKey { get; set; }

    public int NextValue { get; set; } = 1;
}
