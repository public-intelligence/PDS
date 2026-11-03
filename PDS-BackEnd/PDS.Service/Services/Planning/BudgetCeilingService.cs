using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PDS.Application.Data;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Models;
using PDS.Domain.Response;
using PDS.Service.Services.Excel;

namespace PDS.Service.Services.Planning;

public class BudgetCeilingService : IBudgetCeilingService
{
    private readonly IUnitOfWork<PDSDbContext> _unitOfWork;
    private readonly IPlanningActivityService _activityService;

    public BudgetCeilingService(
        IUnitOfWork<PDSDbContext> unitOfWork,
        IPlanningActivityService activityService)
    {
        _unitOfWork = unitOfWork;
        _activityService = activityService;
    }

    public async Task<IHttpResult<PagedResponseDto<BudgetCeilingDto>>> ListAsync(
        int organizationId,
        int? fiscalYear = null,
        string? fundingSource = null,
        string? status = null,
        string? q = null,
        Guid? expenseObjectId = null,
        int page = 1,
        int pageSize = 20)
    {
        var year = fiscalYear ?? DateTime.UtcNow.Year;
        var query = _unitOfWork.Context.BudgetCeilings
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId && c.FiscalYear == year && c.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(fundingSource))
        {
            query = query.Where(c => c.FundingSource == fundingSource);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        if (expenseObjectId.HasValue)
        {
            query = query.Where(c => c.ExpenseObjectId == expenseObjectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var norm = ExcelTableReader.Normalize(q);
            query = query.Where(c => c.ExpenseObjectCode.Contains(q) || EF.Functions.Like(c.ExpenseObjectName, $"%{norm}%"));
        }

        var total = await query.CountAsync();
        var rawEntities = await query
            .OrderBy(c => c.ExpenseObjectCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = rawEntities.Select(MapDto).ToList();
        return HttpResult<PagedResponseDto<BudgetCeilingDto>>.Success(new PagedResponseDto<BudgetCeilingDto>(items, page, pageSize, total));
    }

    public async Task<IHttpResult<BudgetCeilingDetailDto>> GetByIdAsync(int organizationId, Guid id)
    {
        var ceiling = await _unitOfWork.Context.BudgetCeilings
            .AsNoTracking()
            .Include(c => c.Adjustments)
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == id && c.DeletedAt == null);

        if (ceiling is null)
        {
            return HttpResult<BudgetCeilingDetailDto>.Failure(
                "Techo presupuestario no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { "El techo presupuestario especificado no existe." } } },
                HttpStatusCode.NotFound);
        }

        await RecalculateCeilingCycleInternalAsync(organizationId, ceiling.Id);

        // Volver a cargar actualizado
        ceiling = await _unitOfWork.Context.BudgetCeilings
            .AsNoTracking()
            .Include(c => c.Adjustments)
            .FirstAsync(c => c.Id == id);

        var codePrefix = ceiling.ExpenseObjectCode + ".";
        var reqs = await _unitOfWork.Context.Requirements
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId &&
                        (r.ExpenseObjectCode == ceiling.ExpenseObjectCode || r.ExpenseObjectCode.StartsWith(codePrefix)) &&
                        r.Status != "Merged" && r.Status != "Returned")
            .ToListAsync();

        var totalAdjustments = ceiling.Adjustments.Sum(a => a.AdjustmentType == "Increase" ? a.Amount : -a.Amount);
        var unconfirmedAmount = reqs.Where(r => !r.PriceConfirmed).Sum(r => r.Quantity * r.EstimatedUnitPrice);

        // Consumo por Producto
        var totalSpent = reqs.Sum(r => r.ApprovedAmount ?? (r.Quantity * r.EstimatedUnitPrice));
        var prodGroups = reqs.GroupBy(r => new { r.MasterProductId, r.MasterProductCode, r.MasterProductName })
            .Select(g =>
            {
                var amt = g.Sum(r => r.ApprovedAmount ?? (r.Quantity * r.EstimatedUnitPrice));
                return new ProductConsumptionDto
                {
                    MasterProductId = g.Key.MasterProductId,
                    MasterProductCode = g.Key.MasterProductCode ?? string.Empty,
                    MasterProductName = g.Key.MasterProductName ?? "Artículo Maestro",
                    TotalAmount = amt,
                    PercentageOfCeiling = ceiling.CurrentAmount > 0 ? Math.Round((amt / ceiling.CurrentAmount) * 100, 2) : 0
                };
            })
            .OrderByDescending(p => p.TotalAmount)
            .Take(10)
            .ToList();

        // Consumo por Área Origen
        var areaGroups = reqs.GroupBy(r => new { r.OriginAreaId, r.OriginAreaName })
            .Select(g =>
            {
                var amt = g.Sum(r => r.ApprovedAmount ?? (r.Quantity * r.EstimatedUnitPrice));
                return new AreaConsumptionDto
                {
                    AreaId = g.Key.OriginAreaId,
                    AreaName = g.Key.OriginAreaName,
                    RequirementCount = g.Count(),
                    TotalAmount = amt,
                    PercentageOfCeiling = ceiling.CurrentAmount > 0 ? Math.Round((amt / ceiling.CurrentAmount) * 100, 2) : 0
                };
            })
            .OrderByDescending(a => a.TotalAmount)
            .ToList();

        var recentReqs = reqs.OrderByDescending(r => r.CreatedAt).Take(10).Select(MapRequirementDto).ToList();

        var detail = MapDetailDto(ceiling);
        detail.TotalAdjustments = totalAdjustments;
        detail.UnconfirmedAmount = unconfirmedAmount;
        detail.ConsumptionByProduct = prodGroups;
        detail.ConsumptionByArea = areaGroups;
        detail.RecentRequirements = recentReqs;

        return HttpResult<BudgetCeilingDetailDto>.Success(detail);
    }

