using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.OrganizationalStructureAreatype;
using PDS.Service.Services;
using System.Security.Claims;

namespace PDS.Controllers;

[Authorize]
[ApiController]
[Route("areatypes")]
[Consumes("application/json")]
[Produces("application/json")]
public class OrganizationalStructureAreaTypeController : ControllerBase
{
    private readonly IOrganizationalStructureAreaTypeService _organizationalStructureAreaTypeService;

    public OrganizationalStructureAreaTypeController(
        IOrganizationalStructureAreaTypeService organizationalStructureAreaTypeService)
    {
        _organizationalStructureAreaTypeService = organizationalStructureAreaTypeService;
    }

    [HttpGet("{orgStruAreaTypeId}")]
    public async Task<ActionResult<OrganizationalStructureAreaTypeDto>> GetOrganizationalStructureAreaTypesById(Guid orgStruAreaTypeId)
    {
        var result = await _organizationalStructureAreaTypeService.GetAsync(orgStruAreaTypeId);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }
        return Ok(result.Value);
    }
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrganizationalStructureAreaTypeDto>>> GetOrganizationalStructureAreaTypes()
    {
        var result = await _organizationalStructureAreaTypeService.ListAsync();
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }
        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(201)]
    public async Task<IActionResult> CreateOrganizationalStructureAreaType(CreateOrganizationalStructureAreaTypeDto createOrganizationalStructureAreaTypeDto)
    {
        var createOrganizationalStructureAreaTypeResult = await _organizationalStructureAreaTypeService.CreateAsync(createOrganizationalStructureAreaTypeDto);
        if (!createOrganizationalStructureAreaTypeResult.IsSuccess)
        {
            return StatusCode((int)createOrganizationalStructureAreaTypeResult.StatusCode, createOrganizationalStructureAreaTypeResult.Error);
        }
        return NoContent();
    }

    [HttpPut("{orgStruAreaTypeId}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> UpdateOrganizationalStructureAreaType(Guid orgStruAreaTypeId, UpdateOrganizationalStructureAreaTypeDto updateOrganizationalStructureAreaTypeDto)
    {
        var updateOrganizationalStructureAreaTypeResult = await _organizationalStructureAreaTypeService.UpdateAsync(orgStruAreaTypeId, updateOrganizationalStructureAreaTypeDto);
        if (!updateOrganizationalStructureAreaTypeResult.IsSuccess)
        {
            return StatusCode((int)updateOrganizationalStructureAreaTypeResult.StatusCode, updateOrganizationalStructureAreaTypeResult.Error);
        }
        return Ok(updateOrganizationalStructureAreaTypeResult.Value);
    }

    [HttpDelete("{orgStruAreaTypeId}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteOrganizationalStructureAreaType(Guid orgStruAreaTypeId)
    {
        var deleteOrganizationalStructureAreaTypeResult = await _organizationalStructureAreaTypeService.DeleteAsync(orgStruAreaTypeId);
        if (!deleteOrganizationalStructureAreaTypeResult.IsSuccess)
        {
            return StatusCode((int)deleteOrganizationalStructureAreaTypeResult.StatusCode, deleteOrganizationalStructureAreaTypeResult.Error);
        }
        return NoContent();
    }
}
