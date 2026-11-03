namespace PDS.Domain.DTOs.Planning;

// ───────────────────────── Plantillas ─────────────────────────

/// <summary>Resumen de plantilla para listados.</summary>
public record PlanningTemplateDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int Version { get; init; }
    public bool IsActive { get; init; }
    public int ColumnCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>Detalle de plantilla con sus columnas.</summary>
public record PlanningTemplateDetailDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int Version { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public required IReadOnlyCollection<PlanningTemplateColumnDto> Columns { get; init; }
}

/// <summary>Columna de una plantilla.</summary>
public record PlanningTemplateColumnDto
{
    public required Guid Id { get; init; }
    public required string VisibleName { get; init; }
    public required string TechnicalName { get; init; }
    public required string Type { get; init; }
    public bool Required { get; init; }
    public bool Visible { get; init; }
    public required string Origin { get; init; }
    public string? OptionsJson { get; init; }
    public int Order { get; init; }
    public int Version { get; init; }
    public Guid? CreatedBy { get; init; }
    public string? CreatedByName { get; init; }
    public DateTime CreatedAt { get; init; }
}

// ───────────────────────── Comandos ─────────────────────────

public record CreatePlanningTemplateDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
}

public record UpdatePlanningTemplateDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
}

public record CreatePlanningTemplateColumnDto
{
    public required string VisibleName { get; init; }
    public required string TechnicalName { get; init; }
    /// <summary>Text, Number, Currency, Select.</summary>
    public required string Type { get; init; }
    public bool Required { get; init; }
    public bool Visible { get; init; } = true;
    /// <summary>Base o Custom.</summary>
    public required string Origin { get; init; }
    /// <summary>JSON array de opciones (solo para Type=Select).</summary>
    public string? OptionsJson { get; init; }
    public int Order { get; init; }
}

public record UpdatePlanningTemplateColumnDto
{
    public required string VisibleName { get; init; }
    /// <summary>Text, Number, Currency, Select.</summary>
    public required string Type { get; init; }
    public bool Required { get; init; }
    public bool Visible { get; init; }
    /// <summary>JSON array de opciones (solo para Type=Select).</summary>
    public string? OptionsJson { get; init; }
    public int Order { get; init; }
}
