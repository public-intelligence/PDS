using System;
using System.Collections.Generic;

namespace PDS.Domain.DTOs.Planning;

public record ConsolidationBoardDto
{
    public required Guid CycleId { get; init; }
    public required string CycleName { get; init; }
    public required int CycleYear { get; init; }
    public Dictionary<string, int> PlansByStatus { get; init; } = new();
    public decimal TotalApprovedAmount { get; init; }
    public IReadOnlyCollection<ConsolidatorGroupDto> ConsolidatorGroups { get; init; } = Array.Empty<ConsolidatorGroupDto>();
}

public record ConsolidatorGroupDto
{
    public required Guid ConsolidatorAreaId { get; init; }
    public required string ConsolidatorAreaName { get; init; }
    public int RequirementCount { get; init; }
    public decimal TotalAmount { get; init; }
    public Dictionary<string, int> RequirementsByStatus { get; init; } = new();
    public IReadOnlyCollection<RequirementDto> Requirements { get; init; } = Array.Empty<RequirementDto>();
}

public record ProductDemandBoardDto
{
    public required Guid CycleId { get; init; }
    public required string CycleName { get; init; }
    public required int CycleYear { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal TotalApprovedAmount { get; init; }
    public IReadOnlyCollection<ExpenseObjectDemandGroupDto> ExpenseObjectGroups { get; init; } = Array.Empty<ExpenseObjectDemandGroupDto>();
    public IReadOnlyCollection<ProductDemandGroupDto> ProductGroups { get; init; } = Array.Empty<ProductDemandGroupDto>();
}

public record ExpenseObjectDemandGroupDto
{
    public required string ExpenseObjectCode { get; init; }
    public string? ExpenseObjectName { get; init; }
    public decimal TotalQuantity { get; init; }
    public int RequirementCount { get; init; }
    public int RequestingAreaCount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal TotalApprovedAmount { get; init; }
    public decimal? CeilingInitialAmount { get; init; }
    public decimal? CeilingCurrentAmount { get; init; }
    public decimal? CeilingAvailableAmount { get; init; }
    public decimal? CeilingUtilizationPercentage { get; init; }
    public string? CeilingStatus { get; init; }
    public IReadOnlyCollection<ProductDemandGroupDto> ProductGroups { get; init; } = Array.Empty<ProductDemandGroupDto>();
    public IReadOnlyCollection<RequirementDto> Requirements { get; init; } = Array.Empty<RequirementDto>();
}

public record ProductDemandGroupDto
{
    public required Guid MasterProductId { get; init; }
    public string? MasterProductCode { get; init; }
    public required string MasterProductName { get; init; }
    public string? UnspscCode { get; init; }
    public string? ExpenseObjectCode { get; init; }
    public string? ExpenseObjectName { get; init; }
    public string? AccountingAccountCode { get; init; }
    public decimal TotalQuantity { get; init; }
    public int RequirementCount { get; init; }
    public int RequestingAreaCount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal TotalApprovedAmount { get; init; }
    public decimal? CeilingInitialAmount { get; init; }
    public decimal? CeilingCurrentAmount { get; init; }
    public decimal? CeilingAvailableAmount { get; init; }
    public decimal? CeilingUtilizationPercentage { get; init; }
    public string? CeilingStatus { get; init; }
    public IReadOnlyCollection<RequirementDto> Requirements { get; init; } = Array.Empty<RequirementDto>();
}

public record ExpenseObjectsBoardPagedDto
{
    public required Guid CycleId { get; init; }
    public required string CycleName { get; init; }
    public required int CycleYear { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal TotalApprovedAmount { get; init; }
    public int TotalRequirements { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public IReadOnlyCollection<ExpenseObjectDemandGroupDto> ExpenseObjectGroups { get; init; } = Array.Empty<ExpenseObjectDemandGroupDto>();
}

public record ProductsBoardPagedDto
{
    public required Guid CycleId { get; init; }
    public required string CycleName { get; init; }
    public required int CycleYear { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal TotalApprovedAmount { get; init; }
    public int TotalRequirements { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public IReadOnlyCollection<ProductDemandGroupDto> ProductGroups { get; init; } = Array.Empty<ProductDemandGroupDto>();
}

public record ConsolidatorAreasBoardPagedDto
{
    public required Guid CycleId { get; init; }
    public required string CycleName { get; init; }
    public required int CycleYear { get; init; }
    public Dictionary<string, int> PlansByStatus { get; init; } = new();
    public decimal TotalApprovedAmount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public IReadOnlyCollection<ConsolidatorGroupDto> ConsolidatorGroups { get; init; } = Array.Empty<ConsolidatorGroupDto>();
}

