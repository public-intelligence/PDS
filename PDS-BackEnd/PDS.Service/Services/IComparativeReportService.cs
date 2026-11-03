using PDS.Domain.DTOs.Reporting;
using PDS.Domain.Enums;
using PDS.Domain.Response;

namespace PDS.Service.Services;

/// <summary>RF-RE-04: comparativo entre dos contextos (versiones o fechas).</summary>
public interface IComparativeReportService
{
    Task<IHttpResult<ComparativeReportDto>> GetAsync(
        int organizationId, Guid? fromVersionId, DateOnly? fromDate,
        Guid? toVersionId, DateOnly? toDate, SalaryBasis basis, string? generatedByName);
}
