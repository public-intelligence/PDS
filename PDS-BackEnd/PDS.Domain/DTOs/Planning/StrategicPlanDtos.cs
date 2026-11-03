namespace PDS.Domain.DTOs.Planning;

// ───────────────────────── Lectura ─────────────────────────

/// <summary>Versión del PEI en el listado (sin el grafo).</summary>
public record StrategicPlanDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required int PeriodStartYear { get; init; }
    public required int PeriodEndYear { get; init; }
    public required int VersionNumber { get; init; }

    /// <summary>Draft, Current, Historical.</summary>
    public required string Status { get; init; }

    public string? Reason { get; init; }
    public string? CreatedByName { get; init; }
    public required DateTime CreatedAt { get; init; }
    public string? ApprovedByName { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public Guid? PreviousVersionId { get; init; }
    public int FocusCount { get; init; }
}

/// <summary>Versión del PEI con el grafo completo y su registro de cambios.</summary>
public record StrategicPlanDetailDto : StrategicPlanDto
{
    public required IReadOnlyCollection<StrategicFocusDto> Focuses { get; init; }
    public required IReadOnlyCollection<StrategicPlanChangeDto> Changes { get; init; }
}

public record StrategicFocusDto
{
    public required Guid Id { get; init; }
    public required Guid LineageId { get; init; }
    public required string Name { get; init; }
    public string? EndAlignment { get; init; }
    public required int Order { get; init; }
    public required IReadOnlyCollection<StrategicObjectiveDto> Objectives { get; init; }
}

public record StrategicObjectiveDto
{
    public required Guid Id { get; init; }
    public required Guid LineageId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required int Order { get; init; }
    public required IReadOnlyCollection<StrategicProductDto> Products { get; init; }
}

public record StrategicProductDto
{
    public required Guid Id { get; init; }
    public required Guid LineageId { get; init; }
    public required string Name { get; init; }
    public int? DiscontinuedFromYear { get; init; }
    public string? DiscontinuationReason { get; init; }
    public required int Order { get; init; }
    public required IReadOnlyCollection<StrategicInvolvedAreaDto> InvolvedAreas { get; init; }
    public required IReadOnlyCollection<StrategicMeasureUnitDto> MeasureUnits { get; init; }
}

public record StrategicInvolvedAreaDto
{
    public required Guid AreaId { get; init; }
    public string? AreaName { get; init; }
}

public record StrategicMeasureUnitDto
{
    public required Guid Id { get; init; }
    public required Guid LineageId { get; init; }
    public required string Name { get; init; }
    public Guid? ResponsibleAreaId { get; init; }
    public string? ResponsibleAreaName { get; init; }
    public int? DiscontinuedFromYear { get; init; }
    public string? DiscontinuationReason { get; init; }
    public required int Order { get; init; }

    /// <summary>Metas por año del período. El frontend bloquea la edición de años ≤ año actual.</summary>
    public required IReadOnlyCollection<StrategicAnnualTargetDto> Targets { get; init; }
}

public record StrategicAnnualTargetDto
{
    public required int Year { get; init; }
    public required decimal Value { get; init; }
}

public record StrategicPlanChangeDto
{
    public required DateTime Date { get; init; }
    public string? UserName { get; init; }

    /// <summary>Added, Edited, Discontinued.</summary>
    public required string ChangeType { get; init; }

    /// <summary>Focus, Objective, Product, MeasureUnit.</summary>
    public required string Level { get; init; }

    public required string Description { get; init; }
}

// ───────────────────────── Comandos ─────────────────────────

/// <summary>Crea el primer PEI de la organización (nace en Draft, versión 1).</summary>
public record CreateStrategicPlanDto
{
    public required string Name { get; init; }
    public required int PeriodStartYear { get; init; }
    public required int PeriodEndYear { get; init; }
}

/// <summary>Abre una enmienda clonando la versión vigente. La justificación es obligatoria.</summary>
public record CreateAmendmentDto
{
    public required string Reason { get; init; }
}

/// <summary>Edita los datos de cabecera del Draft.</summary>
public record UpdateStrategicPlanDto
{
    public string? Name { get; init; }
    public int? PeriodStartYear { get; init; }
    public int? PeriodEndYear { get; init; }
}

public record CreateStrategicFocusDto
{
    public required string Name { get; init; }
    public string? EndAlignment { get; init; }
}

public record UpdateStrategicFocusDto
{
    public required string Name { get; init; }
    public string? EndAlignment { get; init; }
}

public record CreateStrategicObjectiveDto
{
    public required string Name { get; init; }
}

public record UpdateStrategicObjectiveDto
{
    public required string Name { get; init; }
}

public record CreateStrategicProductDto
{
    public required string Name { get; init; }
}

/// <summary>Edita nombre y/o áreas involucradas (reemplazo total de la lista si viene).</summary>
public record UpdateStrategicProductDto
{
    public string? Name { get; init; }
    public IReadOnlyCollection<Guid>? InvolvedAreaIds { get; init; }
}

/// <summary>Descontinuación de producto o unidad: desde un año futuro, con motivo obligatorio.</summary>
public record DiscontinueDto
{
    public required int FromYear { get; init; }
    public required string Reason { get; init; }
}

/// <summary>
/// Reemplazo saneado de las unidades de un producto (patrón reemplazarUnidadesPei del mock):
/// las metas de años transcurridos se preservan desde lo guardado y las unidades con
/// historia que falten en la lista se reincorporan.
/// </summary>
public record ReplaceMeasureUnitsDto
{
    public required IReadOnlyCollection<UpsertMeasureUnitDto> Units { get; init; }
}

public record UpsertMeasureUnitDto
{
    /// <summary>Null para unidades nuevas.</summary>
    public Guid? Id { get; init; }

    public required string Name { get; init; }
    public Guid? ResponsibleAreaId { get; init; }
    public required IReadOnlyCollection<StrategicAnnualTargetDto> Targets { get; init; }
}
