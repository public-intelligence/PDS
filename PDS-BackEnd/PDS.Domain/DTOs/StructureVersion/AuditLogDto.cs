using PDS.Domain.Enums;

namespace PDS.Domain.DTOs.StructureVersion;

public record AuditLogDto
{
    public Guid Id { get; init; }
    public VersionAction Action { get; init; }
    public Guid UserId { get; init; }
    public string? UserName { get; init; }
    public DateTime Date { get; init; }
    public string? Comment { get; init; }
}
