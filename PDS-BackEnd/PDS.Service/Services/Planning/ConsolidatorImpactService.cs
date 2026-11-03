using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PDS.Application.Data;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

public class ConsolidatorImpactService : IConsolidatorImpactService
{
    private readonly IUnitOfWork<PDSDbContext> _unitOfWork;

    public ConsolidatorImpactService(IUnitOfWork<PDSDbContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IHttpResult<ConsolidatorUnitImpactDto>> AnalyzeUnitImpactAsync(int organizationId, Guid unitId)
    {
        var unit = await _unitOfWork.Context.OrganizationalStructures
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.OrganizationId == organizationId && u.Id == unitId);

        if (unit == null)
        {
            return HttpResult<ConsolidatorUnitImpactDto>.Failure(
                "Unidad no encontrada",
                new Dictionary<string, IEnumerable<string>> { { "unitId", new[] { "La unidad organizativa especificada no existe." } } },
                System.Net.HttpStatusCode.NotFound);
        }

        var isConsolidatorArea = await _unitOfWork.Context.ConsolidatorAreaProfiles
            .AsNoTracking()
            .AnyAsync(p => p.OrganizationId == organizationId && p.OrganizationalStructureId == unitId && p.DeletedAt == null);

        var affectedRules = await _unitOfWork.Context.ConsolidatorRoutingRules
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId && r.ConsolidatorAreaId == unitId && r.DeletedAt == null && r.IsActive)
            .Select(r => r.RuleKey)
            .ToListAsync();

        var affectedProductsCount = await _unitOfWork.Context.MasterProducts
            .AsNoTracking()
            .CountAsync(p => p.OrganizationId == organizationId && p.ConsolidatorAreaId == unitId && p.DeletedAt == null);

        var impact = new ConsolidatorUnitImpactDto
        {
            UnitId = unit.Id,
            UnitName = unit.Name,
            Code = unit.Code ?? string.Empty,
            IsConsolidatorArea = isConsolidatorArea,
            AffectedRoutingRulesCount = affectedRules.Count,
            AffectedRuleKeys = affectedRules,
            AffectedMasterProductsCount = affectedProductsCount
        };

        return HttpResult<ConsolidatorUnitImpactDto>.Success(impact);
    }

    public async Task<IHttpResult<StructureVersionImpactReportDto>> AnalyzeVersionImpactAsync(int organizationId, Guid versionId)
    {
        var version = await _unitOfWork.Context.StructureVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.OrganizationId == organizationId && v.Id == versionId);

        if (version == null)
        {
            return HttpResult<StructureVersionImpactReportDto>.Failure(
                "Versión no encontrada",
                new Dictionary<string, IEnumerable<string>> { { "versionId", new[] { "La versión de estructura especificada no existe." } } },
                System.Net.HttpStatusCode.NotFound);
        }

        // Get all consolidator profile area unit IDs
        var consolidatorAreaUnitIds = await _unitOfWork.Context.ConsolidatorAreaProfiles
            .AsNoTracking()
            .Where(p => p.OrganizationId == organizationId && p.DeletedAt == null && p.OrganizationalStructureId.HasValue)
            .Select(p => p.OrganizationalStructureId!.Value)
            .ToListAsync();

        var affectedUnitsList = new List<ConsolidatorUnitImpactDto>();

        foreach (var unitId in consolidatorAreaUnitIds)
        {
            var unitImpactResult = await AnalyzeUnitImpactAsync(organizationId, unitId);
            if (unitImpactResult.IsSuccess && unitImpactResult.Value.HasImpact)
            {
                affectedUnitsList.Add(unitImpactResult.Value);
            }
        }

        var report = new StructureVersionImpactReportDto
        {
            VersionId = version.Id,
            VersionNumber = version.Number,
            VersionName = version.Name,
            Description = version.Description ?? string.Empty,
            TotalConsolidatorAreasAffected = affectedUnitsList.Count,
            TotalRoutingRulesAffected = affectedUnitsList.Sum(u => u.AffectedRoutingRulesCount),
            TotalMasterProductsAffected = affectedUnitsList.Sum(u => u.AffectedMasterProductsCount),
            AffectedUnits = affectedUnitsList
        };

        return HttpResult<StructureVersionImpactReportDto>.Success(report);
    }
}
