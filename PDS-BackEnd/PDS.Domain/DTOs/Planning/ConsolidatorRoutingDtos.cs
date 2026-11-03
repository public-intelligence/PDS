using System;
using System.Collections.Generic;

namespace PDS.Domain.DTOs.Planning;

public record ConsolidatorRoutingRuleDto
{
    public required Guid Id { get; init; }
    public required string RuleKey { get; init; }
    public int Version { get; init; }
    public string? Author { get; init; }
    public DateTime? RuleDate { get; init; }
    public int? SupersedesVersion { get; init; }
    public bool Reversible { get; init; }
    public int Priority { get; init; }
    public string? Keywords { get; init; }
    public string? ExclusionKeywords { get; init; }
    public string? UnspscPrefixes { get; init; }
    public string? ItemType { get; init; }
    public required string ConsolidatorAreaName { get; init; }
    public Guid? ConsolidatorAreaId { get; init; }
    public string? Justification { get; init; }
    public bool IsActive { get; init; }
    public bool InstitutionallyValidated { get; init; }
    public int BaseConfidence { get; init; }
    public bool ForceReview { get; init; }
    public required string RuleState { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateConsolidatorRoutingRuleDto
{
    public required string RuleKey { get; init; }
    public int Priority { get; init; } = 100;
    public string? Keywords { get; init; }
    public string? ExclusionKeywords { get; init; }
    public string? UnspscPrefixes { get; init; }
    public string? ItemType { get; init; }
    public required string ConsolidatorAreaName { get; init; }
    public Guid? ConsolidatorAreaId { get; init; }
    public string? Justification { get; init; }
    public bool InstitutionallyValidated { get; init; }
    public int BaseConfidence { get; init; } = 80;
    public bool ForceReview { get; init; }
}

public record UpdateConsolidatorRoutingRuleMetadataDto
{
    public string? Justification { get; init; }
    public bool IsActive { get; init; }
    public string? RuleState { get; init; }
}

public record CreateNewRuleVersionDto : CreateConsolidatorRoutingRuleDto
{
}

public record RoutingProposalDto
{
    public Guid? ConsolidatorAreaId { get; init; }
    public string? ConsolidatorAreaName { get; init; }
    public required string ConsolidatorAreaSource { get; init; }
    public Guid? RuleId { get; init; }
    public string? RuleKey { get; init; }
    public int? RuleVersion { get; init; }
    public int Confidence { get; init; }
    public bool AreaAmbiguity { get; init; }
    public IReadOnlyCollection<AlternativeAreaProposalDto> AlternativeAreas { get; init; } = Array.Empty<AlternativeAreaProposalDto>();
    public bool RequiresReview { get; init; }
    public string? ReviewReason { get; init; }
}

public record AlternativeAreaProposalDto
{
    public Guid? ConsolidatorAreaId { get; init; }
    public required string ConsolidatorAreaName { get; init; }
    public required string RuleKey { get; init; }
    public int RulePriority { get; init; }
}

public record RulePreviewResultDto
{
    public required Guid RuleId { get; init; }
    public required string RuleKey { get; init; }
    public int MatchedProductsCount { get; init; }
    public IReadOnlyCollection<MasterProductDto> SampleMatchedProducts { get; init; } = Array.Empty<MasterProductDto>();
}
