using PDS.Domain.DTOs.OrgChart;
using PDS.Domain.DTOs.StructureVersion;
using PDS.Domain.Response;

namespace PDS.Service.Services;

/// <summary>
/// Construye el organigrama (lista plana de nodos) a partir de la fotografía de una versión
/// publicada o, para versiones en curso, de la estructura operativa en vivo.
/// </summary>
public interface IOrgChartService
{
    /// <summary>Organigrama vigente hoy. <paramref name="requirePublic"/> exige Published + IsPublic.</summary>
    Task<IHttpResult<OrgChartDto>> GetCurrentAsync(
        int organizationId,
        Guid? rootUnitId,
        int? maxLevel,
        bool includePositions,
        bool requirePublic
    );

    /// <summary>Organigrama vigente en una fecha pasada (RF-OG-04 / RF-VH-02).</summary>
    Task<IHttpResult<OrgChartDto>> GetAsOfAsync(
        int organizationId,
        DateOnly date,
        Guid? rootUnitId,
        int? maxLevel,
        bool includePositions,
        bool requirePublic
    );

    /// <summary>Organigrama de una versión específica por Id (vista interna).</summary>
    Task<IHttpResult<OrgChartDto>> GetByVersionIdAsync(
        int organizationId,
        Guid versionId,
        Guid? rootUnitId,
        int? maxLevel,
        bool includePositions,
        bool requirePublic
    );

    /// <summary>Organigrama de una versión publicada por su número (selector público).</summary>
    Task<IHttpResult<OrgChartDto>> GetByNumberAsync(
        int organizationId,
        int number,
        Guid? rootUnitId,
        int? maxLevel,
        bool includePositions,
        bool requirePublic
    );

    /// <summary>Lista de versiones publicadas y públicas (para el selector del organigrama público).</summary>
    Task<IHttpResult<IEnumerable<StructureVersionSummaryDto>>> ListPublicVersionsAsync(int organizationId);
}
