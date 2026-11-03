using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Service.Services;

namespace PDS.Controllers;

/// <summary>Lectura en cascada del catálogo territorial de la República Dominicana (RF-LO).</summary>
[Authorize]
[ApiController]
[Route("territorio")]
[Produces("application/json")]
public class TerritorioController : ApiControllerBase
{
    private readonly ITerritorioService _service;

    public TerritorioController(ITerritorioService service) => _service = service;

    [HttpGet("paises")]
    public async Task<ActionResult> GetPaises()
    {
        var result = await _service.GetPaisesAsync();
        return result.IsSuccess ? Ok(result.Value) : StatusCode((int)result.StatusCode, result.Error);
    }

    [HttpGet("regiones")]
    public async Task<ActionResult> GetRegiones([FromQuery] Guid? paisId)
    {
        var result = await _service.GetRegionesAsync(paisId);
        return result.IsSuccess ? Ok(result.Value) : StatusCode((int)result.StatusCode, result.Error);
    }

    [HttpGet("provincias")]
    public async Task<ActionResult> GetProvincias([FromQuery] Guid? regionId, [FromQuery] Guid? paisId)
    {
        var result = await _service.GetProvinciasAsync(regionId, paisId);
        return result.IsSuccess ? Ok(result.Value) : StatusCode((int)result.StatusCode, result.Error);
    }

    [HttpGet("municipios")]
    public async Task<ActionResult> GetMunicipios([FromQuery] Guid? provinciaId)
    {
        var result = await _service.GetMunicipiosAsync(provinciaId);
        return result.IsSuccess ? Ok(result.Value) : StatusCode((int)result.StatusCode, result.Error);
    }

    [HttpGet("distritos")]
    public async Task<ActionResult> GetDistritos([FromQuery] Guid municipioId)
    {
        var result = await _service.GetDistritosAsync(municipioId);
        return result.IsSuccess ? Ok(result.Value) : StatusCode((int)result.StatusCode, result.Error);
    }
}
