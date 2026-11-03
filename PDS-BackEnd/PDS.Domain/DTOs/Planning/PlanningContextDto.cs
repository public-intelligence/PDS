namespace PDS.Domain.DTOs.Planning;

public record AreaInfoDto(Guid Id, string Name);

/// <summary>
/// Contexto de planificación del usuario autenticado: su área organizacional (resuelta vía
/// GOBID Personnel → Position local) y sus permisos efectivos. Es la base de los filtros
/// de acciones por área en todo el módulo de Planificación.
/// </summary>
public record PlanningContextDto
{
    public required Guid UserId { get; init; }
    public string? UserName { get; init; }

    /// <summary>Cargo del usuario en PDS (si se pudo resolver desde GOBID).</summary>
    public Guid? PositionId { get; init; }
    public string? PositionName { get; init; }

    /// <summary>Unidad organizativa exacta a la que pertenece el cargo.</summary>
    public Guid? UnitAreaId { get; init; }
    public string? UnitAreaName { get; init; }

    /// <summary>
    /// Área que formula el plan del usuario: el ancestro más cercano (empezando por su propia
    /// unidad) que califica según el guardrail vigente. Si ninguno califica, cae a la unidad
    /// de nivel 1. Null si no se pudo resolver o si el usuario pertenece a la raíz (despacho).
    /// </summary>
    public Guid? PlanAreaId { get; init; }
    public string? PlanAreaName { get; init; }

    /// <summary>
    /// Áreas sobre las que el usuario puede actuar: su unidad y sus ancestros que califican
    /// para planificar. Existe porque el guardrail admite planes hasta cierta profundidad
    /// (p. ej. Direcciones en nivel 2) mientras que PlanAreaId es una sola: sin la cadena,
    /// el dueño de un plan de nivel 2 no pasaba el chequeo sobre su propio plan.
    /// </summary>
    public IReadOnlyCollection<Guid> EligibleAreaIds { get; init; } = [];
    public IReadOnlyCollection<AreaInfoDto> EligibleAreas { get; init; } = [];

    /// <summary>
    /// Áreas consolidadoras a las que pertenece el usuario (perfil ConsolidatorAreaProfile
    /// enlazado a una de sus áreas). Vacío = no consolida, aunque tenga el permiso.
    /// </summary>
    public IReadOnlyCollection<Guid> ConsolidatorAreaIds { get; init; } = [];
    public IReadOnlyCollection<AreaInfoDto> ConsolidatorAreas { get; init; } = [];

    /// <summary>Permisos efectivos (claim permissions del token + overrides de desarrollo).</summary>
    public required IReadOnlyCollection<string> Permissions { get; init; }

    /// <summary>True si el contexto fue alterado por PlanningDevOverrides (solo entornos de desarrollo).</summary>
    public bool IsDevOverride { get; init; }
}
