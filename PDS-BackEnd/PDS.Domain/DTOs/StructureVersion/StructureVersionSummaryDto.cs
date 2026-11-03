using PDS.Domain.DTOs.LegalInstrument;
using PDS.Domain.Enums;

namespace PDS.Domain.DTOs.StructureVersion;

public record StructureVersionSummaryDto
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
    public DateTime? PublishedAt { get; init; }
    public string? PublishedByName { get; init; }

    public LegalInstrumentDto? LegalInstrument { get; init; }
}
