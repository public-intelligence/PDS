using PDS.Domain.DTOs.LegalInstrument;
using PDS.Domain.Enums;

namespace PDS.Domain.DTOs.StructureVersion;

public record StructureVersionDetailDto
{
    public Guid Id { get; init; }
    public int? Number { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public VersionStatus Status { get; init; }
    public DateOnly? EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; init; }
    public bool IsPublic { get; init; }

    public Guid CreatedBy { get; init; }
    public string? CreatedByName { get; init; }
    public DateTime CreatedAt { get; init; }
    public Guid? ReviewedBy { get; init; }
    public string? ReviewedByName { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public Guid? ApprovedBy { get; init; }
    public string? ApprovedByName { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public Guid? PublishedBy { get; init; }
    public string? PublishedByName { get; init; }
    public DateTime? PublishedAt { get; init; }

    public LegalInstrumentDto? LegalInstrument { get; init; }
    public ICollection<AuditLogDto> AuditLogs { get; init; } = [];
}
