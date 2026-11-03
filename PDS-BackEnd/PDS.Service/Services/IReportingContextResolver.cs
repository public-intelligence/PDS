using PDS.Domain.DTOs.OrgChart;
using PDS.Domain.DTOs.Reporting;
using PDS.Domain.Enums;
using PDS.Domain.Response;

namespace PDS.Service.Services;

/// <summary>
/// Resuelve UNA sola vez el contexto de reportería a través de la capa de versionado
/// (IOrgChartService): organigrama de la versión vigente, de una versión específica o a una fecha.
/// Además carga la fotografía de salario/grado de esa versión (StructureVersionPosition) para que el
/// costo estimado y la categoría salarial sean fieles a la versión consultada. No escribe nada.
/// </summary>
public interface IReportingContextResolver
{
    Task<IHttpResult<ResolvedReportingContext>> ResolveAsync(
        int organizationId, ReportingContextDto context, string? generatedByName);
}

/// <summary>Contexto resuelto: organigrama + etiqueta + mapa de salario/grado por cargo.</summary>
public sealed class ResolvedReportingContext
{
    public required OrgChartDto Chart { get; init; }
    public required ReportContextLabelDto Label { get; init; }
    public required IReadOnlyDictionary<Guid, PositionSalary> SalaryByPositionId { get; init; }

    public ICollection<OrgChartNodeDto> Nodes => Chart.Nodes;
}

/// <summary>Salario y grado fotografiados de un cargo dentro de la versión resuelta.</summary>
public readonly record struct PositionSalary(int Grade, decimal MinSalary, decimal MaxSalary)
{
    /// <summary>Hay salario utilizable (al menos un extremo del rango es positivo).</summary>
    public bool HasSalary => MinSalary > 0m || MaxSalary > 0m;

    /// <summary>Salario unitario según la base (null si no hay salario utilizable).</summary>
    public decimal? Unit(SalaryBasis basis) => !HasSalary
        ? null
        : basis switch
        {
            SalaryBasis.Min => MinSalary,
            SalaryBasis.Max => MaxSalary,
            _ => (MinSalary + MaxSalary) / 2m
        };
}

/// <summary>Agregados por unidad: figuras propias y consolidadas (unidad + descendientes).</summary>
public sealed class UnitAggregate
{
    public required OrgChartNodeDto Node { get; init; }
    public int OwnDistinctPositions { get; set; }
    public int OwnAuthorizedPosts { get; set; }
    public int ConsolidatedDistinctPositions { get; set; }
    public int ConsolidatedAuthorizedPosts { get; set; }
    public bool HasSupervisorDefined { get; set; }
}
