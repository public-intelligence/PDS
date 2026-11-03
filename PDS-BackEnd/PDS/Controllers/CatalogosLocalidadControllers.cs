using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.CatalogoLocalidad;
using PDS.Domain.DTOs.Responsable;
using PDS.Domain.DTOs.Servicio;
using PDS.Service.Authorization;
using PDS.Service.Services;

namespace PDS.Controllers;

// ─────────────────────────── Tipo de localidad (catálogo global) ───────────────────────────
[Authorize]
[ApiController]
[Route("catalogos/tipos-localidad")]
[Produces("application/json")]
public class TiposLocalidadController : ApiControllerBase
{
    private readonly ITipoLocalidadService _service;
    public TiposLocalidadController(ITipoLocalidadService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var r = await _service.GetAllAsync();
        return r.IsSuccess ? Ok(r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(Guid id)
    {
        var r = await _service.GetAsync(id);
        return r.IsSuccess ? Ok(r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpPost]
    [Consumes("application/json")]
    public async Task<ActionResult> Create([FromBody] CreateTipoLocalidadDto dto)
    {
        var r = await _service.CreateAsync(dto);
        return r.IsSuccess ? StatusCode(201, r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpPut("{id}")]
    [Consumes("application/json")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateTipoLocalidadDto dto)
    {
        var r = await _service.UpdateAsync(id, dto);
        return r.IsSuccess ? Ok(r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var r = await _service.DeleteAsync(id);
        return r.IsSuccess ? NoContent() : StatusCode((int)r.StatusCode, r.Error);
    }
}

// ─────────────────── Funciones de localidad (catálogo global, RF-LO-45 / B30) ───────────────────
[Authorize]
[ApiController]
[Route("catalogos/funciones-localidad")]
[Produces("application/json")]
public class FuncionesLocalidadController(IFuncionLocalidadService service, IPaginationService paginationService) : ApiControllerBase
{
    // Lista y búsqueda devuelven un arreglo JSON plano; la paginación viaja en X-Current-Page, X-Page-Count, X-Page-Size y X-Total.
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FuncionLocalidadDto>), 200)]
    public Task<ActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        ListAsync(null, pageNumber, pageSize, ct);

    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<FuncionLocalidadDto>), 200)]
    public Task<ActionResult> Search([FromQuery] string? q, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        ListAsync(q, pageNumber, pageSize, ct);

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FuncionLocalidadDto), 200)]
    public async Task<ActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var r = await service.GetAsync(id, ct);
        return r.IsSuccess ? Ok(r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    // Un cuerpo JSON sin campos falla la validación del servicio con 400 por campo (no con el 400 genérico del binder).
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(FuncionLocalidadDto), 201)]
    public async Task<ActionResult> Create([FromBody] FuncionLocalidadUpsertDto? dto, CancellationToken ct = default)
    {
        var r = await service.CreateAsync(dto ?? new FuncionLocalidadUpsertDto(), ct);
        return r.IsSuccess ? StatusCode(201, r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(FuncionLocalidadDto), 200)]
    public async Task<ActionResult> Update(Guid id, [FromBody] FuncionLocalidadUpsertDto? dto, CancellationToken ct = default)
    {
        var r = await service.UpdateAsync(id, dto ?? new FuncionLocalidadUpsertDto(), ct);
        return r.IsSuccess ? Ok(r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var r = await service.DeleteAsync(id, ct);
        return r.IsSuccess ? NoContent() : StatusCode((int)r.StatusCode, r.Error);
    }

    private async Task<ActionResult> ListAsync(string? q, int pageNumber, int pageSize, CancellationToken ct)
    {
        var r = await service.ListAsync(q, pageNumber, pageSize, HttpContext, ct);
        if (!r.IsSuccess) return StatusCode((int)r.StatusCode, r.Error);

        paginationService.SetPaginationHeaders(Response, r.Value);
        return Ok(r.Value.Value);
    }
}

// ─────────────────────────── Zonas horarias IANA (catálogo de solo lectura, B05) ───────────────────────────
[Authorize]
[ApiController]
[Route("catalogos/zonas-horarias")]
[Produces("application/json")]
public class ZonasHorariasController : ApiControllerBase
{
    private readonly IZonaHorariaService _service;
    public ZonasHorariasController(IZonaHorariaService service) => _service = service;

