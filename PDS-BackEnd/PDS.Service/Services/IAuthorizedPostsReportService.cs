using PDS.Domain.DTOs.Reporting;
using PDS.Domain.Enums;
using PDS.Domain.Response;

namespace PDS.Service.Services;

/// <summary>RF-RE-01: reporte de plazas autorizadas con pivote por unidad/nivel/categoría salarial.</summary>
public interface IAuthorizedPostsReportService
{
    Task<IHttpResult<AuthorizedPostsReportDto>> GetAsync(
        int organizationId, ReportingContextDto context, ReportGrouping groupBy, string? generatedByName);
}
