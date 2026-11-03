using PDS.Domain.DTOs.OccupationalGroups;
using PDS.Domain.Response;

namespace PDS.Service.Services;

public interface IOccupationalGroupService
{
    Task<IHttpResult<IEnumerable<OccupationalGroupDto>>> GetAllOccupationalGroupsAsync();
    Task<IHttpResult<OccupationalGroupDto>> GetOccupationalGroupAsync(Guid id);
    public Task<IHttpResult<OccupationalGroupDto>> CreateOccupationalGroupAsync(
        CreateOccupationalGroupDto createOccupationalGroupDto
    );
    Task<IHttpResult<OccupationalGroupDto>> UpdateOccupationalGroupAsync(
        Guid id,
        UpdateOccupationalGroupDto updateOccupationalGroupDto
    );
    Task<IHttpResult> DeleteOccupationalGroupByIdAsync(Guid id);
}
