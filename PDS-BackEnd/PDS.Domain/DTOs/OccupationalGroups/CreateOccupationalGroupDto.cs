namespace PDS.Domain.DTOs.OccupationalGroups;

public record CreateOccupationalGroupDto
{
    public string Name { get; set; } = null!;
}