namespace PDS.Domain.DTOs.Planning;

// ───────────────────────── Lectura ─────────────────────────

/// <summary>Plan anual en el listado (con su ciclo — navegación por listados con columna Ciclo).</summary>
public record AnnualPlanDto
{
    public required Guid Id { get; init; }
    public required Guid CycleId { get; init; }
    public required string CycleName { get; init; }
    public required int CycleYear { get; init; }
    public required string CycleStatus { get; init; }
    public required Guid AreaId { get; init; }
    public required string AreaName { get; init; }
    public required string Name { get; init; }

    /// <summary>Estado en español (AnnualPlanStatuses).</summary>
    public required string Status { get; init; }

    public required int Version { get; init; }
    public DateTime? DueDate { get; init; }
    public string? GeneralObservation { get; init; }
    public int ProductCount { get; init; }
    public int IncompleteProductCount { get; init; }
}

/// <summary>Expediente completo del plan para el workspace.</summary>
public record AnnualPlanWorkspaceDto : AnnualPlanDto
{
    /// <summary>Versión del PEI que fija el ciclo (para los selectores de alineación).</summary>
    public required Guid StrategicPlanId { get; init; }

    /// <summary>Columnas visibles de la plantilla del ciclo.</summary>
    public required IReadOnlyCollection<PlanningTemplateColumnDto> TemplateColumns { get; init; }

    public required IReadOnlyCollection<PoaProductDto> Products { get; init; }

    /// <summary>True si el ciclo está cerrado: toda escritura se rechaza (409).</summary>
    public required bool ReadOnly { get; init; }

    /// <summary>Concurrency token del plan (base64).</summary>
    public string? RowVersion { get; init; }
}

public record PoaProductDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }

    /// <summary>Pei | Operational.</summary>
    public required string Origin { get; init; }

    public Guid? StrategicFocusId { get; init; }
    public Guid? StrategicObjectiveId { get; init; }
    public Guid? StrategicProductId { get; init; }
    public required string ProductName { get; init; }
    public string? OperationalJustification { get; init; }
    public string? Observation { get; init; }
    public required int Order { get; init; }
    public required IReadOnlyCollection<PoaMeasureUnitDto> MeasureUnits { get; init; }
    public required IReadOnlyCollection<PoaTaskDto> Tasks { get; init; }
    public required IReadOnlyCollection<PoaMunicipalityDto> Municipalities { get; init; }
    public required IReadOnlyCollection<StrategicInvolvedAreaDto> InvolvedAreas { get; init; }

    /// <summary>Valores de columnas personalizadas, indexados por nombre técnico de la columna.</summary>
    public required IReadOnlyDictionary<string, string> CustomValues { get; init; }

    /// <summary>Incidencias de completitud (validación central del servidor).</summary>
    public required IReadOnlyCollection<string> Issues { get; init; }

    /// <summary>Concurrency token (base64).</summary>
    public string? RowVersion { get; init; }
}

public record PoaMeasureUnitDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required decimal TargetValue { get; init; }

    /// <summary>Absolute | Percentage.</summary>
    public required string TargetType { get; init; }

    public string? VerificationMeans { get; init; }
    public Guid? ResponsibleAreaId { get; init; }
    public string? ResponsibleAreaName { get; init; }
    public required decimal Q1 { get; init; }
    public required decimal Q2 { get; init; }
    public required decimal Q3 { get; init; }
    public required decimal Q4 { get; init; }
}

public record PoaTaskDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required bool IsContinuous { get; init; }

    /// <summary>Meses activos (0 = enero … 11 = diciembre), derivados del MonthsMask.</summary>
    public required IReadOnlyCollection<int> Months { get; init; }
}

public record PoaMunicipalityDto
{
    public required Guid MunicipioId { get; init; }
    public required string MunicipioName { get; init; }
}

/// <summary>Incidencias por producto (GET validation y respuesta 400 del submit).</summary>
public record PlanValidationDto
{
    public required Guid PlanId { get; init; }
    public required bool IsValid { get; init; }
    public required IReadOnlyCollection<ProductIssuesDto> Products { get; init; }
}

public record ProductIssuesDto
{
    public required Guid ProductId { get; init; }
    public required string Code { get; init; }
    public required string ProductName { get; init; }
    public required IReadOnlyCollection<string> Issues { get; init; }
}

// ───────────────────────── Comandos ─────────────────────────

/// <summary>
/// Patch parcial del producto: solo se aplican las propiedades no nulas (las colecciones
/// que vienen reemplazan por completo a las guardadas). Réplica del updateRow del mock.
/// </summary>
public record UpdatePoaProductDto
{
    /// <summary>Pei | Operational.</summary>
    public string? Origin { get; init; }

    public Guid? StrategicFocusId { get; init; }
    public Guid? StrategicObjectiveId { get; init; }
    public Guid? StrategicProductId { get; init; }

    /// <summary>True para limpiar la alineación PEI (los Guid? nulos no distinguen "sin cambio" de "limpiar").</summary>
    public bool? ClearAlignment { get; init; }

    public string? ProductName { get; init; }
    public string? OperationalJustification { get; init; }
    public string? Observation { get; init; }
    public IReadOnlyCollection<UpsertPoaUnitDto>? MeasureUnits { get; init; }
    public IReadOnlyCollection<UpsertPoaTaskDto>? Tasks { get; init; }
    public IReadOnlyCollection<Guid>? MunicipioIds { get; init; }
    public IReadOnlyCollection<Guid>? InvolvedAreaIds { get; init; }

    /// <summary>Valores por nombre técnico de columna (solo las claves enviadas se tocan).</summary>
    public IReadOnlyDictionary<string, string>? CustomValues { get; init; }

    /// <summary>Concurrency token del producto (base64); si viene y no coincide → 409.</summary>
    public string? RowVersion { get; init; }
}

public record UpsertPoaUnitDto
{
    /// <summary>Null para unidades nuevas.</summary>
    public Guid? Id { get; init; }

    public required string Name { get; init; }
    public required decimal TargetValue { get; init; }
    public required string TargetType { get; init; }
    public string? VerificationMeans { get; init; }
    public Guid? ResponsibleAreaId { get; init; }
    public required decimal Q1 { get; init; }
    public required decimal Q2 { get; init; }
    public required decimal Q3 { get; init; }
    public required decimal Q4 { get; init; }
}

public record UpsertPoaTaskDto
{
    /// <summary>Null para tareas nuevas (conservar el Id de las existentes protege los enlaces de requerimientos, M7).</summary>
    public Guid? Id { get; init; }

    public required string Name { get; init; }
    public required bool IsContinuous { get; init; }
    public required IReadOnlyCollection<int> Months { get; init; }
}

public record ReturnPlanDto
{
    public required string Observation { get; init; }
}
