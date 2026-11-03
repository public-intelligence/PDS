namespace PDS.Domain.DTOs.Planning;

public record PlanningAreaConfigDto
{
    public int MaxLevel { get; init; } = 2;
    public string Description { get; init; } = "Unidades hasta nivel 2 (Despacho, Viceministerios y Direcciones)";
    public List<PlanningAreaItemDto> QualifyingAreas { get; init; } = [];
}

public record PlanningAreaItemDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Guid? ParentId { get; init; }
    public string? ParentName { get; init; }
    public int DepthLevel { get; init; }
    public bool QualifiesForPlanning { get; init; }
}
