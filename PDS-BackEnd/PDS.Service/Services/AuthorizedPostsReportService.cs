using Microsoft.Extensions.Logging;
using PDS.Domain.DTOs.Reporting;
using PDS.Domain.Enums;
using PDS.Domain.Response;
using System.Net;

namespace PDS.Service.Services;

// RF-RE-01: plazas autorizadas. Solo definición (sin ocupadas/vacantes).
public class AuthorizedPostsReportService(
    ILogger<AuthorizedPostsReportService> logger,
    IReportingContextResolver resolver
) : IAuthorizedPostsReportService
{
    private readonly ILogger<AuthorizedPostsReportService> _logger = logger;
    private readonly IReportingContextResolver _resolver = resolver;

    public async Task<IHttpResult<AuthorizedPostsReportDto>> GetAsync(
        int organizationId, ReportingContextDto context, ReportGrouping groupBy, string? generatedByName)
    {
        var resolution = await _resolver.ResolveAsync(organizationId, context, generatedByName);
        if (!resolution.IsSuccess)
            return HttpResult<AuthorizedPostsReportDto>.Failure(
                resolution.Error.Title, resolution.Error.Detail, resolution.StatusCode);

        var resolved = resolution.Value;
        var salary = resolved.SalaryByPositionId;

        // Filas hoja: una por asignación cargo↔unidad.
        var rows = new List<ReportRowDto>();
        foreach (var node in resolved.Nodes)
        {
            foreach (var pos in node.Positions)
            {
                var grade = salary.TryGetValue(pos.PositionId, out var s) ? s.Grade : 0;
                var sel = ReportGroupSelector.Select(
                    groupBy, node.SourceUnitId, node.Code, node.Name, node.Level, grade, pos.OccupationalGroupName);

                rows.Add(new ReportRowDto
                {
                    GroupKey = sel.Key,
                    UnitId = node.SourceUnitId,
                    UnitCode = node.Code,
                    UnitName = node.Name,
                    AreaTypeName = node.AreaTypeName,
                    Level = node.Level,
                    PositionId = pos.PositionId,
                    PositionName = pos.Name,
                    OccupationalGroupName = pos.OccupationalGroupName,
                    Grade = grade,
                    AuthorizedPosts = pos.AuthorizedPosts
                });
            }
        }

        // Subtotales por grupo, ordenados por la dimensión.
        var subtotals = rows
            .GroupBy(r => r.GroupKey)
            .Select(g =>
            {
                var first = g.First();
                var sel = ReportGroupSelector.Select(
                    groupBy, first.UnitId, first.UnitCode, first.UnitName, first.Level, first.Grade ?? 0, first.OccupationalGroupName);
                return new
                {
                    sel.Sort,
                    Subtotal = new ReportSubtotalDto
                    {
                        GroupKey = sel.Key,
                        GroupLabel = sel.Label,
                        DistinctPositionCount = g.Select(r => r.PositionId).Distinct().Count(),
                        AuthorizedPosts = g.Sum(r => r.AuthorizedPosts)
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
            AuthorizedPosts = rows.Sum(r => r.AuthorizedPosts)
        };

        // Ordena las filas para que sigan el orden de los grupos y sean legibles.
        var groupOrder = subtotals.Select((s, i) => (s.GroupKey, i)).ToDictionary(x => x.GroupKey, x => x.i);
        var orderedRows = rows
            .OrderBy(r => groupOrder.TryGetValue(r.GroupKey ?? "", out var i) ? i : int.MaxValue)
            .ThenBy(r => r.UnitCode)
            .ThenBy(r => r.PositionName)
            .ToList();

        return HttpResult<AuthorizedPostsReportDto>.Success(new AuthorizedPostsReportDto
        {
            Context = resolved.Label,
            GroupBy = groupBy,
            Rows = orderedRows,
            Subtotals = subtotals,
            GrandTotal = grandTotal
        });
    }
}
