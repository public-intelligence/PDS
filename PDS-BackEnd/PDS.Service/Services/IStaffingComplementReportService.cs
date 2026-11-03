using PDS.Domain.DTOs.Reporting;
using PDS.Domain.Response;

namespace PDS.Service.Services;

/// <summary>RF-RE-02: dotación definida (plantilla) por unidad y nivel, consolidando descendientes.</summary>
public interface IStaffingComplementReportService
{
    Task<IHttpResult<StaffingComplementReportDto>> GetAsync(
        int organizationId, ReportingContextDto context, string? generatedByName);
}
