namespace PDS.Domain.DTOs.Position;

public record PositionSupervisorLevelDto
{
    public int Level { get; init; }
    public required IEnumerable<PositionDto> Positions { get; init; }
}
