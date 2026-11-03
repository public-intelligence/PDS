using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Service.Services;
using System.Threading;
using System.Threading.Tasks;

namespace PDS.Controllers;

[Authorize]
[ApiController]
[Route("User")]
public class UserController : ApiControllerBase
{
    private readonly IGobidUserPositionsClient _userPositionsClient;

    public UserController(IGobidUserPositionsClient userPositionsClient)
    {
        _userPositionsClient = userPositionsClient;
    }

    /// <summary>
    /// Consulta los usuarios y roles de una organización en GOBID vía conexión M2M.
    /// Endpoint expuesto: GET /api/v1/User/organization-application/{organizationApplicationId}/position
    /// </summary>
    [HttpGet("organization-application/{organizationApplicationId:int}/position")]
    public async Task<IActionResult> GetUserPositions(
        [FromRoute] int organizationApplicationId,
        [FromQuery] string? searchposition,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        try
        {
            var jsonResult = await _userPositionsClient.GetUserPositionsJsonAsync(
                organizationApplicationId, searchposition, pageNumber, pageSize, ct);

            return Content(jsonResult, "application/json");
        }
        catch
        {
            return StatusCode(502, new
            {
                succeeded = false,
                message = "No se pudo consultar el servicio de usuarios y roles de GOBID."
            });
        }
    }
}
