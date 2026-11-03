using System;
using System.Collections.Generic;

namespace PDS.Domain.DTOs.Planning;

public record MasterProductDto
{
    public required Guid Id { get; init; }
    public required int OrganizationId { get; init; }
    public Guid? SourceId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? OriginalDescription { get; init; }
    public string? EnglishName { get; init; }
    public string? Definition { get; init; }
    public Guid? UnifiedCodeId { get; init; }
    public string? UnspscCode { get; init; }
    public string? UnspscName { get; init; }
    public string? ItemType { get; init; }
    public string? ItemSubtype { get; init; }
    public string? AssetTreatment { get; init; }
    public string? MeasureUnit { get; init; }
    public decimal? ReferencePrice { get; init; }
    public string? Currency { get; init; }
    public string? PriceSource { get; init; }
    public Guid? ExpenseObjectId { get; init; }
    public string? ExpenseObjectCode { get; init; }
    public string? ExpenseObjectName { get; init; }
    public Guid? AccountingAccountId { get; init; }
    public string? AccountingAccountCode { get; init; }
    public string? AccountingAccountName { get; init; }
    public Guid? ConsolidatorAreaId { get; init; }
    public string? ConsolidatorAreaName { get; init; }
    public required string ConsolidatorAreaSource { get; init; }
    public Guid? ConsolidatorRuleId { get; init; }
    public string? ConsolidatorRuleKey { get; init; }
    public int? ConsolidatorRuleVersion { get; init; }
    public int? ConsolidatorAreaConfidence { get; init; }
    public bool AreaAmbiguity { get; init; }
    public string? AlternativeAreasJson { get; init; }
    public bool RequiresReview { get; init; }
    public string? ReviewReason { get; init; }
    public string? Notes { get; init; }
    public required string State { get; init; }
    public Guid? DuplicateOfMasterProductId { get; init; }
    public Guid? RequestedBy { get; init; }
    public string? RequestedByName { get; init; }
    public Guid? RequestedAreaId { get; init; }
    public DateTime? RequestedAt { get; init; }
    public string? RequestJustification { get; init; }
    public int Version { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record MasterProductDetailDto : MasterProductDto
{
    public IReadOnlyCollection<MasterProductAliasDto> Aliases { get; init; } = Array.Empty<MasterProductAliasDto>();
}

public record MasterProductAliasDto
{
    public Guid Id { get; init; }
    public Guid MasterProductId { get; init; }
    public required string Kind { get; init; }
    public string? System { get; init; }
    public required string Text { get; init; }
}

public record MasterProductSearchItemDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? UnspscCode { get; init; }
    public string? UnspscName { get; init; }
    public string? ItemType { get; init; }
    public string? MeasureUnit { get; init; }
    public decimal? ReferencePrice { get; init; }
    public Guid? ExpenseObjectId { get; init; }
    public string? ExpenseObjectCode { get; init; }
    public string? ExpenseObjectName { get; init; }
    public Guid? AccountingAccountId { get; init; }
    public string? AccountingAccountCode { get; init; }
    public string? AccountingAccountName { get; init; }
    public Guid? ConsolidatorAreaId { get; init; }
    public string? ConsolidatorAreaName { get; init; }
}

public record MasterProductMappingSummaryDto
{
    public int TotalProducts { get; init; }
    public int ActiveProducts { get; init; }
    public int PendingProducts { get; init; }
    public int RequestedProducts { get; init; }
    public int DisabledProducts { get; init; }
    public int MissingExpenseObject { get; init; }
    public int MissingAccountingAccount { get; init; }
    public int MissingConsolidatorArea { get; init; }
    public int RequiresReviewCount { get; init; }
    public int AreaAmbiguityCount { get; init; }
    public int CompleteMappingCount { get; init; }
}

public record CreateMasterProductDto
{
    public required string Name { get; init; }
    public string? OriginalDescription { get; init; }
    public string? EnglishName { get; init; }
    public string? Definition { get; init; }
    public Guid? UnifiedCodeId { get; init; }
    public string? ItemType { get; init; }
    public string? MeasureUnit { get; init; }
    public decimal? ReferencePrice { get; init; }
    public Guid? ExpenseObjectId { get; init; }
    public Guid? AccountingAccountId { get; init; }
    public Guid? ConsolidatorAreaId { get; init; }
    public List<string>? Synonyms { get; init; }
}

public record UpdateMasterProductDto
{
    public required string Name { get; init; }
    public string? OriginalDescription { get; init; }
    public string? EnglishName { get; init; }
    public string? Definition { get; init; }
    public Guid? UnifiedCodeId { get; init; }
    public string? ItemType { get; init; }
    public string? MeasureUnit { get; init; }
    public decimal? ReferencePrice { get; init; }
    public List<string>? Synonyms { get; init; }
}

public record UpdateMasterProductMappingDto
{
    public string? ItemType { get; init; }
    public Guid? ExpenseObjectId { get; init; }
    public Guid? AccountingAccountId { get; init; }
    public Guid? ConsolidatorAreaId { get; init; }
    public bool RatifyConsolidatorArea { get; init; }
    public string? State { get; init; }
    public string? MeasureUnit { get; init; }
    public decimal? ReferencePrice { get; init; }
    public string? Notes { get; init; }
}

public record RatifyConsolidatorAreaDto
{
    public required Guid ConsolidatorAreaId { get; init; }
    public string? Notes { get; init; }
}

public record BulkRatifyByUnspscDto
{
    public required string UnspscPrefix { get; init; }
    public required Guid ConsolidatorAreaId { get; init; }
    public Guid? ExpenseObjectId { get; init; }
    public Guid? AccountingAccountId { get; init; }
    public string? Justification { get; init; }
}

public record RequestMasterProductDto
{
    public required string ProposedName { get; init; }
    public required string Justification { get; init; }
    public string? MeasureUnit { get; init; }
    public decimal? EstimatedQuantity { get; init; }
    public string? ItemType { get; init; }
}
