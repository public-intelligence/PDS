using System;
using System.Collections.Generic;

namespace PDS.Domain.DTOs.Planning;

public record RequirementDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid CycleId { get; init; }
    public required Guid PlanId { get; init; }
    public required Guid PoaProductId { get; init; }
    public required Guid PoaTaskId { get; init; }
    public required Guid OriginAreaId { get; init; }
    public required string OriginAreaName { get; init; }
    public required Guid ConsolidatorAreaId { get; init; }
    public required string ConsolidatorAreaName { get; init; }
    public string? AppliedRule { get; init; }
    public required Guid MasterProductId { get; init; }
    public string? MasterProductCode { get; init; }
    public required string MasterProductName { get; init; }
    public string? UnspscCode { get; init; }
    public string? UnspscName { get; init; }
    public string? ExpenseObjectCode { get; init; }
    public string? ExpenseObjectName { get; init; }
    public string? AccountingAccountCode { get; init; }
    public string? AccountingAccountName { get; init; }
    public string? ItemType { get; init; }
    public required string Description { get; init; }
    public required decimal Quantity { get; init; }
    public required string UnitOfMeasure { get; init; }
    public required decimal EstimatedUnitPrice { get; init; }
    public required bool PriceConfirmed { get; init; }
    public required string Quarter { get; init; }
    public DateTime? RequiredDate { get; init; }
    public string? FundingSource { get; init; }
    public required string Priority { get; init; }
    public required string Status { get; init; }
    public decimal? ApprovedAmount { get; init; }
    public Guid? MergedIntoId { get; init; }
    public required Guid CreatedBy { get; init; }
    public string? CreatedByName { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }

    public decimal CalculatedTotal => ApprovedAmount ?? (Quantity * EstimatedUnitPrice);
}

public record RequirementDetailDto : RequirementDto
{
    public IReadOnlyCollection<RequirementCommentDto> Comments { get; init; } = Array.Empty<RequirementCommentDto>();
    public IReadOnlyCollection<PrioritizationDecisionDto> Decisions { get; init; } = Array.Empty<PrioritizationDecisionDto>();
}

public record RequirementCommentDto
{
    public Guid Id { get; init; }
    public Guid RequirementId { get; init; }
    public Guid UserId { get; init; }
    public string? UserName { get; init; }
    public string? AreaName { get; init; }
    public DateTime Date { get; init; }
    public required string Text { get; init; }
}

public record AddRequirementCommentDto
{
    public required string Text { get; init; }
}

public record PrioritizationDecisionDto
{
    public Guid Id { get; init; }
    public Guid RequirementId { get; init; }
    public required string RequirementCode { get; init; }
    public Guid? MasterProductId { get; init; }
    public required string Type { get; init; }
    public required decimal RequestedAmount { get; init; }
    public decimal? ApprovedAmount { get; init; }
    public required string Reason { get; init; }
    public required Guid DecidedBy { get; init; }
    public string? DecidedByName { get; init; }
    public required Guid DecidingAreaId { get; init; }
    public required DateTime Date { get; init; }
    public required string Level { get; init; }
}

public record CreateRequirementDto
{
    public required Guid PoaTaskId { get; init; }
    public required Guid MasterProductId { get; init; }
    public required string Description { get; init; }
    public required decimal Quantity { get; init; }
    public required string UnitOfMeasure { get; init; }
    public decimal EstimatedUnitPrice { get; init; }
    public required string Quarter { get; init; }
    public DateTime? RequiredDate { get; init; }
    public string? FundingSource { get; init; }
    public required string Priority { get; init; }
}

public record UpdateRequirementDto
{
    public required Guid MasterProductId { get; init; }
    public required string Description { get; init; }
    public required decimal Quantity { get; init; }
    public required string UnitOfMeasure { get; init; }
    public decimal EstimatedUnitPrice { get; init; }
    public required string Quarter { get; init; }
    public DateTime? RequiredDate { get; init; }
    public string? FundingSource { get; init; }
    public required string Priority { get; init; }
}

public record ConfirmPriceDto
{
    public required decimal Price { get; init; }
}

public record RequirementDecisionDto
{
    /// <summary>Approve | Return | Reschedule</summary>
    public required string Action { get; init; }
    public decimal? ApprovedAmount { get; init; }
    public string? Quarter { get; init; }
    public required string Comment { get; init; }
}

public record ReduceRequirementDto
{
    public required decimal ApprovedAmount { get; init; }
    public required string Reason { get; init; }
}

public record MergeRequirementsDto
{
    public required List<Guid> RequirementIds { get; init; } = new();
    public required string NewDescription { get; init; }
    public string? Reason { get; init; }
}

/// <summary>Tarea POA disponible para originar un requerimiento (selector del front). El Id es el Guid real de PoaTask.</summary>
public record PoaTaskOptionDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductCode { get; init; }
    public required string ProductName { get; init; }
    public required Guid PlanId { get; init; }
    public required Guid CycleId { get; init; }
    public required Guid AreaId { get; init; }
    public required string AreaName { get; init; }
}
