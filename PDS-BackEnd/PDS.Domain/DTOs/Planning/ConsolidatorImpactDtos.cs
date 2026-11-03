using System;
using System.Collections.Generic;

namespace PDS.Domain.DTOs.Planning;

public class ConsolidatorUnitImpactDto
{
    public Guid UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsConsolidatorArea { get; set; }
    public int AffectedRoutingRulesCount { get; set; }
    public List<string> AffectedRuleKeys { get; set; } = new();
    public int AffectedMasterProductsCount { get; set; }
    public bool HasImpact => IsConsolidatorArea && (AffectedRoutingRulesCount > 0 || AffectedMasterProductsCount > 0);
}

public class StructureVersionImpactReportDto
{
    public Guid VersionId { get; set; }
    public int? VersionNumber { get; set; }
    public string VersionName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TotalConsolidatorAreasAffected { get; set; }
    public int TotalRoutingRulesAffected { get; set; }
    public int TotalMasterProductsAffected { get; set; }
    public List<ConsolidatorUnitImpactDto> AffectedUnits { get; set; } = new();
    public bool RequiresReevaluation => TotalConsolidatorAreasAffected > 0;
}
