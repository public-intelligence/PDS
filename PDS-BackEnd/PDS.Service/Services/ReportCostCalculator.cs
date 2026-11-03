using PDS.Domain.DTOs.OrgChart;
using PDS.Domain.Enums;

namespace PDS.Service.Services;

/// <summary>
/// Costo estimado = salario (según base) × plazas autorizadas. Reutilizado por RF-RE-03, RF-RE-04 y
/// el dashboard (RF-RE-07). Es una estimación, no nómina real.
/// </summary>
internal static class ReportCostCalculator
{
    public static decimal? LineCost(PositionSalary? salary, int authorizedPosts, SalaryBasis basis)
    {
        var unit = salary?.Unit(basis);
        return unit is null ? null : unit.Value * authorizedPosts;
    }

    /// <summary>Costo total del organigrama resuelto y conteo de cargos sin salario utilizable.</summary>
    public static (decimal Total, int Unresolved) Total(
        IEnumerable<OrgChartNodeDto> nodes,
        IReadOnlyDictionary<Guid, PositionSalary> salaryByPositionId,
        SalaryBasis basis)
    {
        decimal total = 0m;
        var unresolved = 0;
        foreach (var node in nodes)
        {
            foreach (var pos in node.Positions)
            {
                PositionSalary? salary = salaryByPositionId.TryGetValue(pos.PositionId, out var s) ? s : null;
                var line = LineCost(salary, pos.AuthorizedPosts, basis);
                if (line is null) unresolved++;
                else total += line.Value;
            }
        }
        return (total, unresolved);
    }
}