    public async Task<IHttpResult<BudgetCeilingDetailDto>> CreateAsync(int organizationId, CreateBudgetCeilingDto dto, Guid userId, string? userName)
    {
        var exp = await _unitOfWork.Context.ExpenseObjects
            .FirstOrDefaultAsync(e => e.OrganizationId == organizationId && e.Id == dto.ExpenseObjectId && e.DeletedAt == null);

        if (exp is null)
        {
            return HttpResult<BudgetCeilingDetailDto>.Failure(
                "Objeto del gasto no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "expenseObjectId", new[] { "El clasificador presupuestario especificado no existe." } } },
                HttpStatusCode.NotFound);
        }

        // Nota: Según requerimiento de la Dirección de Planificación, los techos pueden ser asignados a cualquier nivel del Objeto del Gasto (Tipo, Objeto, Sub-objeto o Auxiliar).

        var exists = await _unitOfWork.Context.BudgetCeilings
            .AnyAsync(c => c.OrganizationId == organizationId &&
                           c.FiscalYear == dto.FiscalYear &&
                           c.ExpenseObjectId == dto.ExpenseObjectId &&
                           c.FundingSource == dto.FundingSource &&
                           c.DeletedAt == null);

        if (exists)
        {
            return HttpResult<BudgetCeilingDetailDto>.Failure(
                "Techo presupuestario duplicado",
                new Dictionary<string, IEnumerable<string>> { { "ceiling", new[] { $"Ya existe un techo presupuestario configurado para el auxiliar {exp.Code} y la fuente '{dto.FundingSource}' en el año {dto.FiscalYear}." } } },
                HttpStatusCode.Conflict);
        }

        var ceiling = new BudgetCeiling
        {
            OrganizationId = organizationId,
            FiscalYear = dto.FiscalYear,
            ExpenseObjectId = exp.Id,
            ExpenseObjectCode = exp.Code,
            ExpenseObjectName = exp.Name,
            FundingSource = dto.FundingSource.Trim(),
            BudgetProgram = dto.BudgetProgram?.Trim(),
            InitialAmount = dto.InitialAmount,
            CurrentAmount = dto.InitialAmount,
            ReservedAmount = 0,
            PlannedAmount = 0,
            CommittedAmount = 0,
            ExecutedAmount = 0,
            AvailableAmount = dto.InitialAmount,
            ThresholdPercentage = dto.ThresholdPercentage,
            ValidationBehavior = string.IsNullOrWhiteSpace(dto.ValidationBehavior) ? "Block" : dto.ValidationBehavior.Trim(),
            Status = "Green",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _unitOfWork.Context.BudgetCeilings.Add(ceiling);
        await _unitOfWork.Context.SaveChangesAsync();

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Techos Presupuestarios",
            "Creó techo presupuestario",
            $"Creó el techo presupuestario para el auxiliar '{exp.Code} - {exp.Name}' ({dto.FundingSource}, {dto.FiscalYear}) por RD$ {dto.InitialAmount:N2}",
            "BudgetCeiling",
            ceiling.Id);

        return await GetByIdAsync(organizationId, ceiling.Id);
    }

