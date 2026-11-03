using PDS.Domain.DTOs.Reporting;
using PDS.Domain.Enums;
using PDS.Domain.Response;

namespace PDS.Service.Services;

/// <summary>RF-RE-03: costo estimado de la plantilla (salario × plazas autorizadas).</summary>
public interface IEstimatedCostReportService
{
    Task<IHttpResult<EstimatedCostReportDto>> GetAsync(
        int organizationId, ReportingContextDto context, SalaryBasis basis, ReportGrouping groupBy, string? generatedByName);
}
