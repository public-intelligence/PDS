using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Planning;
using PDS.Service.Authorization;
using PDS.Service.Services.Planning;
using System.Threading.Tasks;

namespace PDS.Controllers;

[Authorize]
[ApiController]
[Route("master-data")]
[Produces("application/json")]
public class MasterDataImportController : ApiControllerBase
{
    private readonly IMasterDataImportService _service;
    private readonly IPlanningContextService _contextService;
    private readonly IPlanningAccessGuard _guard;

    public MasterDataImportController(
        IMasterDataImportService service,
        IPlanningContextService contextService,
        IPlanningAccessGuard guard)
    {
        _service = service;
        _contextService = contextService;
        _guard = guard;
    }

    [HttpPost("import")]
    public async Task<ActionResult<MasterDataImportResultDto>> Import([FromForm] IFormFile file, [FromQuery] bool dryRun = false)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { errors = new[] { "Debe proporcionar un archivo Excel de Maestra Institucional." } });
        }

        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var perms = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(organizationId, userId, GetUserName(), GetPersonnelId(), perms);
        if (!contextResult.IsSuccess) return StatusCode((int)contextResult.StatusCode, contextResult.Error);
        var ctx = contextResult.Value;

        if (!_guard.HasPermission(ctx, PlanningPermissions.CatalogManage))
        {
            return Forbid();
        }

        using var stream = file.OpenReadStream();
        var result = await _service.ImportAsync(organizationId, stream, dryRun, userId, GetUserName());
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpPost("import/from-repository")]
    public async Task<ActionResult<MasterDataImportResultDto>> ImportFromRepository([FromQuery] bool dryRun = false)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var perms = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(organizationId, userId, GetUserName(), GetPersonnelId(), perms);
        if (!contextResult.IsSuccess) return StatusCode((int)contextResult.StatusCode, contextResult.Error);
        var ctx = contextResult.Value;

        if (!_guard.HasPermission(ctx, PlanningPermissions.CatalogManage))
        {
            return Forbid();
        }

        var result = await _service.ImportFromRepositoryAsync(organizationId, dryRun, userId, GetUserName());
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }
}
