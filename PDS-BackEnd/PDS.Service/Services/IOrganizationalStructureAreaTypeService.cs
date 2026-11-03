using PDS.Domain.DTOs.OrganizationalStructureAreatype;
using PDS.Domain.Response;

namespace PDS.Service.Services;

public interface IOrganizationalStructureAreaTypeService
{
    Task<IHttpResult<IEnumerable<OrganizationalStructureAreaTypeDto>>> ListAsync(
        );
    Task<IHttpResult<OrganizationalStructureAreaTypeDto>> GetAsync(
         Guid organizationalStructureAreaTypeId
     );
    Task<IHttpResult<OrganizationalStructureAreaTypeDto>> CreateAsync(
          CreateOrganizationalStructureAreaTypeDto createOrganizationalStructureAreaTypeDto
      );
    Task<IHttpResult<OrganizationalStructureAreaTypeDto>> UpdateAsync(
       Guid organizationalStructureAreaTypeId,
       UpdateOrganizationalStructureAreaTypeDto updateOrganizationalStructureAreaTypeDto
   );
    Task<IHttpResult> DeleteAsync(
        Guid organizationalStructureAreaTypeId
    );
}
