using System;
using System.Threading.Tasks;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

public interface IConsolidatorImpactService
{
    Task<IHttpResult<ConsolidatorUnitImpactDto>> AnalyzeUnitImpactAsync(int organizationId, Guid unitId);
    Task<IHttpResult<StructureVersionImpactReportDto>> AnalyzeVersionImpactAsync(int organizationId, Guid versionId);
}
