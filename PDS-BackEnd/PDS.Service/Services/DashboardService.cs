using Microsoft.Extensions.Logging;
using PDS.Domain.DTOs.Reporting;
using PDS.Domain.DTOs.StructureVersion;
using PDS.Domain.Enums;
using PDS.Domain.Response;

namespace PDS.Service.Services;

// RF-RE-07: KPIs definicionales (sin ocupación). Reemplaza "vacantes críticas" por alertas de calidad.
public class DashboardService(
    ILogger<DashboardService> logger,
    IReportingContextResolver resolver,
    IStructureVersionService versionService
) : IDashboardService
{
    private const SalaryBasis DefaultBasis = SalaryBasis.Midpoint;

    private readonly ILogger<DashboardService> _logger = logger;
    private readonly IReportingContextResolver _resolver = resolver;
    private readonly IStructureVersionService _versionService = versionService;

    public async Task<IHttpResult<DashboardDto>> GetAsync(
        int organizationId, ReportingContextDto context, string? generatedByName)
    {
        var resolution = await _resolver.ResolveAsync(organizationId, context, generatedByName);
        if (!resolution.IsSuccess)
            return HttpResult<DashboardDto>.Failure(
                resolution.Error.Title, resolution.Error.Detail, resolution.StatusCode);

        var resolved = resolution.Value;
        var nodes = resolved.Nodes;
        var salary = resolved.SalaryByPositionId;

        // Unidades y plazas por nivel.
        var byLevel = nodes
            .GroupBy(n => n.Level)
            .OrderBy(g => g.Key)
            .Select(g => new LevelCountDto
            {
                Level = g.Key,
                Label = $"Nivel {g.Key}",
                Count = g.Count(),
                AuthorizedPosts = g.Sum(n => n.Positions.Sum(p => p.AuthorizedPosts))
            })
            .ToList();

        // Cargos con su grado (categoría salarial) y grupo ocupacional.
        var positions = nodes
            .SelectMany(n => n.Positions.Select(p => new
            {
                p.PositionId,
                p.AuthorizedPosts,
                p.OccupationalGroupName,
                Grade = salary.TryGetValue(p.PositionId, out var s) ? s.Grade : 0
            }))
            .ToList();

        var bySalaryCategory = positions
            .GroupBy(x => x.Grade)
            .OrderBy(g => g.Key)
            .Select(g => new CategoryCountDto
            {
                Key = $"G{g.Key}",
                Label = $"Grado {g.Key}",
                DistinctPositions = g.Select(x => x.PositionId).Distinct().Count(),
                AuthorizedPosts = g.Sum(x => x.AuthorizedPosts)
            })
            .ToList();

        var byOccupationalGroup = positions
            .GroupBy(x => x.OccupationalGroupName ?? "(Sin grupo)")
            .OrderBy(g => g.Key)
            .Select(g => new CategoryCountDto
            {
                Key = g.Key,
                Label = g.Key,
                DistinctPositions = g.Select(x => x.PositionId).Distinct().Count(),
                AuthorizedPosts = g.Sum(x => x.AuthorizedPosts)
            })
            .ToList();

        var (totalCost, _) = ReportCostCalculator.Total(nodes, salary, DefaultBasis);

        // Versiones: pendientes de aprobación + predecesor para los cambios recientes.
        var listResult = await _versionService.ListAsync(organizationId, null);
        var versions = listResult.IsSuccess ? listResult.Value.ToList() : new List<StructureVersionSummaryDto>();
        var pendingCount = versions.Count(v => v.Status == VersionStatus.InReview || v.Status == VersionStatus.Approved);

        var recent = await BuildRecentChangesAsync(organizationId, resolved.Label, versions);

        var dashboard = new DashboardDto
        {
            Context = resolved.Label,
            ActiveUnitsByLevel = byLevel,
            TotalActiveUnits = nodes.Count,
            TotalDefinedPositions = positions.Count,
            TotalAuthorizedPosts = positions.Sum(x => x.AuthorizedPosts),
            AuthorizedPostsByLevel = byLevel,
            AuthorizedPostsBySalaryCategory = bySalaryCategory,
            AuthorizedPostsByOccupationalGroup = byOccupationalGroup,
            EstimatedTotalCost = totalCost,
            CostIsEstimate = true,
            CostBasis = DefaultBasis,
            PendingApprovalCount = pendingCount,
            RecentStructuralChanges = recent,
            QualityAlerts = BuildQualityAlerts(resolved)
        };

        return HttpResult<DashboardDto>.Success(dashboard);
    }

    private async Task<StructuralChangeSummaryDto> BuildRecentChangesAsync(
        int organizationId, ReportContextLabelDto label, List<StructureVersionSummaryDto> versions)
    {
        var empty = new StructuralChangeSummaryDto();
        if (!label.VersionNumber.HasValue) return empty;

        var predecessor = versions
            .Where(v => v.Number.HasValue && v.Number < label.VersionNumber
                && (v.Status == VersionStatus.Published || v.Status == VersionStatus.Superseded))
            .OrderByDescending(v => v.Number)
            .FirstOrDefault();
        if (predecessor is null) return empty;

        var comparison = await _versionService.CompareAsync(organizationId, predecessor.Id, null, label.VersionId, null);
        if (!comparison.IsSuccess) return empty;

        var s = comparison.Value.Summary;
        return new StructuralChangeSummaryDto
        {
            CreatedUnits = s.CreatedCount,
            ModifiedUnits = s.ModifiedCount,
            RelocatedUnits = s.RelocatedCount,
            DeletedUnits = s.DeletedCount,
            FromVersionId = predecessor.Id,
            ToVersionId = label.VersionId
        };
    }

    // Alertas de calidad definicional (reemplazo en alcance de "vacantes críticas").
    private static List<QualityAlertDto> BuildQualityAlerts(ResolvedReportingContext resolved)
    {
        var alerts = new List<QualityAlertDto>();
        var nodes = resolved.Nodes;
        var salary = resolved.SalaryByPositionId;

        var unitsNoPosts = nodes.Count(n => n.Positions.Sum(p => p.AuthorizedPosts) == 0);
        if (unitsNoPosts > 0)
            alerts.Add(new QualityAlertDto
            {
                Code = "UnitNoAuthorizedPosts",
                Message = "Unidades sin plazas autorizadas definidas.",
                Count = unitsNoPosts
            });

        var unitsNoSupervisor = nodes.Count(n => !n.Positions.Any(p => p.IsSupervisor));
        if (unitsNoSupervisor > 0)
            alerts.Add(new QualityAlertDto
            {
                Code = "UnitMissingSupervisor",
                Message = "Unidades sin un cargo supervisor definido.",
                Count = unitsNoSupervisor
            });

        var positionsZeroPosts = nodes.SelectMany(n => n.Positions).Count(p => p.AuthorizedPosts == 0);
        if (positionsZeroPosts > 0)
            alerts.Add(new QualityAlertDto
            {
                Code = "PositionZeroAuthorizedPosts",
                Message = "Cargos con cero plazas autorizadas.",
                Count = positionsZeroPosts
            });

        var positionsNoCategory = nodes
            .SelectMany(n => n.Positions)
            .Count(p => !(salary.TryGetValue(p.PositionId, out var s) && s.Grade > 0));
        if (positionsNoCategory > 0)
            alerts.Add(new QualityAlertDto
            {
                Code = "PositionNoSalaryCategory",
                Message = "Cargos sin categoría salarial (grado) asignada.",
                Count = positionsNoCategory
            });

        return alerts;
    }
}
