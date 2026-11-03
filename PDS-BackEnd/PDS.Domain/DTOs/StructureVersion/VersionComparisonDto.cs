namespace PDS.Domain.DTOs.StructureVersion;

/// <summary>
/// Resultado de comparar dos versiones (RF-VH-03): unidades creadas, eliminadas, modificadas y
/// reubicadas. Las marcas (changeType) son combinables: una unidad puede ser Modified y Relocated.
/// </summary>
public record VersionComparisonDto
{
    public required StructureVersionSummaryDto FromVersion { get; init; }
    public required StructureVersionSummaryDto ToVersion { get; init; }
    public required ComparisonSummaryDto Summary { get; init; }
    public ICollection<UnitChangeDto> UnitChanges { get; init; } = [];
}

public record ComparisonSummaryDto
{
    public int CreatedCount { get; init; }
    public int DeletedCount { get; init; }
    public int ModifiedCount { get; init; }
    public int RelocatedCount { get; init; }
}

public record UnitChangeDto
{
    public Guid SourceUnitId { get; init; }

    /// <summary>Marcas combinables: "Created", "Deleted", "Modified", "Relocated".</summary>
    public ICollection<string> ChangeType { get; init; } = [];

    public UnitSnapshotDto? Before { get; init; }
    public UnitSnapshotDto? After { get; init; }

    public ICollection<FieldChangeDto> FieldChanges { get; init; } = [];

    public Guid? FromParentSourceUnitId { get; init; }
    public Guid? ToParentSourceUnitId { get; init; }
}

public record FieldChangeDto
{
    public required string Field { get; init; }
    public string? From { get; init; }
    public string? To { get; init; }
}

public record UnitSnapshotDto
{
    public Guid SourceUnitId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? AreaTypeName { get; init; }
    public int Level { get; init; }
    public Guid? ParentSourceUnitId { get; init; }
}
