using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Localidad;
using PDS.Domain.DTOs.Pagination;
using PDS.Service.Authorization;
using PDS.Service.Services;

namespace PDS.Controllers;

[Authorize]
[ApiController]
[Route("localidades")]
[Consumes("application/json")]
[Produces("application/json")]
public class LocalidadesController : ApiControllerBase
{
    private readonly ILogger<LocalidadesController> _logger;
    private readonly ILocalidadService _service;
    private readonly IPaginationService _paginationService;
    private static readonly string[] tokenError = ["No se pudo obtener userId y organizationId del token."];

    public LocalidadesController(
        ILogger<LocalidadesController> logger,
        ILocalidadService service,
        IPaginationService paginationService)
    {
        _logger = logger;
        _service = service;
        _paginationService = paginationService;
    }

    // RF-LO-15: búsqueda y filtros con paginación.
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<ActionResult<PagedResponseDto<LocalidadListItemDto>>> GetAll([FromQuery] LocalidadFilterDto filter)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _service.ListAsync(organizationId, filter, HttpContext);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        _paginationService.SetPaginationHeaders(Response, result.Value);
        return Ok(result.Value.Value);
    }

    /// <summary>
    /// Alias en desuso de <c>GET m2m/organization/{organizationId}/localidades</c> (RF-LO-52): mismo comportamiento y
    /// encabezado <c>Deprecation</c> hasta que WF cambie su operación.
    /// </summary>
    [HttpGet("/m2m/organization/{organizationId}/locality")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<IEnumerable<LocalidadListItemDto>>> GetAllM2M([FromRoute]int organizationId, [FromQuery] LocalidadFilterDto filter)
    {
        var m2mCheck = await ValidateM2MPermissionsAsync();
        if (m2mCheck != null) return m2mCheck;

        var result = await _service.ListAllAsync(organizationId, filter);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        Response.Headers["Deprecation"] = "true";
        return Ok(result.Value);
    }

    /// <summary>
    /// Localidad para SDS (spec M2M #1). Solo cuentas de servicio con <c>access:M2M</c>. 404 si la localidad es de otra
    /// organización. Responde con <c>ETag</c>; <c>If-None-Match</c> vigente devuelve 304 sin cuerpo.
    /// </summary>
    [HttpGet("/m2m/organization/{organizationId:int}/locality/{localityId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(304)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<LocalidadM2MDto>> GetM2M([FromRoute] int organizationId, [FromRoute] Guid localityId)
    {
        var m2mCheck = await RequireM2MServiceAccountAsync();
        if (m2mCheck != null) return m2mCheck;

        var result = await _service.GetM2MAsync(organizationId, localityId);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        var dto = result.Value;
        var etag = ETags.FromParts(
            dto.Id.ToString("N"), dto.Codigo, dto.NombreOficial, dto.DireccionCompleta,
            dto.OrganizationId.ToString(), dto.Activa ? "1" : "0", dto.IanaTimeZone, dto.Version);
        Response.Headers.ETag = etag;

        if (ETags.Matches(Request.Headers.IfNoneMatch.ToString(), etag))
            return StatusCode(StatusCodes.Status304NotModified);

        return Ok(dto);
    }

    [HttpGet("/m2m/localidades/by-ids")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<IEnumerable<LocalidadListItemDto>>> GetByIds(
        [FromQuery] List<Guid> localidadIds, [FromQuery] int? organizationId = null)
    {
        var m2mCheck = await ValidateM2MPermissionsAsync();
        if (m2mCheck != null) return m2mCheck;

        if (localidadIds == null || localidadIds.Count == 0)
            return BadRequest(new { errors = new[] { "Debe proporcionar al menos un localidadIds." } });

        var result = await _service.GetByIdsAsync(localidadIds, organizationId);
        if(!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    // RF-LO-16: carga ligera para el mapa.
    [HttpGet("mapa")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetMapa([FromQuery] LocalidadFilterDto filter)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _service.GetMapAsync(organizationId, filter);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("validar-duplicado")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> ValidarDuplicado([FromQuery] DuplicateCheckRequestDto request)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _service.ValidarDuplicadoAsync(organizationId, request);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    // RF-LO-14: detalle completo.
    [HttpGet("{id}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetById(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _service.GetDetailAsync(organizationId, id);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    // RF-LO-14: bitácora / historial de cambios.
    [HttpGet("{id}/historial")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetHistorial(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _service.GetHistorialAsync(organizationId, id);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    // RF-LO-01: crear (con verificación de duplicados RF-LO-13).
    [HttpPost]
    [ProducesResponseType(201)]
    public async Task<ActionResult> Create([FromBody] CreateLocalidadDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _service.CreateAsync(organizationId, userId, GetUserName(), dto,
            await HasPermissionAsync(LocalidadPermissions.CalendarManage));
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    // RF-LO-06: modificar.
    [HttpPut("{id}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateLocalidadDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _service.UpdateAsync(organizationId, id, userId, GetUserName(), dto,
            await HasPermissionAsync(LocalidadPermissions.CalendarManage));
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    // RF-LO-09: suspensión temporal.
    [HttpPost("{id}/suspender")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> Suspender(Guid id, [FromBody] SuspenderLocalidadDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _service.SuspenderAsync(organizationId, id, userId, GetUserName(), dto);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return NoContent();
    }

    // Levantar suspensión.
    [HttpPost("{id}/reactivar")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> Reactivar(Guid id, [FromBody] ReactivarLocalidadDto? dto)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _service.ReactivarAsync(organizationId, id, userId, GetUserName(), dto ?? new ReactivarLocalidadDto());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return NoContent();
    }

    // RF-LO-10: cierre / desactivación.
    [HttpPost("{id}/cerrar")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> Cerrar(Guid id, [FromBody] CerrarLocalidadDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _service.CerrarAsync(organizationId, id, userId, GetUserName(), dto);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return NoContent();
    }

    // ───────────────────────────── Sub-recursos ─────────────────────────────

    [HttpGet("{id}/servicios")]
    public async Task<ActionResult> GetServicios(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var result = await _service.GetServiciosAsync(organizationId, id);
        return result.IsSuccess ? Ok(result.Value) : StatusCode((int)result.StatusCode, result.Error);
    }

    [HttpPost("{id}/servicios")]
    [ProducesResponseType(201)]
    public async Task<ActionResult> AddServicio(Guid id, [FromBody] LocalidadServicioInputDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var result = await _service.AddServicioAsync(organizationId, id, dto);
        return result.IsSuccess ? StatusCode(201, result.Value) : StatusCode((int)result.StatusCode, result.Error);
    }

    [HttpDelete("{id}/servicios/{linkId}")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> RemoveServicio(Guid id, Guid linkId)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var result = await _service.RemoveServicioAsync(organizationId, id, linkId);
        return result.IsSuccess ? NoContent() : StatusCode((int)result.StatusCode, result.Error);
    }

    [HttpGet("{id}/responsables")]
    public async Task<ActionResult> GetResponsables(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var result = await _service.GetResponsablesAsync(organizationId, id);
        return result.IsSuccess ? Ok(result.Value) : StatusCode((int)result.StatusCode, result.Error);
    }

    [HttpPost("{id}/responsables")]
    [ProducesResponseType(201)]
    public async Task<ActionResult> AddResponsable(Guid id, [FromBody] LocalidadResponsableInputDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var result = await _service.AddResponsableAsync(organizationId, id, dto, await HasPermissionAsync(LocalidadPermissions.CalendarManage));
        return result.IsSuccess ? StatusCode(201, result.Value) : StatusCode((int)result.StatusCode, result.Error);
    }

    [HttpDelete("{id}/responsables/{linkId}")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> RemoveResponsable(Guid id, Guid linkId)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var result = await _service.RemoveResponsableAsync(organizationId, id, linkId);
        return result.IsSuccess ? NoContent() : StatusCode((int)result.StatusCode, result.Error);
    }

    [HttpGet("{id}/documentos")]
    public async Task<ActionResult> GetDocumentos(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var result = await _service.GetDocumentosAsync(organizationId, id);
        return result.IsSuccess ? Ok(result.Value) : StatusCode((int)result.StatusCode, result.Error);
    }

    [HttpPost("{id}/documentos")]
    [ProducesResponseType(201)]
    public async Task<ActionResult> AddDocumento(Guid id, [FromBody] LocalidadDocumentoInputDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var result = await _service.AddDocumentoAsync(organizationId, id, GetUserName(), dto);
        return result.IsSuccess ? StatusCode(201, result.Value) : StatusCode((int)result.StatusCode, result.Error);
    }

    [HttpDelete("{id}/documentos/{linkId}")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> RemoveDocumento(Guid id, Guid linkId)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var result = await _service.RemoveDocumentoAsync(organizationId, id, linkId);
        return result.IsSuccess ? NoContent() : StatusCode((int)result.StatusCode, result.Error);
    }

    [HttpGet("{id}/unidades-presencia")]
    public async Task<ActionResult> GetUnidadesPresencia(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var result = await _service.GetUnidadesPresenciaAsync(organizationId, id);
        return result.IsSuccess ? Ok(result.Value) : StatusCode((int)result.StatusCode, result.Error);
    }

    [HttpPost("{id}/unidades-presencia")]
    [ProducesResponseType(201)]
    public async Task<ActionResult> AddUnidadPresencia(Guid id, [FromBody] LocalidadUnidadPresenciaInputDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var result = await _service.AddUnidadPresenciaAsync(organizationId, id, dto);
        return result.IsSuccess ? StatusCode(201, result.Value) : StatusCode((int)result.StatusCode, result.Error);
    }

    [HttpDelete("{id}/unidades-presencia/{linkId}")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> RemoveUnidadPresencia(Guid id, Guid linkId)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var result = await _service.RemoveUnidadPresenciaAsync(organizationId, id, linkId);
        return result.IsSuccess ? NoContent() : StatusCode((int)result.StatusCode, result.Error);
    }
}