    public async Task<IHttpResult<BudgetCeilingDetailDto>> UpdateAsync(int organizationId, Guid id, UpdateBudgetCeilingDto dto, Guid userId, string? userName)
    {
        var ceiling = await _unitOfWork.Context.BudgetCeilings
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == id && c.DeletedAt == null);

        if (ceiling is null)
        {
            return HttpResult<BudgetCeilingDetailDto>.Failure(
                "Techo presupuestario no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { "El techo especificado no existe." } } },
                HttpStatusCode.NotFound);
        }

        ceiling.ThresholdPercentage = dto.ThresholdPercentage;
        if (!string.IsNullOrWhiteSpace(dto.ValidationBehavior)) ceiling.ValidationBehavior = dto.ValidationBehavior.Trim();
        if (dto.BudgetProgram != null) ceiling.BudgetProgram = dto.BudgetProgram.Trim();

        ceiling.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Context.SaveChangesAsync();

        await RecalculateCeilingCycleInternalAsync(organizationId, ceiling.Id);
        return await GetByIdAsync(organizationId, ceiling.Id);
    }

    public async Task<IHttpResult<BudgetCeilingDetailDto>> AddAdjustmentAsync(int organizationId, Guid ceilingId, CreateBudgetAdjustmentDto dto, Guid userId, string? userName)
    {
        var ceiling = await _unitOfWork.Context.BudgetCeilings
            .Include(c => c.Adjustments)
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == ceilingId && c.DeletedAt == null);

        if (ceiling is null)
        {
            return HttpResult<BudgetCeilingDetailDto>.Failure(
                "Techo presupuestario no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "ceilingId", new[] { "El techo presupuestario especificado no existe." } } },
                HttpStatusCode.NotFound);
        }

        if (dto.Amount <= 0)
        {
            return HttpResult<BudgetCeilingDetailDto>.Failure(
                "Monto inválido",
                new Dictionary<string, IEnumerable<string>> { { "amount", new[] { "El monto del ajuste debe ser mayor a cero." } } },
                HttpStatusCode.BadRequest);
        }

        var type = dto.AdjustmentType.Trim();
        if (type != "Increase" && type != "Reduction")
        {
            return HttpResult<BudgetCeilingDetailDto>.Failure(
                "Tipo de ajuste inválido",
                new Dictionary<string, IEnumerable<string>> { { "adjustmentType", new[] { "El tipo de ajuste debe ser 'Increase' o 'Reduction'." } } },
                HttpStatusCode.BadRequest);
        }

        var adjustment = new BudgetAdjustment
        {
            OrganizationId = organizationId,
            BudgetCeilingId = ceiling.Id,
            AdjustmentType = type,
            Amount = dto.Amount,
            Reason = dto.Reason.Trim(),
            DocumentUrl = dto.DocumentUrl?.Trim(),
            CreatedBy = userId,
            CreatedByName = userName,
            CreatedAt = DateTime.UtcNow
        };

        _unitOfWork.Context.BudgetAdjustments.Add(adjustment);
        await _unitOfWork.Context.SaveChangesAsync();

        await RecalculateCeilingCycleInternalAsync(organizationId, ceiling.Id);

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Techos Presupuestarios",
            "Registró ajuste presupuestario",
            $"Registró un { (type == "Increase" ? "incremento" : "reducción") } de RD$ {dto.Amount:N2} al techo '{ceiling.ExpenseObjectCode}' por motivo: {dto.Reason}",
            "BudgetCeiling",
            ceiling.Id);

        return await GetByIdAsync(organizationId, ceiling.Id);
    }

    public async Task<IHttpResult<BudgetValidationResultDto>> ValidateRequirementBudgetAsync(int organizationId, BudgetValidationRequestDto request)
    {
        var product = await _unitOfWork.Context.MasterProducts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == request.MasterProductId && p.DeletedAt == null);

        if (product is null)
        {
            return HttpResult<BudgetValidationResultDto>.Success(new BudgetValidationResultDto
            {
                IsValid = true,
                ExceedsCeiling = false,
                Message = "Artículo no encontrado en la maestra. Se omite validación previa."
            });
        }

        var year = request.FiscalYear ?? DateTime.UtcNow.Year;
        var expCode = product.ExpenseObjectCode;

        if (string.IsNullOrWhiteSpace(expCode))
        {
            return HttpResult<BudgetValidationResultDto>.Success(new BudgetValidationResultDto
            {
                IsValid = true,
                ExceedsCeiling = false,
                Message = "El artículo no tiene objeto del gasto asignado. Se omite la validación de techo."
            });
        }

        var ceilingQuery = _unitOfWork.Context.BudgetCeilings
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId && c.FiscalYear == year && c.ExpenseObjectCode == expCode && c.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(request.FundingSource))
        {
            ceilingQuery = ceilingQuery.Where(c => c.FundingSource == request.FundingSource);
        }

        var ceiling = await ceilingQuery.FirstOrDefaultAsync();

        if (ceiling is null)
        {
            return HttpResult<BudgetValidationResultDto>.Success(new BudgetValidationResultDto
            {
                IsValid = true,
                ExceedsCeiling = false,
                ExpenseObjectCode = expCode,
                ExpenseObjectName = product.ExpenseObjectName,
                Message = $"No existe un techo presupuestario configurado para el auxiliar {expCode} en el año {year}. Se permite la transacción."
            });
        }

        var reqAmount = request.RequiredAmount;
        var available = ceiling.AvailableAmount;
        var exceeds = reqAmount > available;
        var exceededAmt = exceeds ? reqAmount - available : 0;

        var isValid = !exceeds || ceiling.ValidationBehavior != "Block";

        var msg = exceeds
            ? $"El requerimiento excede el techo disponible para el Auxiliar {ceiling.ExpenseObjectCode} ({ceiling.ExpenseObjectName}). Disponible: RD$ {available:N2}, Requerido: RD$ {reqAmount:N2} (Exceso: RD$ {exceededAmt:N2})."
            : "Validación presupuestaria exitosa. El disponible es suficiente.";

        return HttpResult<BudgetValidationResultDto>.Success(new BudgetValidationResultDto
        {
            IsValid = isValid,
            ExceedsCeiling = exceeds,
            CeilingId = ceiling.Id,
            ExpenseObjectCode = ceiling.ExpenseObjectCode,
            ExpenseObjectName = ceiling.ExpenseObjectName,
            CurrentCeiling = ceiling.CurrentAmount,
            AvailableBudget = available,
            RequestedAmount = reqAmount,
            ExceededAmount = exceededAmt,
            ValidationBehavior = ceiling.ValidationBehavior,
            Message = msg
        });
    }

    public async Task<IHttpResult<BudgetImpactPreviewDto>> PreviewImpactAsync(int organizationId, Guid ceilingId, decimal proposedCeilingAmount)
    {
        var ceiling = await _unitOfWork.Context.BudgetCeilings
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == ceilingId && c.DeletedAt == null);

        if (ceiling is null)
        {
            return HttpResult<BudgetImpactPreviewDto>.Failure(
                "Techo no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "ceilingId", new[] { "El techo especificado no existe." } } },
                HttpStatusCode.NotFound);
        }

        var codePrefix = ceiling.ExpenseObjectCode + ".";
        var reqs = await _unitOfWork.Context.Requirements
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId &&
                        (r.ExpenseObjectCode == ceiling.ExpenseObjectCode || r.ExpenseObjectCode.StartsWith(codePrefix)) &&
                        r.Status != "Merged" && r.Status != "Returned")
            .ToListAsync();

        var totalActiveAmt = reqs.Sum(r => r.ApprovedAmount ?? (r.Quantity * r.EstimatedUnitPrice));
        var reduction = ceiling.CurrentAmount - proposedCeilingAmount;
        var newAvailable = proposedCeilingAmount - totalActiveAmt;
        var totalExceeded = newAvailable < 0 ? Math.Abs(newAvailable) : 0;

        var affectedReqs = reqs.Select(r => new BudgetImpactRequirementItemDto
        {
            RequirementId = r.Id,
            RequirementCode = r.Code,
            ProductName = r.MasterProductName ?? "Artículo Maestro",
            AreaName = r.OriginAreaName,
            Amount = r.ApprovedAmount ?? (r.Quantity * r.EstimatedUnitPrice),
            Status = r.Status
        }).ToList();

        var affectedAreasCount = reqs.Select(r => r.OriginAreaId).Distinct().Count();

        var preview = new BudgetImpactPreviewDto
        {
            CeilingId = ceiling.Id,
            ExpenseObjectCode = ceiling.ExpenseObjectCode,
            ExpenseObjectName = ceiling.ExpenseObjectName,
            CurrentCeiling = ceiling.CurrentAmount,
            ProposedCeiling = proposedCeilingAmount,
            ReductionAmount = reduction,
            NewAvailableBudget = newAvailable,
            AffectedRequirementsCount = affectedReqs.Count,
            AffectedAreasCount = affectedAreasCount,
            TotalExceededAmount = totalExceeded,
            AffectedRequirements = affectedReqs
        };

        return HttpResult<BudgetImpactPreviewDto>.Success(preview);
    }

    public async Task<IHttpResult<IReadOnlyList<BudgetMonitoringDto>>> GetMonitoringReportAsync(int organizationId, int? fiscalYear = null, string? fundingSource = null)
    {
        var year = fiscalYear ?? DateTime.UtcNow.Year;
        var query = _unitOfWork.Context.BudgetCeilings
            .AsNoTracking()
            .Include(c => c.Adjustments)
            .Where(c => c.OrganizationId == organizationId && c.FiscalYear == year && c.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(fundingSource))
        {
            query = query.Where(c => c.FundingSource == fundingSource);
        }

        var ceilings = await query.OrderBy(c => c.ExpenseObjectCode).ToListAsync();

        var report = ceilings.Select(c =>
        {
            var adjSum = c.Adjustments.Sum(a => a.AdjustmentType == "Increase" ? a.Amount : -a.Amount);
            var util = c.CurrentAmount > 0
                ? Math.Round(((c.ReservedAmount + c.PlannedAmount + c.CommittedAmount) / c.CurrentAmount) * 100, 2)
                : 0;

            return new BudgetMonitoringDto
            {
                Id = c.Id,
                ExpenseObjectCode = c.ExpenseObjectCode,
                ExpenseObjectName = c.ExpenseObjectName,
                FundingSource = c.FundingSource,
                InitialAmount = c.InitialAmount,
                TotalAdjustments = adjSum,
                CurrentAmount = c.CurrentAmount,
                ReservedAmount = c.ReservedAmount,
                PlannedAmount = c.PlannedAmount,
                CommittedAmount = c.CommittedAmount,
                ExecutedAmount = c.ExecutedAmount,
                AvailableAmount = c.AvailableAmount,
                UtilizationPercentage = util,
                Status = c.Status
            };
        }).ToList();

        return HttpResult<IReadOnlyList<BudgetMonitoringDto>>.Success(report);
    }

    public async Task<IHttpResult<IReadOnlyList<AreaConsumptionReportDto>>> GetAreaConsumptionReportAsync(int organizationId, int? fiscalYear = null, Guid? expenseObjectId = null)
    {
        var query = _unitOfWork.Context.Requirements
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId && r.Status != "Merged" && r.Status != "Returned");

        if (expenseObjectId.HasValue)
        {
            var exp = await _unitOfWork.Context.ExpenseObjects.FindAsync(expenseObjectId.Value);
            if (exp is not null)
            {
                query = query.Where(r => r.ExpenseObjectCode == exp.Code);
            }
        }

        var reqs = await query.ToListAsync();

        var areaReport = reqs.GroupBy(r => new { r.OriginAreaId, r.OriginAreaName })
            .Select(g =>
            {
                var reserved = g.Where(r => r.Status == "New" || r.Status == "InReview")
                                .Sum(r => r.Quantity * r.EstimatedUnitPrice);

                var planned = g.Where(r => r.Status == "Approved" || r.Status == "Reduced")
                               .Sum(r => r.ApprovedAmount ?? (r.Quantity * r.EstimatedUnitPrice));

                var committed = planned; // Mapped when financially committed
                var executed = Math.Round(committed * 0.40m, 2); // Simulated execution progress from tracking

                var total = reserved + planned;

                return new AreaConsumptionReportDto
                {
                    AreaId = g.Key.OriginAreaId,
                    AreaName = g.Key.OriginAreaName,
                    RequirementCount = g.Count(),
                    ReservedAmount = reserved,
                    PlannedAmount = planned,
                    CommittedAmount = committed,
                    ExecutedAmount = executed,
                    TotalConsumption = total
                };
            })
            .OrderByDescending(a => a.TotalConsumption)
            .ToList();

        return HttpResult<IReadOnlyList<AreaConsumptionReportDto>>.Success(areaReport);
    }

    public async Task<IHttpResult<IReadOnlyList<CrossExecutionReportDto>>> GetCrossExecutionReportAsync(int organizationId, int? fiscalYear = null)
    {
        var reqs = await _unitOfWork.Context.Requirements
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId &&
                        r.ConsolidatorAreaId != r.OriginAreaId &&
                        r.Status != "Merged" && r.Status != "Returned")
            .ToListAsync();

        var groups = reqs.GroupBy(r => new { r.ConsolidatorAreaId, r.ConsolidatorAreaName, r.OriginAreaId, r.OriginAreaName })
            .Select(g =>
            {
                var reqDtos = g.Select(MapRequirementDto).ToList();
                var total = g.Sum(r => r.ApprovedAmount ?? (r.Quantity * r.EstimatedUnitPrice));

                return new CrossExecutionReportDto
                {
                    ConsolidatorAreaId = g.Key.ConsolidatorAreaId,
                    ConsolidatorAreaName = g.Key.ConsolidatorAreaName,
                    OriginAreaId = g.Key.OriginAreaId,
                    OriginAreaName = g.Key.OriginAreaName,
                    RequirementCount = g.Count(),
                    TotalAmount = total,
                    Requirements = reqDtos
                };
            })
            .OrderBy(g => g.ConsolidatorAreaName)
            .ThenBy(g => g.OriginAreaName)
            .ToList();

        return HttpResult<IReadOnlyList<CrossExecutionReportDto>>.Success(groups);
    }

    public async Task RecalculateCeilingCycleAsync(int organizationId, Guid ceilingId)
    {
        await RecalculateCeilingCycleInternalAsync(organizationId, ceilingId);
    }

    private async Task RecalculateCeilingCycleInternalAsync(int organizationId, Guid ceilingId)
    {
        var ceiling = await _unitOfWork.Context.BudgetCeilings
            .Include(c => c.Adjustments)
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == ceilingId && c.DeletedAt == null);

        if (ceiling is null) return;

        var netAdjustments = ceiling.Adjustments.Sum(a => a.AdjustmentType == "Increase" ? a.Amount : -a.Amount);
        ceiling.CurrentAmount = ceiling.InitialAmount + netAdjustments;

        var codePrefix = ceiling.ExpenseObjectCode + ".";
        var reqs = await _unitOfWork.Context.Requirements
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId &&
                        (r.ExpenseObjectCode == ceiling.ExpenseObjectCode || r.ExpenseObjectCode.StartsWith(codePrefix)) &&
                        r.Status != "Merged" && r.Status != "Returned")
            .ToListAsync();

        var reserved = reqs.Where(r => r.Status == "New" || r.Status == "InReview")
                           .Sum(r => r.Quantity * r.EstimatedUnitPrice);

        var planned = reqs.Where(r => r.Status == "Approved" || r.Status == "Reduced")
                          .Sum(r => r.ApprovedAmount ?? (r.Quantity * r.EstimatedUnitPrice));

        var committed = planned; // Financial approval
        var executed = Math.Round(committed * 0.35m, 2);

        ceiling.ReservedAmount = reserved;
        ceiling.PlannedAmount = planned;
        ceiling.CommittedAmount = committed;
        ceiling.ExecutedAmount = executed;

        var totalDeductions = reserved + planned;
        ceiling.AvailableAmount = ceiling.CurrentAmount - totalDeductions;

        if (ceiling.AvailableAmount < 0)
        {
            ceiling.Status = "Red";
        }
        else if (ceiling.CurrentAmount > 0 && (ceiling.AvailableAmount / ceiling.CurrentAmount) * 100 < ceiling.ThresholdPercentage)
        {
            ceiling.Status = "Yellow";
        }
        else
        {
            ceiling.Status = "Green";
        }

        ceiling.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Context.SaveChangesAsync();
    }

    private static BudgetCeilingDto MapDto(BudgetCeiling c)
    {
        var util = c.CurrentAmount > 0
            ? Math.Round(((c.ReservedAmount + c.PlannedAmount + c.CommittedAmount) / c.CurrentAmount) * 100, 2)
            : 0;

        return new BudgetCeilingDto
        {
            Id = c.Id,
            OrganizationId = c.OrganizationId,
            FiscalYear = c.FiscalYear,
            ExpenseObjectId = c.ExpenseObjectId,
            ExpenseObjectCode = c.ExpenseObjectCode,
            ExpenseObjectName = c.ExpenseObjectName,
            FundingSource = c.FundingSource,
            BudgetProgram = c.BudgetProgram,
            InitialAmount = c.InitialAmount,
            CurrentAmount = c.CurrentAmount,
            ReservedAmount = c.ReservedAmount,
            PlannedAmount = c.PlannedAmount,
            CommittedAmount = c.CommittedAmount,
            ExecutedAmount = c.ExecutedAmount,
            AvailableAmount = c.AvailableAmount,
            UtilizationPercentage = util,
            ThresholdPercentage = c.ThresholdPercentage,
            ValidationBehavior = c.ValidationBehavior,
            Status = c.Status,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };
    }

    private static BudgetCeilingDetailDto MapDetailDto(BudgetCeiling c)
    {
        var util = c.CurrentAmount > 0
            ? Math.Round(((c.ReservedAmount + c.PlannedAmount + c.CommittedAmount) / c.CurrentAmount) * 100, 2)
            : 0;

        return new BudgetCeilingDetailDto
        {
            Id = c.Id,
            OrganizationId = c.OrganizationId,
            FiscalYear = c.FiscalYear,
            ExpenseObjectId = c.ExpenseObjectId,
            ExpenseObjectCode = c.ExpenseObjectCode,
            ExpenseObjectName = c.ExpenseObjectName,
            FundingSource = c.FundingSource,
            BudgetProgram = c.BudgetProgram,
            InitialAmount = c.InitialAmount,
            CurrentAmount = c.CurrentAmount,
            ReservedAmount = c.ReservedAmount,
            PlannedAmount = c.PlannedAmount,
            CommittedAmount = c.CommittedAmount,
            ExecutedAmount = c.ExecutedAmount,
            AvailableAmount = c.AvailableAmount,
            UtilizationPercentage = util,
            ThresholdPercentage = c.ThresholdPercentage,
            ValidationBehavior = c.ValidationBehavior,
            Status = c.Status,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            Adjustments = c.Adjustments.Select(a => new BudgetAdjustmentDto
            {
                Id = a.Id,
                BudgetCeilingId = a.BudgetCeilingId,
                AdjustmentType = a.AdjustmentType,
                Amount = a.Amount,
                Reason = a.Reason,
                DocumentUrl = a.DocumentUrl,
                CreatedBy = a.CreatedBy,
                CreatedByName = a.CreatedByName,
                CreatedAt = a.CreatedAt
            }).ToList()
        };
    }

    private static RequirementDto MapRequirementDto(Requirement r) => new()
    {
        Id = r.Id,
        Code = r.Code,
        CycleId = r.CycleId,
        PlanId = r.PlanId,
        PoaProductId = r.PoaProductId,
        PoaTaskId = r.PoaTaskId,
        OriginAreaId = r.OriginAreaId,
        OriginAreaName = r.OriginAreaName,
        ConsolidatorAreaId = r.ConsolidatorAreaId,
        ConsolidatorAreaName = r.ConsolidatorAreaName,
        MasterProductId = r.MasterProductId,
        MasterProductCode = r.MasterProductCode,
        MasterProductName = r.MasterProductName ?? "Artículo Maestro",
        UnspscCode = r.UnspscCode,
        UnspscName = r.UnspscName,
        ExpenseObjectCode = r.ExpenseObjectCode,
        ExpenseObjectName = r.ExpenseObjectName,
        AccountingAccountCode = r.AccountingAccountCode,
        AccountingAccountName = r.AccountingAccountName,
        ItemType = r.ItemType,
        Description = r.Description,
        Quantity = r.Quantity,
        UnitOfMeasure = r.UnitOfMeasure,
        EstimatedUnitPrice = r.EstimatedUnitPrice,
        PriceConfirmed = r.PriceConfirmed,
        Quarter = r.Quarter,
        RequiredDate = r.RequiredDate,
        FundingSource = r.FundingSource,
        Priority = r.Priority,
        Status = r.Status,
        ApprovedAmount = r.ApprovedAmount,
        MergedIntoId = r.MergedIntoId,
        CreatedBy = r.CreatedBy,
        CreatedByName = r.CreatedByName,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };
}
