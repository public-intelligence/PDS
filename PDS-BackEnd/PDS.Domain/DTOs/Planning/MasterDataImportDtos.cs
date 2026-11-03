using System;
using System.Collections.Generic;

namespace PDS.Domain.DTOs.Planning;

public record MasterDataImportResultDto
{
    public bool Applied { get; init; }
    public required SheetImportSummaryDto AreasSummary { get; init; }
    public required SheetImportSummaryDto UnspscSummary { get; init; }
    public required SheetImportSummaryDto ExpenseObjectsSummary { get; init; }
    public required SheetImportSummaryDto AccountingAccountsSummary { get; init; }
    public required SheetImportSummaryDto RoutingRulesSummary { get; init; }
    public required SheetImportSummaryDto MasterProductsSummary { get; init; }
    public IReadOnlyCollection<string> Warnings { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();
}

public record SheetImportSummaryDto
{
    public required string SheetName { get; init; }
    public int TotalRows { get; init; }
    public int Added { get; init; }
    public int Updated { get; init; }
    public int Unchanged { get; init; }
    public int Disabled { get; init; }
}
