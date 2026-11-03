using System;
using System.Collections.Generic;

namespace PDS.Domain.DTOs.Planning;

public record PlanningCycleDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required int Year { get; init; }
    public int? EndYear { get; init; }
    public required string Status { get; init; }
    public required Guid StrategicPlanId { get; init; }
    public string? StrategicPlanName { get; init; }
    public required Guid TemplateId { get; init; }
    public string? TemplateName { get; init; }
    public DateTime? PublishedAt { get; init; }
    public int AreaCount { get; init; }
    public Dictionary<string, int> PlanCountsByStatus { get; init; } = new();
}

public record PlanningCycleDetailDto : PlanningCycleDto
{
    public required Guid CreatedBy { get; init; }
    public string? CreatedByName { get; init; }
    public required DateTime CreatedAt { get; init; }
    public IReadOnlyCollection<PlanningCycleMilestoneDto> Milestones { get; init; } = Array.Empty<PlanningCycleMilestoneDto>();
    public IReadOnlyCollection<PlanningCycleAreaDto> Areas { get; init; } = Array.Empty<PlanningCycleAreaDto>();
    public IReadOnlyCollection<BudgetCeilingDto> BudgetCeilings { get; init; } = Array.Empty<BudgetCeilingDto>();
    public IReadOnlyCollection<PlanningCycleAmendmentLogDto> Amendments { get; init; } = Array.Empty<PlanningCycleAmendmentLogDto>();
}

public record PlanningCycleAmendmentLogDto
{
    public Guid Id { get; init; }
    public required string AmendmentType { get; init; }
    public required string Justification { get; init; }
    public string? PreviousValuesJson { get; init; }
    public string? NewValuesJson { get; init; }
    public Guid CreatedBy { get; init; }
    public string? CreatedByName { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record PlanningCycleMilestoneDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime Date { get; init; }
    public required string Kind { get; init; }
}

public record PlanningCycleAreaDto
{
    public Guid Id { get; init; }
    public required Guid AreaId { get; init; }
    public required string AreaName { get; init; }
}

public record CreatePlanningCycleDto
{
    public required string Name { get; init; }
    public required int Year { get; init; }
    public int? EndYear { get; init; }
    public required Guid StrategicPlanId { get; init; }
    public required Guid TemplateId { get; init; }
    public required List<CreatePlanningCycleMilestoneDto> Milestones { get; init; } = new();
    public required List<Guid> AreaIds { get; init; } = new();
}

public record CreatePlanningCycleMilestoneDto
{
    public required string Name { get; init; }
    public required DateTime Date { get; init; }
    public required string Kind { get; init; }
}

public record UpdatePlanningCycleDto
{
    public required string Name { get; init; }
    public required int Year { get; init; }
    public int? EndYear { get; init; }
    public required Guid StrategicPlanId { get; init; }
    public required Guid TemplateId { get; init; }
    public string? Justification { get; init; }
    public required List<CreatePlanningCycleMilestoneDto> Milestones { get; init; } = new();
    public required List<Guid> AreaIds { get; init; } = new();
}

public record EligibleAreaDto
{
    public required Guid AreaId { get; init; }
    public required string AreaName { get; init; }
}
