namespace PDS.Domain.DTOs.StructureVersion;

public record CreateStructureVersionDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
}
