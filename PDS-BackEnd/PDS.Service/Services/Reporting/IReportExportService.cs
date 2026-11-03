using PDS.Domain.DTOs.Reporting;
using PDS.Domain.Enums;
using PDS.Domain.Response;

namespace PDS.Service.Services.Reporting;

/// <summary>
/// RF-RE-06: genera la exportación (PDF/Excel/CSV) de cada reporte. Reúsa los servicios de datos para
/// que la exportación coincida exactamente con la pantalla para el mismo contexto y filtros.
/// </summary>
public interface IReportExportService
{
    Task<IHttpResult<ReportExportFile>> ExportAuthorizedPostsAsync(
        int organizationId, ReportingContextDto context, ReportGrouping groupBy, ReportFormat format, string? generatedByName);

    Task<IHttpResult<ReportExportFile>> ExportStaffingComplementAsync(
        int organizationId, ReportingContextDto context, ReportFormat format, string? generatedByName);

    Task<IHttpResult<ReportExportFile>> ExportEstimatedCostAsync(
        int organizationId, ReportingContextDto context, SalaryBasis basis, ReportGrouping groupBy, ReportFormat format, string? generatedByName);

    Task<IHttpResult<ReportExportFile>> ExportComparativeAsync(
        int organizationId, Guid? fromVersionId, DateOnly? fromDate, Guid? toVersionId, DateOnly? toDate,
        SalaryBasis basis, ReportFormat format, string? generatedByName);

    Task<IHttpResult<ReportExportFile>> ExportStructuralChangesAsync(
        int organizationId, StructuralChangesFilterDto filter, ReportFormat format, string? generatedByName);
}

/// <summary>Archivo exportado listo para transmitir (bytes + tipo de contenido + nombre).</summary>
public sealed class ReportExportFile
{
    public required byte[] Content { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
}
