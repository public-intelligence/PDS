using Microsoft.Extensions.Logging;
using PDS.Domain.DTOs.Reporting;
using PDS.Domain.Response;

namespace PDS.Service.Services;

// RF-RE-02: dotación definida. Cargos distintos + plazas autorizadas, consolidados por jerarquía.
// NO expone ocupación real (plazas ocupadas/vacantes están fuera de alcance por diseño).
public class StaffingComplementReportService(
    ILogger<StaffingComplementReportService> logger,
    IReportingContextResolver resolver
) : IStaffingComplementReportService
{
    private readonly ILogger<StaffingComplementReportService> _logger = logger;
    private readonly IReportingContextResolver _resolver = resolver;

    public async Task<IHttpResult<StaffingComplementReportDto>> GetAsync(
        int organizationId, ReportingContextDto context, string? generatedByName)
    {
        var resolution = await _resolver.ResolveAsync(organizationId, context, generatedByName);
        if (!resolution.IsSuccess)
            return HttpResult<StaffingComplementReportDto>.Failure(
                resolution.Error.Title, resolution.Error.Detail, resolution.StatusCode);

        var resolved = resolution.Value;
        var rollup = ReportingContextResolver.Rollup(resolved.Nodes);

        var units = rollup.Values
            .OrderBy(a => a.Node.Level).ThenBy(a => a.Node.Code)
            .Select(a => new StaffingComplementRowDto
            {
                UnitId = a.Node.SourceUnitId,
                Code = a.Node.Code,
                Name = a.Node.Name,
                AreaTypeName = a.Node.AreaTypeName,
                Level = a.Node.Level,
                ParentUnitId = a.Node.ParentSourceUnitId,
                OwnDistinctPositions = a.OwnDistinctPositions,
                OwnAuthorizedPosts = a.OwnAuthorizedPosts,
                ConsolidatedDistinctPositions = a.ConsolidatedDistinctPositions,
                ConsolidatedAuthorizedPosts = a.ConsolidatedAuthorizedPosts,
                HasSupervisorDefined = a.HasSupervisorDefined
            })
            .ToList();

        // Por nivel: se suman las figuras PROPIAS (no consolidadas) para no contar dos veces.
        var byLevel = rollup.Values
            .GroupBy(a => a.Node.Level)
            .OrderBy(g => g.Key)
            .Select(g => new ReportSubtotalDto
            {
                GroupKey = $"L{g.Key}",
                GroupLabel = $"Nivel {g.Key}",
                DistinctPositionCount = g.Sum(a => a.OwnDistinctPositions),
                AuthorizedPosts = g.Sum(a => a.OwnAuthorizedPosts)
            })
            .ToList();

        var grandTotal = new ReportSubtotalDto
        {
            GroupKey = "__total__",
            GroupLabel = "Total general",
            DistinctPositionCount = rollup.Values.Sum(a => a.OwnDistinctPositions),
            AuthorizedPosts = rollup.Values.Sum(a => a.OwnAuthorizedPosts)
        };

        return HttpResult<StaffingComplementReportDto>.Success(new StaffingComplementReportDto
        {
            Context = resolved.Label,
            Units = units,
            ByLevel = byLevel,
            GrandTotal = grandTotal
        });
    }
}
