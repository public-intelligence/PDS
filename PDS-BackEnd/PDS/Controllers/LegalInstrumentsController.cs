using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.LegalInstrument;
using PDS.Service.Services;

namespace PDS.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public class LegalInstrumentsController : ApiControllerBase
{
    private readonly ILogger<LegalInstrumentsController> _logger;
    private readonly ILegalInstrumentService _service;
    private static readonly string[] tokenError = ["No se pudo obtener userId y organizationId del token."];

    public LegalInstrumentsController(ILogger<LegalInstrumentsController> logger, ILegalInstrumentService service)
    {
        _logger = logger;
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetAll()
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _service.ListAsync(organizationId);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetById(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _service.GetAsync(organizationId, id);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(201)]
    public async Task<ActionResult> Create([FromBody] CreateLegalInstrumentDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _service.CreateAsync(organizationId, dto);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateLegalInstrumentDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _service.UpdateAsync(organizationId, id, dto);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> Delete(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _service.DeleteAsync(organizationId, id);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return NoContent();
    }
}
