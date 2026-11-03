using Microsoft.Extensions.Logging;
using PDS.Domain.DTOs.Reporting;
using PDS.Domain.Enums;
using PDS.Domain.Response;

namespace PDS.Service.Services;

// RF-RE-03: costo estimado. Salario tomado de la fotografía de la versión (StructureVersionPosition).
public class EstimatedCostReportService(
    ILogger<EstimatedCostReportService> logger,
    IReportingContextResolver resolver
) : IEstimatedCostReportService
{
    private const string FidelityNote =
        "Costo estimado (salario × plazas autorizadas), no nómina real. El salario y el grado se " +
        "fotografían al publicar cada versión, por lo que reflejan los valores vigentes en esa versión.";

    private readonly ILogger<EstimatedCostReportService> _logger = logger;
    private readonly IReportingContextResolver _resolver = resolver;

    public async Task<IHttpResult<EstimatedCostReportDto>> GetAsync(
        int organizationId, ReportingContextDto context, SalaryBasis basis, ReportGrouping groupBy, string? generatedByName)
    {
        var resolution = await _resolver.ResolveAsync(organizationId, context, generatedByName);
        if (!resolution.IsSuccess)
            return HttpResult<EstimatedCostReportDto>.Failure(
                resolution.Error.Title, resolution.Error.Detail, resolution.StatusCode);

        var resolved = resolution.Value;
        var salary = resolved.SalaryByPositionId;

        var rows = new List<EstimatedCostRowDto>();
        var unresolved = 0;
        foreach (var node in resolved.Nodes)
        {
            foreach (var pos in node.Positions)
            {
                PositionSalary? s = salary.TryGetValue(pos.PositionId, out var v) ? v : null;
                var unitSalary = s?.Unit(basis);
                var lineCost = ReportCostCalculator.LineCost(s, pos.AuthorizedPosts, basis);
                var grade = s?.Grade ?? 0;
                if (lineCost is null) unresolved++;

                var sel = ReportGroupSelector.Select(
                    groupBy, node.SourceUnitId, node.Code, node.Name, node.Level, grade, pos.OccupationalGroupName);

                rows.Add(new EstimatedCostRowDto
                {
                    GroupKey = sel.Key,
                    UnitId = node.SourceUnitId,
                    UnitCode = node.Code,
                    UnitName = node.Name,
                    Level = node.Level,
                    PositionId = pos.PositionId,
                    PositionName = pos.Name,
                    OccupationalGroupName = pos.OccupationalGroupName,
                    Grade = grade,
                    AuthorizedPosts = pos.AuthorizedPosts,
                    UnitSalary = unitSalary,
                    LineCost = lineCost,
                    SalaryResolved = lineCost is not null
                });
            }
        }

        var subtotals = rows
            .GroupBy(r => r.GroupKey)
            .Select(g =>
            {
                var first = g.First();
                var sel = ReportGroupSelector.Select(
                    groupBy, first.UnitId, first.UnitCode, first.UnitName, first.Level, first.Grade, first.OccupationalGroupName);
                return new
                {
                    sel.Sort,
                    Subtotal = new ReportSubtotalDto
                    {
                        GroupKey = sel.Key,
                        GroupLabel = sel.Label,
                        DistinctPositionCount = g.Select(r => r.PositionId).Distinct().Count(),
                        AuthorizedPosts = g.Sum(r => r.AuthorizedPosts),
                        EstimatedCost = g.Sum(r => r.LineCost ?? 0m)
                    }
                };
            })
            .OrderBy(x => x.Sort).ThenBy(x => x.Subtotal.GroupLabel)
            .Select(x => x.Subtotal)
            .ToList();

        var grandTotal = new ReportSubtotalDto
        {
            GroupKey = "__total__",
            GroupLabel = "Total general",
            DistinctPositionCount = rows.Select(r => r.PositionId).Distinct().Count(),
            AuthorizedPosts = rows.Sum(r => r.AuthorizedPosts),
            EstimatedCost = rows.Sum(r => r.LineCost ?? 0m)
        };

        var groupOrder = subtotals.Select((s, i) => (s.GroupKey, i)).ToDictionary(x => x.GroupKey, x => x.i);
        var orderedRows = rows
            .OrderBy(r => groupOrder.TryGetValue(r.GroupKey ?? "", out var i) ? i : int.MaxValue)
            .ThenBy(r => r.UnitCode)
            .ThenBy(r => r.PositionName)
            .ToList();

        return HttpResult<EstimatedCostReportDto>.Success(new EstimatedCostReportDto
        {
            Context = resolved.Label,
            Basis = basis,
            GroupBy = groupBy,
            IsEstimate = true,
            Rows = orderedRows,
            Subtotals = subtotals,
            GrandTotal = grandTotal,
            UnresolvedPositionCount = unresolved,
            HistoricalFidelityNote = FidelityNote
        });
    }
}
