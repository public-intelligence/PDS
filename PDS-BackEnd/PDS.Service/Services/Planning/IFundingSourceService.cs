using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

public interface IFundingSourceService
{
    Task<IHttpResult<IEnumerable<FundingSourceDto>>> ListAsync(int organizationId, string? q = null, bool? onlyActive = null);
    Task<IHttpResult<FundingSourceDto>> GetByIdAsync(int organizationId, Guid id);
    Task<IHttpResult<FundingSourceDto>> CreateAsync(int organizationId, CreateFundingSourceDto dto, Guid userId, string? userName);
    Task<IHttpResult<FundingSourceDto>> UpdateAsync(int organizationId, Guid id, UpdateFundingSourceDto dto, Guid userId, string? userName);
    Task<IHttpResult> DeleteAsync(int organizationId, Guid id, Guid userId, string? userName);
}
