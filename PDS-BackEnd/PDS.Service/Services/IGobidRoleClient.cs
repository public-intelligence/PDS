using PDS.Domain.Response;

namespace PDS.Service.Services;

public interface IGobidRoleClient
{
    /// <summary>
    /// Crea (o actualiza) en GOBID el rol asociado a una posición, con los claims automáticos asignados.
    /// </summary>
    Task<IHttpResult<bool>> UpsertPositionRoleAsync(
        Guid positionId,
        string positionName,
        CancellationToken ct = default);

    /// <summary>
    /// Elimina en GOBID todos los roles asociados a una posición. Si no hay ninguno, no hace nada.
    /// </summary>
    Task<IHttpResult<bool>> DeletePositionRoleAsync(
        Guid positionId,
        CancellationToken ct = default);
}
