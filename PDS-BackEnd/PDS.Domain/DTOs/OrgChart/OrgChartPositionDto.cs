namespace PDS.Domain.DTOs.OrgChart;

public record OrgChartPositionDto
{
    public Guid PositionId { get; init; }
    public required string Name { get; init; }
    public bool IsSupervisor { get; init; }
    public int AuthorizedPosts { get; init; }
    public string? OccupationalGroupName { get; init; }
}
