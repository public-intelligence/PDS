using PDS.Domain.DTOs.Organization;
using PDS.Domain.Response;

namespace PDS.Service.Services;

public interface IOrganizationDirectoryService
{
    /// <summary>Crea o actualiza la entrada de directorio del tenant a partir de los datos del token.</summary>
    Task UpsertAsync(int organizationId, string? name, string? acronym);

    /// <summary>Resuelve una organización pública por su slug (para el organigrama anónimo).</summary>
    Task<IHttpResult<OrganizationDirectoryDto>> ResolveBySlugAsync(string slug);
}
