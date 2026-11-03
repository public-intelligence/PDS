namespace PDS.Domain.DTOs.OccupationalGroups;

public record OccupationalGroupDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
}