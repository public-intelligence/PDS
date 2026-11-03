using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Planning;
using PDS.Service.Services.Planning;

namespace PDS.Controllers;

[Authorize]
[ApiController]
[Route("planning/funding-sources")]
[Produces("application/json")]
public class FundingSourcesController : ApiControllerBase
{
    private static readonly string[] MissingClaimsError =
        ["No se pudo obtener userId y organizationId del token."];

    private readonly IFundingSourceService _service;

    public FundingSourcesController(IFundingSourceService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FundingSourceDto>>> ListAsync(
        [FromQuery] string? q,
        [FromQuery] bool? onlyActive)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _service.ListAsync(organizationId, q, onlyActive);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpGet("{id}", Name = "GetFundingSourceById")]
    public async Task<ActionResult<FundingSourceDto>> GetByIdAsync(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _service.GetByIdAsync(organizationId, id);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<FundingSourceDto>> CreateAsync([FromBody] CreateFundingSourceDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _service.CreateAsync(organizationId, dto, userId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return CreatedAtRoute("GetFundingSourceById", new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<FundingSourceDto>> UpdateAsync(Guid id, [FromBody] UpdateFundingSourceDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _service.UpdateAsync(organizationId, id, dto, userId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _service.DeleteAsync(organizationId, id, userId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return NoContent();
    }
}
