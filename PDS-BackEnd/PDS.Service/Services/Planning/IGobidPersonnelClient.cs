using PDS.Domain.DTOs.Planning;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

/// <summary>
/// Cliente M2M hacia la Personnel API de GOBID. Único propósito en PDS: resolver el cargo
/// (positionId) de un personnel para que PDS derive el área con su propio organigrama.
/// La ruta del endpoint es configurable (GobidPersonnelSettings.PathTemplate).
/// </summary>
public interface IGobidPersonnelClient
{
    Task<IHttpResult<GobidPersonnelDto>> GetPersonnelAsync(string personnelId, CancellationToken ct = default);
}
