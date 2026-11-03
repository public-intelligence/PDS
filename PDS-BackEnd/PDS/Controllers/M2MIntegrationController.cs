using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Localidad;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.DTOs.Planning;
using PDS.Service.Services;
using PDS.Service.Services.Planning;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PDS.Controllers;

/// <summary>
/// API de Integración M2M (Machine-to-Machine) para consumo por sistemas de terceros.
/// Permite consultar catálogos de Localidades, Artículos, Áreas del Organigrama y Rubros,
/// además de la creación y actualización (escritura) de artículos en la Maestra Institucional.
/// </summary>
[Authorize]
[ApiController]
[Route("m2m/organization/{organizationId:int}")]
[Consumes("application/json")]
[Produces("application/json")]
public class M2MIntegrationController : ApiControllerBase
{
    private readonly ILocalidadService _localidadService;
    private readonly IMasterProductService _masterProductService;
    private readonly IOrgChartService _orgChartService;
    private readonly IRegulatoryCatalogService _regulatoryCatalogService;
    private readonly IPaginationService _paginationService;
    private readonly IUnifiedCodeService _unifiedCodeService;

    public M2MIntegrationController(
        ILocalidadService localidadService,
        IMasterProductService masterProductService,
        IOrgChartService orgChartService,
        IRegulatoryCatalogService regulatoryCatalogService,
        IPaginationService paginationService,
        IUnifiedCodeService unifiedCodeService)
    {
        _localidadService = localidadService;
        _masterProductService = masterProductService;
        _orgChartService = orgChartService;
        _regulatoryCatalogService = regulatoryCatalogService;
        _paginationService = paginationService;
        _unifiedCodeService = unifiedCodeService;
    }

    #region 1. LOCALIDADES (GET)

