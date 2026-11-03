using PDS.Domain.DTOs.Reporting;
using PDS.Domain.Response;

namespace PDS.Service.Services;

/// <summary>RF-RE-07: indicadores definicionales del contexto seleccionado, en un solo payload.</summary>
public interface IDashboardService
{
    Task<IHttpResult<DashboardDto>> GetAsync(
        int organizationId, ReportingContextDto context, string? generatedByName);
}
