using System;
using System.Collections.Generic;

namespace PDS.Domain.DTOs.Planning;

public class MastersoftAccountDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ControlAccountCode { get; set; }
    public Guid? ControlAccountId { get; set; }
    public int Level { get; set; }
    public bool IsPostable { get; set; }
}

public class MastersoftDigecobMappingDto
{
    public Guid Id { get; set; }
    public Guid DigecobAccountId { get; set; }
    public string DigecobAccountCode { get; set; } = string.Empty;
    public string DigecobAccountName { get; set; } = string.Empty;
    public bool DigecobIsPostable { get; set; }
    public int DigecobLevel { get; set; }

    public Guid? MastersoftAccountId { get; set; }
    public string? MastersoftAccountCode { get; set; }
    public string? MastersoftAccountName { get; set; }

    /// <summary>
    /// Indica si el mapeo fue heredado por jerarquía del padre DIGECOB.
    /// </summary>
    public bool IsInherited { get; set; }
    public string? InheritedFromDigecobCode { get; set; }
}

public class CreateMastersoftMappingDto
{
    public Guid DigecobAccountId { get; set; }
    public Guid MastersoftAccountId { get; set; }
}

public class MastersoftImportSummaryDto
{
    public int DigecobAccountsProcessed { get; set; }
    public int MastersoftAccountsCreated { get; set; }
    public int MastersoftAccountsUpdated { get; set; }
    public int MappingsCreated { get; set; }
    public int MappingsUpdated { get; set; }
    public List<string> Warnings { get; set; } = [];
}
