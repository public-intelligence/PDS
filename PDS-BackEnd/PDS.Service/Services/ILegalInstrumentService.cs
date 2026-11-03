using PDS.Domain.DTOs.LegalInstrument;
using PDS.Domain.Response;

namespace PDS.Service.Services;

public interface ILegalInstrumentService
{
    Task<IHttpResult<IEnumerable<LegalInstrumentDto>>> ListAsync(int organizationId);
    Task<IHttpResult<LegalInstrumentDto>> GetAsync(int organizationId, Guid id);
    Task<IHttpResult<LegalInstrumentDto>> CreateAsync(int organizationId, CreateLegalInstrumentDto dto);
    Task<IHttpResult<LegalInstrumentDto>> UpdateAsync(int organizationId, Guid id, UpdateLegalInstrumentDto dto);
    Task<IHttpResult> DeleteAsync(int organizationId, Guid id);
}
