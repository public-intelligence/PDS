using PDS.Domain.DTOs.Reporting;
using PDS.Domain.Response;

namespace PDS.Service.Services;

/// <summary>RF-RE-05: cambios estructurales aprobados en un período (trazabilidad RF-FA-01).</summary>
public interface IStructuralChangesReportService
{
    Task<IHttpResult<StructuralChangesReportDto>> GetAsync(
        int organizationId, StructuralChangesFilterDto filter, string? generatedByName);
}
