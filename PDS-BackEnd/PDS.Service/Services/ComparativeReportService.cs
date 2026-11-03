using Microsoft.Extensions.Logging;
using PDS.Domain.DTOs.Reporting;
using PDS.Domain.Enums;
using PDS.Domain.Response;

namespace PDS.Service.Services;

// RF-RE-04: reutiliza CompareAsync (RF-VH-03) para el diff de unidades y agrega los deltas de
// cargos/plazas y el cambio en el costo estimado como capa de agregación.
public class ComparativeReportService(
    ILogger<ComparativeReportService> logger,
    IStructureVersionService versionService,
    IReportingContextResolver resolver
) : IComparativeReportService
{
    private readonly ILogger<ComparativeReportService> _logger = logger;
    private readonly IStructureVersionService _versionService = versionService;
    private readonly IReportingContextResolver _resolver = resolver;

    public async Task<IHttpResult<ComparativeReportDto>> GetAsync(
        int organizationId, Guid? fromVersionId, DateOnly? fromDate,
        Guid? toVersionId, DateOnly? toDate, SalaryBasis basis, string? generatedByName)
    {
        // 1) Diff de unidades (reutilización directa de RF-VH-03).
        var comparison = await _versionService.CompareAsync(organizationId, fromVersionId, fromDate, toVersionId, toDate);
        if (!comparison.IsSuccess)
            return HttpResult<ComparativeReportDto>.Failure(
                comparison.Error.Title, comparison.Error.Detail, comparison.StatusCode);

        // 2) Resolución de ambos contextos (organigrama + salario fotografiado).
        var fromResolution = await _resolver.ResolveAsync(
            organizationId, new ReportingContextDto { VersionId = fromVersionId, AsOfDate = fromDate }, generatedByName);
        if (!fromResolution.IsSuccess)
            return HttpResult<ComparativeReportDto>.Failure(
                fromResolution.Error.Title, fromResolution.Error.Detail, fromResolution.StatusCode);

        var toResolution = await _resolver.ResolveAsync(
            organizationId, new ReportingContextDto { VersionId = toVersionId, AsOfDate = toDate }, generatedByName);
        if (!toResolution.IsSuccess)
            return HttpResult<ComparativeReportDto>.Failure(
                toResolution.Error.Title, toResolution.Error.Detail, toResolution.StatusCode);

        var from = fromResolution.Value;
        var to = toResolution.Value;

        var fromMap = BuildPositionMap(from);
        var toMap = BuildPositionMap(to);
        var positionDeltas = BuildPositionDeltas(fromMap, toMap);

        // 3) Delta de costo estimado (cada lado con su propio salario fotografiado).
        var fromCost = ReportCostCalculator.Total(from.Nodes, from.SalaryByPositionId, basis).Total;
        var toCost = ReportCostCalculator.Total(to.Nodes, to.SalaryByPositionId, basis).Total;
        var costDelta = new CostDeltaDto
        {
            Basis = basis,
            FromEstimatedCost = fromCost,
            ToEstimatedCost = toCost,
            CostDelta = toCost - fromCost,
            IsEstimate = true
        };

        return HttpResult<ComparativeReportDto>.Success(new ComparativeReportDto
        {
            FromContext = from.Label,
            ToContext = to.Label,
            UnitComparison = comparison.Value,
            PositionDeltas = positionDeltas,
            CostDelta = costDelta
        });
    }

    private sealed record PositionEntry(int AuthorizedPosts, string Name, Guid UnitId);

    private static Dictionary<Guid, PositionEntry> BuildPositionMap(ResolvedReportingContext ctx)
    {
        var map = new Dictionary<Guid, PositionEntry>();
        foreach (var node in ctx.Nodes)
            foreach (var pos in node.Positions)
                map[pos.PositionId] = new PositionEntry(pos.AuthorizedPosts, pos.Name, node.SourceUnitId);
        return map;
    }

    private static PositionDeltaSummaryDto BuildPositionDeltas(
        IReadOnlyDictionary<Guid, PositionEntry> fromMap, IReadOnlyDictionary<Guid, PositionEntry> toMap)
    {
        var changes = new List<PositionDeltaDto>();
        int added = 0, removed = 0, postsChanged = 0;

        foreach (var (id, entry) in toMap)
        {
            if (fromMap.ContainsKey(id)) continue;
            added++;
            changes.Add(new PositionDeltaDto
            {
                PositionId = id,
                PositionName = entry.Name,
                UnitId = entry.UnitId,
                ChangeType = "Added",
                FromAuthorizedPosts = 0,
                ToAuthorizedPosts = entry.AuthorizedPosts,
                AuthorizedPostsDelta = entry.AuthorizedPosts
            });
        }

        foreach (var (id, entry) in fromMap)
        {
            if (toMap.ContainsKey(id)) continue;
            removed++;
            changes.Add(new PositionDeltaDto
            {
                PositionId = id,
                PositionName = entry.Name,
                UnitId = entry.UnitId,
                ChangeType = "Removed",
                FromAuthorizedPosts = entry.AuthorizedPosts,
                ToAuthorizedPosts = 0,
                AuthorizedPostsDelta = -entry.AuthorizedPosts
            });
        }

        foreach (var (id, fromEntry) in fromMap)
        {
            if (!toMap.TryGetValue(id, out var toEntry)) continue;
            if (fromEntry.AuthorizedPosts == toEntry.AuthorizedPosts) continue;
            postsChanged++;
            changes.Add(new PositionDeltaDto
            {
                PositionId = id,
                PositionName = toEntry.Name,
                UnitId = toEntry.UnitId,
                ChangeType = "PostsChanged",
                FromAuthorizedPosts = fromEntry.AuthorizedPosts,
                ToAuthorizedPosts = toEntry.AuthorizedPosts,
                AuthorizedPostsDelta = toEntry.AuthorizedPosts - fromEntry.AuthorizedPosts
            });
        }

        var fromTotal = fromMap.Values.Sum(e => e.AuthorizedPosts);
        var toTotal = toMap.Values.Sum(e => e.AuthorizedPosts);

        return new PositionDeltaSummaryDto
        {
            AddedCount = added,
            RemovedCount = removed,
            PostCountChangedCount = postsChanged,
            FromTotalAuthorizedPosts = fromTotal,
            ToTotalAuthorizedPosts = toTotal,
            AuthorizedPostsDelta = toTotal - fromTotal,
            Changes = changes
        };
    }
}
