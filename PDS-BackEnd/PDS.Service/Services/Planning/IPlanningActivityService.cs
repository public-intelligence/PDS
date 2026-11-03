using Microsoft.AspNetCore.Http;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

/// <summary>
/// Bitácora del módulo de Planificación. La escritura es interna (la llaman los servicios de
/// negocio en cada mutación); la lectura alimenta el panel de actividad y la trazabilidad.
/// </summary>
public interface IPlanningActivityService
{
    /// <summary>
    /// Registra una acción de negocio. Nunca lanza: un fallo de bitácora no debe romper la
    /// operación que la origina (se registra en el log de aplicación).
    /// </summary>
    Task LogAsync(
        int organizationId,
        Guid userId,
        string? userName,
        string module,
        string action,
        string? detail = null,
        string? entityType = null,
        Guid? entityId = null);

    Task<IHttpResult<PagedResponseDto<ActivityLogDto>>> ListAsync(
        int organizationId,
        PaginationFilterDto pagination,
        HttpContext httpContext,
        string? module = null,
        Guid? entityId = null);
}
