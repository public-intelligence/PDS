using PDS.Domain.DTOs.Planning;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

public interface IPlanningConfigService
{
    Task<IHttpResult<PlanningAreaConfigDto>> GetAreaConfigAsync(int organizationId, CancellationToken ct = default);
    Task<IHttpResult<PlanningAreaConfigDto>> UpdateAreaConfigAsync(
        int organizationId, int maxLevel, Guid userId, string? userName, List<Guid>? customAreaIds = null, CancellationToken ct = default);
    Task<List<string>> GetQualifyingAreaNamesAsync(int organizationId, CancellationToken ct = default);

    /// <summary>Ids de las áreas que, según el guardrail vigente, deben formular plan.</summary>
    Task<HashSet<Guid>> GetQualifyingAreaIdsAsync(int organizationId, CancellationToken ct = default);

    /// <summary>
    /// Profundidad máxima configurada (-1 = selección personalizada). Es asíncrono porque el
    /// guardrail se lee de base de datos, no de memoria de proceso.
    /// </summary>
    Task<int> GetMaxLevelAsync(int organizationId, CancellationToken ct = default);
}
