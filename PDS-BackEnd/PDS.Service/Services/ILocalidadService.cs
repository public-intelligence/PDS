using Microsoft.AspNetCore.Http;
using PDS.Domain.DTOs.Localidad;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.Response;

namespace PDS.Service.Services;

public interface ILocalidadService
{
    Task<IHttpResult<PagedResponseDto<LocalidadListItemDto>>> ListAsync(
        int organizationId, LocalidadFilterDto filter, HttpContext httpContext);

    /// <summary>Lista completa sin paginar para integraciones M2M (RF-LO-52); ordenada por nombre, sin eliminadas.</summary>
    Task<IHttpResult<IEnumerable<LocalidadListItemDto>>> ListAllAsync(
        int organizationId, LocalidadFilterDto filter);

    /// <summary>Localidades por ID (sin eliminadas). Con <paramref name="organizationId"/> solo devuelve las de esa organización.</summary>
    Task<IHttpResult<IEnumerable<LocalidadListItemDto>>> GetByIdsAsync(
        IEnumerable<Guid> localidaIds, int? organizationId = null);

    /// <summary>Localidad para SDS (M2M); 404 si no existe, está dada de baja o es de otra organización.</summary>
    Task<IHttpResult<LocalidadM2MDto>> GetM2MAsync(int organizationId, Guid id);

    Task<IHttpResult<IEnumerable<LocalidadMapItemDto>>> GetMapAsync(
        int organizationId, LocalidadFilterDto filter);

    Task<IHttpResult<LocalidadDetailDto>> GetDetailAsync(int organizationId, Guid id);

    Task<IHttpResult<LocalidadDetailDto>> CreateAsync(
        int organizationId, Guid userId, string? userName, CreateLocalidadDto dto, bool canManageCalendar = false);

    Task<IHttpResult<LocalidadDetailDto>> UpdateAsync(
        int organizationId, Guid id, Guid userId, string? userName, UpdateLocalidadDto dto, bool canManageCalendar = false);

    Task<IHttpResult> SuspenderAsync(
        int organizationId, Guid id, Guid userId, string? userName, SuspenderLocalidadDto dto);

    Task<IHttpResult> ReactivarAsync(
        int organizationId, Guid id, Guid userId, string? userName, ReactivarLocalidadDto dto);

    Task<IHttpResult> CerrarAsync(
        int organizationId, Guid id, Guid userId, string? userName, CerrarLocalidadDto dto);

    Task<IHttpResult<DuplicateCheckResultDto>> ValidarDuplicadoAsync(
        int organizationId, DuplicateCheckRequestDto request);

    Task<IHttpResult<IEnumerable<LocalidadAuditoriaDto>>> GetHistorialAsync(int organizationId, Guid id);

    // Sub-recursos
    Task<IHttpResult<IEnumerable<LocalidadServicioDto>>> GetServiciosAsync(int organizationId, Guid localidadId);
    Task<IHttpResult<LocalidadServicioDto>> AddServicioAsync(int organizationId, Guid localidadId, LocalidadServicioInputDto dto);
    Task<IHttpResult> RemoveServicioAsync(int organizationId, Guid localidadId, Guid linkId);

    Task<IHttpResult<IEnumerable<LocalidadResponsableDto>>> GetResponsablesAsync(int organizationId, Guid localidadId);
    Task<IHttpResult<LocalidadResponsableDto>> AddResponsableAsync(
        int organizationId, Guid localidadId, LocalidadResponsableInputDto dto, bool canManageCalendar = false);
    Task<IHttpResult> RemoveResponsableAsync(int organizationId, Guid localidadId, Guid linkId);

    Task<IHttpResult<IEnumerable<LocalidadDocumentoDto>>> GetDocumentosAsync(int organizationId, Guid localidadId);
    Task<IHttpResult<LocalidadDocumentoDto>> AddDocumentoAsync(int organizationId, Guid localidadId, string? userName, LocalidadDocumentoInputDto dto);
    Task<IHttpResult> RemoveDocumentoAsync(int organizationId, Guid localidadId, Guid linkId);

    Task<IHttpResult<IEnumerable<LocalidadUnidadPresenciaDto>>> GetUnidadesPresenciaAsync(int organizationId, Guid localidadId);
    Task<IHttpResult<LocalidadUnidadPresenciaDto>> AddUnidadPresenciaAsync(int organizationId, Guid localidadId, LocalidadUnidadPresenciaInputDto dto);
    Task<IHttpResult> RemoveUnidadPresenciaAsync(int organizationId, Guid localidadId, Guid linkId);
}