    /// <summary>Lista las zonas horarias IANA; con <c>paisCodigo</c> (alfa-2) solo las de ese país.</summary>
    [HttpGet]
    public ActionResult GetAll([FromQuery] string? paisCodigo)
    {
        var r = _service.Get(paisCodigo);
        return r.IsSuccess ? Ok(r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }
}

// ─────────────────── Naturaleza administrativa (catálogo global) ───────────────────
[Authorize]
[ApiController]
[Route("catalogos/naturalezas-administrativas")]
[Produces("application/json")]
public class NaturalezasAdministrativasController : ApiControllerBase
{
    private readonly INaturalezaAdministrativaService _service;
    public NaturalezasAdministrativasController(INaturalezaAdministrativaService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var r = await _service.GetAllAsync();
        return r.IsSuccess ? Ok(r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(Guid id)
    {
        var r = await _service.GetAsync(id);
        return r.IsSuccess ? Ok(r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpPost]
    [Consumes("application/json")]
    public async Task<ActionResult> Create([FromBody] CreateNaturalezaAdministrativaDto dto)
    {
        var r = await _service.CreateAsync(dto);
        return r.IsSuccess ? StatusCode(201, r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpPut("{id}")]
    [Consumes("application/json")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateNaturalezaAdministrativaDto dto)
    {
        var r = await _service.UpdateAsync(id, dto);
        return r.IsSuccess ? Ok(r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var r = await _service.DeleteAsync(id);
        return r.IsSuccess ? NoContent() : StatusCode((int)r.StatusCode, r.Error);
    }
}

// ─────────────────────────── Estado de localidad (catálogo global) ───────────────────────────
[Authorize]
[ApiController]
[Route("catalogos/estados-localidad")]
[Produces("application/json")]
public class EstadosLocalidadController : ApiControllerBase
{
    private readonly IEstadoLocalidadService _service;
    public EstadosLocalidadController(IEstadoLocalidadService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var r = await _service.GetAllAsync();
        return r.IsSuccess ? Ok(r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(Guid id)
    {
        var r = await _service.GetAsync(id);
        return r.IsSuccess ? Ok(r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpPost]
    [Consumes("application/json")]
    public async Task<ActionResult> Create([FromBody] CreateEstadoLocalidadDto dto)
    {
        var r = await _service.CreateAsync(dto);
        return r.IsSuccess ? StatusCode(201, r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpPut("{id}")]
    [Consumes("application/json")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateEstadoLocalidadDto dto)
    {
        var r = await _service.UpdateAsync(id, dto);
        return r.IsSuccess ? Ok(r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var r = await _service.DeleteAsync(id);
        return r.IsSuccess ? NoContent() : StatusCode((int)r.StatusCode, r.Error);
    }
}

// ─────────────────────────── Servicios (por organización) ───────────────────────────
[Authorize]
[ApiController]
[Route("catalogos/servicios")]
[Produces("application/json")]
public class ServiciosController : ApiControllerBase
{
    private readonly IServicioService _service;
    private static readonly string[] tokenError = ["No se pudo obtener userId y organizationId del token."];
    public ServiciosController(IServicioService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var r = await _service.GetAllAsync(organizationId);
        return r.IsSuccess ? Ok(r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var r = await _service.GetAsync(organizationId, id);
        return r.IsSuccess ? Ok(r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpPost]
    [Consumes("application/json")]
    public async Task<ActionResult> Create([FromBody] CreateServicioDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var r = await _service.CreateAsync(organizationId, dto);
        return r.IsSuccess ? StatusCode(201, r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpPut("{id}")]
    [Consumes("application/json")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateServicioDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var r = await _service.UpdateAsync(organizationId, id, dto);
        return r.IsSuccess ? Ok(r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var r = await _service.DeleteAsync(organizationId, id);
        return r.IsSuccess ? NoContent() : StatusCode((int)r.StatusCode, r.Error);
    }
}

// ─────────────────────────── Responsables (por organización) ───────────────────────────
[Authorize]
[ApiController]
[Route("catalogos/responsables")]
[Produces("application/json")]
public class ResponsablesController : ApiControllerBase
{
    private readonly IResponsableService _service;
    private static readonly string[] tokenError = ["No se pudo obtener userId y organizationId del token."];
    public ResponsablesController(IResponsableService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var r = await _service.GetAllAsync(organizationId, await HasPermissionAsync(LocalidadPermissions.CalendarManage));
        return r.IsSuccess ? Ok(r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var r = await _service.GetAsync(organizationId, id, await HasPermissionAsync(LocalidadPermissions.CalendarManage));
        return r.IsSuccess ? Ok(r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpPost]
    [Consumes("application/json")]
    public async Task<ActionResult> Create([FromBody] CreateResponsableDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var r = await _service.CreateAsync(organizationId, dto, await HasPermissionAsync(LocalidadPermissions.CalendarManage));
        return r.IsSuccess ? StatusCode(201, r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpPut("{id}")]
    [Consumes("application/json")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateResponsableDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var r = await _service.UpdateAsync(organizationId, id, dto, await HasPermissionAsync(LocalidadPermissions.CalendarManage));
        return r.IsSuccess ? Ok(r.Value) : StatusCode((int)r.StatusCode, r.Error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        var r = await _service.DeleteAsync(organizationId, id);
        return r.IsSuccess ? NoContent() : StatusCode((int)r.StatusCode, r.Error);
    }
}
