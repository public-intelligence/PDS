namespace PDS.Domain.Models;

/// <summary>
/// Configuración institucional del módulo de Planificación, persistida por organización.
/// Antes vivía en memoria estática: se perdía al reiniciar y divergía entre instancias,
/// de modo que el guardrail que define qué áreas formulan plan podía cambiar solo.
/// Una fila por (OrganizationId, Key); el valor va serializado en JSON.
/// </summary>
public class PlanningConfigEntry
{
    /// <summary>Guardrail de nivel jerárquico y selección personalizada de áreas.</summary>
    public const string AreaGuardrailKey = "area-guardrail";

    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    public required string Key { get; set; }

    /// <summary>Valor serializado (JSON). Su forma depende de <see cref="Key"/>.</summary>
    public required string JsonValue { get; set; }

    public Guid UpdatedBy { get; set; }
    public string? UpdatedByName { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Forma del valor guardado bajo <see cref="PlanningConfigEntry.AreaGuardrailKey"/>.</summary>
public sealed class AreaGuardrailValue
{
    /// <summary>Profundidad máxima que formula plan; -1 = selección personalizada.</summary>
    public int MaxLevel { get; set; } = 2;

    /// <summary>Áreas elegidas a mano cuando MaxLevel == -1.</summary>
    public List<Guid> CustomAreaIds { get; set; } = [];
}
