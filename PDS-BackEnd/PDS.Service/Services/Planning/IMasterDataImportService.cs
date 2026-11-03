using System;
using System.IO;
using System.Threading.Tasks;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

public interface IMasterDataImportService
{
    Task<IHttpResult<MasterDataImportResultDto>> ImportAsync(int organizationId, Stream stream, bool dryRun, Guid userId, string? userName);
    Task<IHttpResult<MasterDataImportResultDto>> ImportFromRepositoryAsync(int organizationId, bool dryRun, Guid userId, string? userName);
}
