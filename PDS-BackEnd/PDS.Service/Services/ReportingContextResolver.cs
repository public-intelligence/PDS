using Microsoft.Extensions.Logging;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.OrgChart;
using PDS.Domain.DTOs.Reporting;
using PDS.Domain.DTOs.StructureVersion;
using PDS.Domain.Models;
using PDS.Domain.Response;
using System.Net;

namespace PDS.Service.Services;

public class ReportingContextResolver(
    ILogger<ReportingContextResolver> logger,
    IOrgChartService orgChartService,
    IRepository<StructureVersionPosition> versionPositionRepository
) : IReportingContextResolver
{
    private readonly ILogger<ReportingContextResolver> _logger = logger;
    private readonly IOrgChartService _orgChartService = orgChartService;
    private readonly IRepository<StructureVersionPosition> _versionPositionRepository = versionPositionRepository;

    public async Task<IHttpResult<ResolvedReportingContext>> ResolveAsync(
        int organizationId, ReportingContextDto context, string? generatedByName)
    {
        // Resolución a través de la capa de versionado (no se duplica la lógica de reconstrucción).
        IHttpResult<OrgChartDto> chartResult;
        if (context.VersionId is { } versionId)
            chartResult = await _orgChartService.GetByVersionIdAsync(
                organizationId, versionId, null, null, includePositions: true, requirePublic: false);
        else if (context.AsOfDate is { } asOf)
            chartResult = await _orgChartService.GetAsOfAsync(
                organizationId, asOf, null, null, includePositions: true, requirePublic: false);
        else
            chartResult = await _orgChartService.GetCurrentAsync(
                organizationId, null, null, includePositions: true, requirePublic: false);

        if (!chartResult.IsSuccess)
            return HttpResult<ResolvedReportingContext>.Failure(
                chartResult.Error.Title, chartResult.Error.Detail, chartResult.StatusCode);

        var chart = chartResult.Value;
        var salary = await LoadSalaryAsync(chart.Version.Id);
        var label = BuildLabel(chart.Version, context, generatedByName);

        return HttpResult<ResolvedReportingContext>.Success(new ResolvedReportingContext
        {
            Chart = chart,
            Label = label,
            SalaryByPositionId = salary
        });
    }

    /// <summary>Carga la fotografía de salario/grado de la versión (vacío si la versión no está publicada).</summary>
    private async Task<IReadOnlyDictionary<Guid, PositionSalary>> LoadSalaryAsync(Guid versionId)
    {
        var result = await _versionPositionRepository.FilterAsync(p => p.StructureVersionId == versionId);
        if (!result.IsSuccess)
            return new Dictionary<Guid, PositionSalary>();

        var map = new Dictionary<Guid, PositionSalary>();
        foreach (var p in result.Value)
            map[p.PositionId] = new PositionSalary(p.Grade, p.MinSalary, p.MaxSalary);
        return map;
    }

    private static ReportContextLabelDto BuildLabel(
        StructureVersionSummaryDto version, ReportingContextDto context, string? generatedByName)
    {
        var resolvedAsOf = context.AsOfDate
            ?? version.EffectiveFrom
            ?? DateOnly.FromDateTime(DateTime.UtcNow);

        return new ReportContextLabelDto
        {
            VersionId = version.Id,
            VersionNumber = version.Number,
            VersionName = version.Name,
            Status = version.Status,
            EffectiveFrom = version.EffectiveFrom,
            EffectiveTo = version.EffectiveTo,
            ResolvedAsOf = resolvedAsOf,
            GeneratedAtUtc = DateTime.UtcNow,
            GeneratedByName = generatedByName
        };
    }

    /// <summary>
    /// Roll-up jerárquico O(N+P) sin N+1: figuras propias en una pasada y consolidadas (subárbol)
    /// acumulando de hijos a padres en orden de nivel descendente. La lista plana ya trae Level y
    /// ParentSourceUnitId, así que no hay recorridos repetidos del árbol.
    /// </summary>
    public static IReadOnlyDictionary<Guid, UnitAggregate> Rollup(ICollection<OrgChartNodeDto> nodes)
    {
        var agg = new Dictionary<Guid, UnitAggregate>(nodes.Count);
        foreach (var node in nodes)
        {
            var ownPositions = node.Positions.Count;
            var ownPosts = node.Positions.Sum(p => p.AuthorizedPosts);
            agg[node.SourceUnitId] = new UnitAggregate
            {
                Node = node,
                OwnDistinctPositions = ownPositions,
                OwnAuthorizedPosts = ownPosts,
                ConsolidatedDistinctPositions = ownPositions,
                ConsolidatedAuthorizedPosts = ownPosts,
                HasSupervisorDefined = node.Positions.Any(p => p.IsSupervisor)
            };
        }

        // De hijos a padres: al procesar un nodo, sus descendientes (nivel mayor) ya sumaron en él.
        foreach (var unit in agg.Values.OrderByDescending(a => a.Node.Level))
        {
            var parentId = unit.Node.ParentSourceUnitId;
            if (parentId.HasValue && agg.TryGetValue(parentId.Value, out var parent))
            {
                parent.ConsolidatedDistinctPositions += unit.ConsolidatedDistinctPositions;
                parent.ConsolidatedAuthorizedPosts += unit.ConsolidatedAuthorizedPosts;
            }
        }

        return agg;
    }
}
