using System;
using System.Collections.Generic;

namespace PDS.Domain.DTOs.Planning;

public record ExpenseObjectDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int Level { get; init; }
    public string? ParentCode { get; init; }
    public Guid? ParentId { get; init; }
    public bool IsDetail { get; init; }
}

public record AccountingAccountDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public bool IsPostable { get; init; }
    public int Level { get; init; }
    public string? ParentCode { get; init; }
    public Guid? ParentId { get; init; }
    public string? ExtendedDescription { get; init; }

    public string? MastersoftAccountCode { get; init; }
    public string? MastersoftAccountName { get; init; }
    public bool IsMastersoftInherited { get; init; }
    public string? InheritedFromDigecobCode { get; init; }
}

public record ConsolidatorAreaProfileDto
{
    public required Guid Id { get; init; }
    public required string AreaCode { get; init; }
    public string? OrganicGroup { get; init; }
    public required string AreaName { get; init; }
    public Guid? OrganizationalStructureId { get; init; }
    public string? MandateSummary { get; init; }
    public string? ConsolidatorKind { get; init; }
}
