namespace PDS.Domain.DTOs.Planning;

public record ActivityLogDto
{
    public required Guid Id { get; init; }
    public required DateTime Date { get; init; }
    public string? UserName { get; init; }
    public required string Module { get; init; }
    public required string Action { get; init; }
    public string? Detail { get; init; }
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
}
