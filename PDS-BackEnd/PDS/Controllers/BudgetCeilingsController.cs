using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.DTOs.Planning;
using PDS.Service.Services.Planning;

namespace PDS.Controllers;

[Authorize]
[ApiController]
[Route("budget-ceilings")]
[Produces("application/json")]
public class BudgetCeilingsController : ApiControllerBase
{
    private readonly IBudgetCeilingService _ceilingService;

    public BudgetCeilingsController(IBudgetCeilingService ceilingService)
    {
        _ceilingService = ceilingService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponseDto<BudgetCeilingDto>>> List(
        [FromQuery] int? fiscalYear,
        [FromQuery] string? fundingSource,
        [FromQuery] string? status,
        [FromQuery] string? q,
        [FromQuery] Guid? expenseObjectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!TryGetUserAndOrganizationIds(out var _, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var result = await _ceilingService.ListAsync(organizationId, fiscalYear, fundingSource, status, q, expenseObjectId, page, pageSize);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BudgetCeilingDetailDto>> GetById(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out var _, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var result = await _ceilingService.GetByIdAsync(organizationId, id);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<BudgetCeilingDetailDto>> Create([FromBody] CreateBudgetCeilingDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var result = await _ceilingService.CreateAsync(organizationId, dto, userId, GetUserName());
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BudgetCeilingDetailDto>> Update(Guid id, [FromBody] UpdateBudgetCeilingDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var result = await _ceilingService.UpdateAsync(organizationId, id, dto, userId, GetUserName());
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/adjustments")]
    public async Task<ActionResult<BudgetCeilingDetailDto>> AddAdjustment(Guid id, [FromBody] CreateBudgetAdjustmentDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var result = await _ceilingService.AddAdjustmentAsync(organizationId, id, dto, userId, GetUserName());
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpPost("validate")]
    public async Task<ActionResult<BudgetValidationResultDto>> ValidateRequirement([FromBody] BudgetValidationRequestDto request)
    {
        if (!TryGetUserAndOrganizationIds(out var _, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var result = await _ceilingService.ValidateRequirementBudgetAsync(organizationId, request);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/preview-impact")]
    public async Task<ActionResult<BudgetImpactPreviewDto>> PreviewImpact(Guid id, [FromQuery] decimal proposedAmount)
    {
        if (!TryGetUserAndOrganizationIds(out var _, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var result = await _ceilingService.PreviewImpactAsync(organizationId, id, proposedAmount);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("monitoring")]
    public async Task<ActionResult<IReadOnlyList<BudgetMonitoringDto>>> GetMonitoring([FromQuery] int? fiscalYear, [FromQuery] string? fundingSource)
    {
        if (!TryGetUserAndOrganizationIds(out var _, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var result = await _ceilingService.GetMonitoringReportAsync(organizationId, fiscalYear, fundingSource);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("area-consumption")]
    public async Task<ActionResult<IReadOnlyList<AreaConsumptionReportDto>>> GetAreaConsumption([FromQuery] int? fiscalYear, [FromQuery] Guid? expenseObjectId)
    {
        if (!TryGetUserAndOrganizationIds(out var _, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var result = await _ceilingService.GetAreaConsumptionReportAsync(organizationId, fiscalYear, expenseObjectId);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("cross-execution")]
    public async Task<ActionResult<IReadOnlyList<CrossExecutionReportDto>>> GetCrossExecution([FromQuery] int? fiscalYear)
    {
        if (!TryGetUserAndOrganizationIds(out var _, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var result = await _ceilingService.GetCrossExecutionReportAsync(organizationId, fiscalYear);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }
}