    /// <summary>
    /// M2M: lista de localidades de la organización (RF-LO-52). Devuelve todas las que cumplen los filtros en un solo
    /// arreglo JSON, ordenadas por <c>nombreOficial</c>, sin paginar y sin encabezados de paginación. Nunca incluye
    /// eliminadas; <c>estadoCodigo=ACTIVA</c> deja solo las operativas.
    /// </summary>
    [HttpGet("localidades")]
    [ProducesResponseType(typeof(IEnumerable<LocalidadListItemDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> GetLocalidades(
        [FromRoute] int organizationId,
        [FromQuery] LocalidadFilterDto filter)
    {
        var m2mCheck = await RequireM2MServiceAccountAsync();
        if (m2mCheck != null) return m2mCheck;

        var result = await _localidadService.ListAllAsync(organizationId, filter);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    #endregion

    #region 2. ARTÍCULOS MAESTROS (GET & WRITE)

    /// <summary>
    /// M2M: Consulta paginada y filtrable de la Maestra Institucional de Artículos.
    /// </summary>
    [HttpGet("articulos")]
    [ProducesResponseType(typeof(PagedResponseDto<MasterProductDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> GetArticulos(
        [FromRoute] int organizationId,
        [FromQuery] string? q,
        [FromQuery] string? state,
        [FromQuery] Guid? consolidatorAreaId,
        [FromQuery] bool? missingMapping,
        [FromQuery] bool? requiresReview,
        [FromQuery] string? unspscPrefix,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var m2mCheck = await RequireM2MServiceAccountAsync();
        if (m2mCheck != null) return m2mCheck;

        var result = await _masterProductService.ListAsync(
            organizationId, q, state, consolidatorAreaId, missingMapping, requiresReview, unspscPrefix, page, pageSize);

        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// M2M: Obtiene el detalle técnico y mapeos regulatorios de un artículo específico.
    /// </summary>
    [HttpGet("articulos/{id:guid}")]
    [ProducesResponseType(typeof(MasterProductDetailDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> GetArticuloById(
        [FromRoute] int organizationId,
        [FromRoute] Guid id)
    {
        var m2mCheck = await RequireM2MServiceAccountAsync();
        if (m2mCheck != null) return m2mCheck;

        var result = await _masterProductService.GetByIdAsync(organizationId, id);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// M2M (Escritura): Crea un nuevo artículo en la Maestra Institucional de Productos y Servicios.
    /// </summary>
    [HttpPost("articulos")]
    [ProducesResponseType(typeof(MasterProductDetailDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> CreateArticulo(
        [FromRoute] int organizationId,
        [FromBody] CreateMasterProductDto dto)
    {
        var m2mCheck = await RequireM2MServiceAccountAsync();
        if (m2mCheck != null) return m2mCheck;

        TryGetUserAndOrganizationIds(out var actorUserId, out _);
        var actorName = GetUserName() ?? "M2M Client Integration";

        var result = await _masterProductService.CreateAsync(organizationId, dto, actorUserId, actorName);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return CreatedAtAction(
            nameof(GetArticuloById),
            new { organizationId, id = result.Value!.Id },
            result.Value);
    }

    /// <summary>
    /// M2M (Escritura): Actualiza la información o mapeos regulatorios de un artículo existente.
    /// </summary>
    [HttpPut("articulos/{id:guid}")]
    [ProducesResponseType(typeof(MasterProductDetailDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> UpdateArticulo(
        [FromRoute] int organizationId,
        [FromRoute] Guid id,
        [FromBody] UpdateMasterProductDto dto)
    {
        var m2mCheck = await RequireM2MServiceAccountAsync();
        if (m2mCheck != null) return m2mCheck;

        TryGetUserAndOrganizationIds(out var actorUserId, out _);
        var actorName = GetUserName() ?? "M2M Client Integration";

        var result = await _masterProductService.UpdateAsync(organizationId, id, dto, actorUserId, actorName);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    #endregion

    #region 3. ÁREAS DEL ORGANIGRAMA (GET)

    /// <summary>
    /// M2M: Obtiene la estructura del organigrama público y las unidades/áreas organizacionales publicadas.
    /// </summary>
    [HttpGet("organigrama/areas")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> GetAreasOrganigrama(
        [FromRoute] int organizationId,
        [FromQuery] Guid? rootUnitId,
        [FromQuery] int? maxLevel,
        [FromQuery] bool includePositions = false)
    {
        var m2mCheck = await RequireM2MServiceAccountAsync();
        if (m2mCheck != null) return m2mCheck;

        var result = await _orgChartService.GetCurrentAsync(
            organizationId, rootUnitId, maxLevel, includePositions, requirePublic: false);

        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    #endregion

    #region 4. RUBROS Y CLASIFICADORES (GET)

    /// <summary>
    /// M2M: Consulta los objetos del gasto (clasificador presupuestario DIGEPRES).
    /// </summary>
    [HttpGet("rubros/objetos-gasto")]
    [ProducesResponseType(typeof(PagedResponseDto<ExpenseObjectDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> GetObjetosGasto(
        [FromRoute] int organizationId,
        [FromQuery] string? q,
        [FromQuery] bool? onlyDetail,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        var m2mCheck = await RequireM2MServiceAccountAsync();
        if (m2mCheck != null) return m2mCheck;

        var result = await _regulatoryCatalogService.ListExpenseObjectsAsync(
            organizationId, q, onlyDetail, page, pageSize);

        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// M2M: Consulta los perfiles de áreas consolidadoras y rubros institucionales de compra.
    /// </summary>
    [HttpGet("rubros/areas-consolidadoras")]
    [ProducesResponseType(typeof(IReadOnlyList<ConsolidatorAreaProfileDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> GetAreasConsolidadoras([FromRoute] int organizationId)
    {
        var m2mCheck = await RequireM2MServiceAccountAsync();
        if (m2mCheck != null) return m2mCheck;

        var result = await _regulatoryCatalogService.ListConsolidatorAreaProfilesAsync(organizationId);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// M2M: Consulta paginada y filtrable del Plan de Cuentas Contables (DIGECOB - M8).
    /// </summary>
    [HttpGet("rubros/cuentas-contables")]
    [ProducesResponseType(typeof(PagedResponseDto<AccountingAccountDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> GetCuentasContables(
        [FromRoute] int organizationId,
        [FromQuery] string? q,
        [FromQuery] bool? onlyPostable,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var m2mCheck = await RequireM2MServiceAccountAsync();
        if (m2mCheck != null) return m2mCheck;

        var result = await _regulatoryCatalogService.ListAccountingAccountsAsync(
            organizationId, q, onlyPostable, page, pageSize);

        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    #endregion

    #region 5. CATÁLOGO UNSPSC (GET)

    /// <summary>
    /// M2M: Consulta paginada y filtrable del Catálogo UNSPSC de Bienes y Servicios (DGCP).
    /// Soporta filtros por nivel jerárquico (Segment, Family, Class, Material), parentId, prefijo de código y búsqueda libre.
    /// </summary>
    [HttpGet("unspsc")]
    [ProducesResponseType(typeof(PagedResponseDto<UnifiedCodeDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> GetUnspsc(
        [FromRoute] int organizationId,
        [FromQuery] string? q = null,
        [FromQuery] string? level = null,
        [FromQuery] Guid? parentId = null,
        [FromQuery] string? codePrefix = null,
        [FromQuery] bool? enabled = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var m2mCheck = await RequireM2MServiceAccountAsync();
        if (m2mCheck != null) return m2mCheck;

        var result = await _unifiedCodeService.ListPagedAsync(
            organizationId, level, parentId, codePrefix, q, enabled, page, pageSize);

        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    #endregion
}
