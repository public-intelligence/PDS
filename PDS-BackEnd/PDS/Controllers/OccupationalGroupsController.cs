using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.OccupationalGroups;
using PDS.Service.Services;

namespace PDS.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public class OccupationalGroupsController : ControllerBase
{
    private readonly ILogger<OccupationalGroupsController> _logger;
    private readonly IOccupationalGroupService _occupationalGroupService;

    public OccupationalGroupsController(
        ILogger<OccupationalGroupsController> logger,
        IOccupationalGroupService occupationalGroupService  
    )
    {
        _logger = logger;
        _occupationalGroupService = occupationalGroupService;
    }

    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<ActionResult<IEnumerable<OccupationalGroupDto>>> GetAllOccupationalGroups()
    {
        var result = await _occupationalGroupService.GetAllOccupationalGroupsAsync();
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }

        var occupationalGroupDtos = result.Value;
        return Ok(occupationalGroupDtos);
    }

    [HttpGet("{occupationalGroupId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<OccupationalGroupDto>> GetOccupationalGroupById(
        Guid occupationalGroupId
    )
    {
        var result = await _occupationalGroupService.GetOccupationalGroupAsync(occupationalGroupId);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }

        var occupationalGroupDto = result.Value;
        return Ok(occupationalGroupDto);
    }
}