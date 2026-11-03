using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PDS.Application.Data;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.DTOs.Reporting;
using PDS.Domain.Models;
using PDS.Domain.Response;
using PDS.Domain.Enums;
using PDS.Service.Services.Reporting;

namespace PDS.Service.Services.Planning;

public class ConsolidationService : IConsolidationService
{
    private readonly IUnitOfWork<PDSDbContext> _unitOfWork;
    private readonly IRepository<AnnualPlan> _planRepository;
    private readonly IRepository<Requirement> _requirementRepository;
    private readonly IRepository<PlanningCycle> _cycleRepository;
    private readonly IPlanningActivityService _activityService;
    private readonly IEnumerable<IReportRenderer> _reportRenderers;

    public ConsolidationService(
        IUnitOfWork<PDSDbContext> unitOfWork,
        IRepository<AnnualPlan> planRepository,
        IRepository<Requirement> requirementRepository,
        IRepository<PlanningCycle> cycleRepository,
        IPlanningActivityService activityService,
        IEnumerable<IReportRenderer> reportRenderers)
    {
        _unitOfWork = unitOfWork;
        _planRepository = planRepository;
        _requirementRepository = requirementRepository;
        _cycleRepository = cycleRepository;
        _activityService = activityService;
        _reportRenderers = reportRenderers;
    }

    public async Task<IHttpResult<ConsolidationBoardDto>> GetBoardAsync(int organizationId, Guid cycleId)
    {
        var cycle = await _unitOfWork.Context.PlanningCycles
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == cycleId);

        if (cycle is null)
        {
            return HttpResult<ConsolidationBoardDto>.Failure(
                "Ciclo no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "cycleId", new[] { "El ciclo especificado no existe." } } },
                HttpStatusCode.NotFound);
        }

        var plans = await _unitOfWork.Context.AnnualPlans
            .AsNoTracking()
            .Where(p => p.OrganizationId == organizationId && p.CycleId == cycleId)
            .ToListAsync();

        var plansByStatus = plans
            .GroupBy(p => p.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        var requirements = await _unitOfWork.Context.Requirements
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId && r.CycleId == cycleId && r.Status != "Merged")
            .ToListAsync();

        var groups = requirements
            .GroupBy(r => new { r.ConsolidatorAreaId, r.ConsolidatorAreaName })
            .Select(g =>
            {
                var reqDtos = g.Select(MapRequirementDto).ToList();
                var reqsByStatus = g.GroupBy(r => r.Status).ToDictionary(sg => sg.Key, sg => sg.Count());
                var total = g.Sum(r => r.ApprovedAmount ?? (r.Quantity * r.EstimatedUnitPrice));

                return new ConsolidatorGroupDto
                {
                    ConsolidatorAreaId = g.Key.ConsolidatorAreaId,
                    ConsolidatorAreaName = g.Key.ConsolidatorAreaName,
                    RequirementCount = g.Count(),
                    TotalAmount = total,
                    RequirementsByStatus = reqsByStatus,
                    Requirements = reqDtos
                };
            })
            .OrderBy(g => g.ConsolidatorAreaName)
            .ToList();

        var totalApprovedAmount = requirements
            .Where(r => r.Status == "Approved" || r.Status == "Reduced")
            .Sum(r => r.ApprovedAmount ?? (r.Quantity * r.EstimatedUnitPrice));

        var board = new ConsolidationBoardDto
        {
            CycleId = cycle.Id,
            CycleName = cycle.Name,
            CycleYear = cycle.Year,
            PlansByStatus = plansByStatus,
            TotalApprovedAmount = totalApprovedAmount,
            ConsolidatorGroups = groups
        };

        return HttpResult<ConsolidationBoardDto>.Success(board);
    }

    public async Task<IHttpResult<ProductDemandBoardDto>> GetProductBoardAsync(int organizationId, Guid cycleId, Guid? consolidatorAreaId = null)
    {
        var cycle = await _unitOfWork.Context.PlanningCycles
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == cycleId);

        if (cycle is null)
        {
            return HttpResult<ProductDemandBoardDto>.Failure(
                "Ciclo no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "cycleId", new[] { "El ciclo especificado no existe." } } },
                HttpStatusCode.NotFound);
        }

        var query = _unitOfWork.Context.Requirements
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId && r.CycleId == cycleId && r.Status != "Merged");

        if (consolidatorAreaId.HasValue)
        {
            query = query.Where(r => r.ConsolidatorAreaId == consolidatorAreaId.Value);
        }

        var requirements = await query.ToListAsync();

        var ceilings = await _unitOfWork.Context.BudgetCeilings
            .AsNoTracking()
            .Where(b => b.OrganizationId == organizationId && (b.CycleId == cycleId || b.FiscalYear == cycle.Year))
            .ToListAsync();

        var ceilingMap = ceilings
            .Where(b => !string.IsNullOrWhiteSpace(b.ExpenseObjectCode))
            .GroupBy(b => b.ExpenseObjectCode)
            .ToDictionary(g => g.Key, g => g.First());

        var groups = requirements
            .GroupBy(r => new { r.MasterProductId, r.MasterProductCode, r.MasterProductName, r.UnspscCode, r.ExpenseObjectCode, r.ExpenseObjectName, r.AccountingAccountCode })
            .Select(g =>
            {
                var reqDtos = g.Select(MapRequirementDto).ToList();
                var totalQty = g.Sum(r => r.Quantity);
                var totalAmt = g.Sum(r => r.Quantity * r.EstimatedUnitPrice);
                var totalApproved = g.Where(r => r.Status == "Approved" || r.Status == "Reduced")
                                     .Sum(r => r.ApprovedAmount ?? (r.Quantity * r.EstimatedUnitPrice));
                var distinctAreas = g.Select(r => r.OriginAreaId).Distinct().Count();

                var c = !string.IsNullOrWhiteSpace(g.Key.ExpenseObjectCode) && ceilingMap.TryGetValue(g.Key.ExpenseObjectCode, out var foundC) ? foundC : null;

                return new ProductDemandGroupDto
                {
                    MasterProductId = g.Key.MasterProductId,
                    MasterProductCode = g.Key.MasterProductCode,
                    MasterProductName = g.Key.MasterProductName ?? "Artículo Maestro",
                    UnspscCode = g.Key.UnspscCode,
                    ExpenseObjectCode = g.Key.ExpenseObjectCode,
                    ExpenseObjectName = g.Key.ExpenseObjectName,
                    AccountingAccountCode = g.Key.AccountingAccountCode,
                    TotalQuantity = totalQty,
                    RequirementCount = g.Count(),
                    RequestingAreaCount = distinctAreas,
                    TotalAmount = totalAmt,
                    TotalApprovedAmount = totalApproved,
                    CeilingInitialAmount = c?.InitialAmount,
                    CeilingCurrentAmount = c?.CurrentAmount,
                    CeilingAvailableAmount = c?.AvailableAmount,
                    CeilingUtilizationPercentage = c != null && c.CurrentAmount > 0 ? Math.Round((c.PlannedAmount / c.CurrentAmount) * 100, 2) : 0,
                    CeilingStatus = c?.Status,
                    Requirements = reqDtos
                };
            })
            .OrderBy(g => g.MasterProductName)
            .ToList();

        var expenseGroups = requirements
            .GroupBy(r => new { Code = r.ExpenseObjectCode ?? "Sin Objeto", Name = r.ExpenseObjectName ?? "Objeto del Gasto no asignado" })
            .Select(eg =>
            {
                var egReqs = eg.Select(MapRequirementDto).ToList();
                var totalQty = eg.Sum(r => r.Quantity);
                var totalAmt = eg.Sum(r => r.Quantity * r.EstimatedUnitPrice);
                var totalApproved = eg.Where(r => r.Status == "Approved" || r.Status == "Reduced")
                                      .Sum(r => r.ApprovedAmount ?? (r.Quantity * r.EstimatedUnitPrice));
                var distinctAreas = eg.Select(r => r.OriginAreaId).Distinct().Count();

                var c = ceilingMap.TryGetValue(eg.Key.Code, out var foundC) ? foundC : null;
                var prodGroupsInEg = groups.Where(pg => (pg.ExpenseObjectCode ?? "Sin Objeto") == eg.Key.Code).ToList();

                return new ExpenseObjectDemandGroupDto
                {
                    ExpenseObjectCode = eg.Key.Code,
                    ExpenseObjectName = eg.Key.Name,
                    TotalQuantity = totalQty,
                    RequirementCount = eg.Count(),
                    RequestingAreaCount = distinctAreas,
                    TotalAmount = totalAmt,
                    TotalApprovedAmount = totalApproved,
                    CeilingInitialAmount = c?.InitialAmount,
                    CeilingCurrentAmount = c?.CurrentAmount,
                    CeilingAvailableAmount = c?.AvailableAmount,
                    CeilingUtilizationPercentage = c != null && c.CurrentAmount > 0 ? Math.Round((c.PlannedAmount / c.CurrentAmount) * 100, 2) : 0,
                    CeilingStatus = c?.Status,
                    ProductGroups = prodGroupsInEg,
                    Requirements = egReqs
                };
            })
            .OrderBy(eg => eg.ExpenseObjectCode)
            .ToList();

        var totalBoardAmount = groups.Sum(g => g.TotalAmount);
        var totalBoardApproved = groups.Sum(g => g.TotalApprovedAmount);

        var board = new ProductDemandBoardDto
        {
            CycleId = cycle.Id,
            CycleName = cycle.Name,
            CycleYear = cycle.Year,
            TotalAmount = totalBoardAmount,
            TotalApprovedAmount = totalBoardApproved,
            ExpenseObjectGroups = expenseGroups,
            ProductGroups = groups
        };

        return HttpResult<ProductDemandBoardDto>.Success(board);
    }

    public async Task<IHttpResult> OpenAdjustmentsPhaseAsync(int organizationId, Guid cycleId, Guid userId, string? userName)
    {
        var plans = await _unitOfWork.Context.AnnualPlans
            .Where(p => p.OrganizationId == organizationId && p.CycleId == cycleId && (p.Status == AnnualPlanStatuses.ListoParaRevision || p.Status == AnnualPlanStatuses.Observado))
            .ToListAsync();

        if (plans.Count == 0)
        {
            return HttpResult.Failure(
                "Sin planes elegibles",
                new Dictionary<string, IEnumerable<string>> { { "cycleId", new[] { "No hay planes en estado 'Listo para revisión' u 'Observado' para abrir la fase de ajustes." } } },
                HttpStatusCode.Conflict);
        }

        foreach (var p in plans)
        {
            p.Status = AnnualPlanStatuses.EnAjustes;
            p.UpdatedAt = DateTime.UtcNow;
        }

        await _unitOfWork.Context.SaveChangesAsync();

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Consolidación",
            "Abrió fase de ajustes",
            $"Abrió la fase de ajustes para {plans.Count} planes del ciclo",
            "PlanningCycle",
            cycleId);

        return HttpResult.Success();
    }

    public async Task<IHttpResult> SubmitAdjustmentsAsync(int organizationId, Guid planId, Guid userId, string? userName)
    {
        var plan = await _unitOfWork.Context.AnnualPlans
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == planId);

        if (plan is null)
        {
            return HttpResult.Failure(
                "Plan no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "planId", new[] { "El plan no existe." } } },
                HttpStatusCode.NotFound);
        }

        if (plan.Status != AnnualPlanStatuses.EnAjustes)
        {
            return HttpResult.Failure(
                "Estado inválido",
                new Dictionary<string, IEnumerable<string>> { { "status", new[] { "Solo se pueden enviar ajustes de un plan en estado 'En ajustes'." } } },
                HttpStatusCode.Conflict);
        }

        plan.Status = AnnualPlanStatuses.ListoParaRevision;
        plan.Version += 1;
        plan.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Context.SaveChangesAsync();

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Consolidación",
            "Envió ajustes de plan",
            $"Envió los ajustes del plan '{plan.Name}' (v{plan.Version}) para revisión",
            "AnnualPlan",
            plan.Id);

        return HttpResult.Success();
    }

    public async Task<IHttpResult<byte[]>> ExportPaccAsync(int organizationId, Guid cycleId)
    {
        var cycle = await _unitOfWork.Context.PlanningCycles
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == cycleId);

        if (cycle is null)
        {
            return HttpResult<byte[]>.Failure(
                "Ciclo no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "cycleId", new[] { "El ciclo especificado no existe." } } },
                HttpStatusCode.NotFound);
        }

        var reqs = await _unitOfWork.Context.Requirements
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId && r.CycleId == cycleId && (r.Status == "Approved" || r.Status == "Reduced"))
            .OrderBy(r => r.Code)
            .ToListAsync();

        var directMappings = await _unitOfWork.Context.MastersoftDigecobMappings
            .AsNoTracking()
            .Where(m => m.OrganizationId == organizationId)
            .Include(m => m.MastersoftAccount)
            .ToDictionaryAsync(m => m.DigecobAccountCode.Trim(), m => (m.MastersoftAccountCode, m.MastersoftAccount?.Name ?? string.Empty), StringComparer.OrdinalIgnoreCase);

        var digecobParents = await _unitOfWork.Context.AccountingAccounts
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId && a.DeletedAt == null)
            .ToDictionaryAsync(a => a.Code.Trim(), a => a.ParentCode, StringComparer.OrdinalIgnoreCase);

        var columns = new List<ReportColumn>
        {
            new() { Header = "Código" },
            new() { Header = "Área Origen" },
            new() { Header = "Área Consolidadora" },
            new() { Header = "Artículo" },
            new() { Header = "UNSPSC" },
            new() { Header = "Objeto del Gasto" },
            new() { Header = "Descripción Objeto" },
            new() { Header = "Cuenta DIGECOB" },
            new() { Header = "Descripción DIGECOB" },
            new() { Header = "Código MasterSoft" },
            new() { Header = "Descripción MasterSoft" },
            new() { Header = "Descripción Requerimiento" },
            new() { Header = "Cantidad", IsNumeric = true },
            new() { Header = "Unidad Medida" },
            new() { Header = "Precio Unitario (RD$)", IsNumeric = true },
            new() { Header = "Monto Aprobado (RD$)", IsNumeric = true },
            new() { Header = "Trimestre" },
            new() { Header = "Fuente Financiamiento" },
            new() { Header = "Prioridad" }
        };

        var rows = reqs.Select(r =>
        {
            var msRes = RegulatoryCatalogService.ResolveMastersoftMapping(r.AccountingAccountCode ?? string.Empty, directMappings, digecobParents);
            return new ReportTableRow
            {
                Cells = new string?[]
                {
                    r.Code,
                    r.OriginAreaName,
                    r.ConsolidatorAreaName,
                    r.MasterProductName,
                    r.UnspscCode,
                    r.ExpenseObjectCode,
                    r.ExpenseObjectName,
                    r.AccountingAccountCode,
                    r.AccountingAccountName,
                    msRes.MastersoftCode ?? "",
                    msRes.MastersoftName ?? "",
                    r.Description,
                    r.Quantity.ToString("F2"),
                    r.UnitOfMeasure,
                    r.EstimatedUnitPrice.ToString("F2"),
                    (r.ApprovedAmount ?? (r.Quantity * r.EstimatedUnitPrice)).ToString("F2"),
                    r.Quarter,
                    r.FundingSource ?? "Fondo General",
                    r.Priority
                }
            };
        }).ToList();

        var table = new ReportTable
        {
            Title = $"Plan Anual de Compras y Contrataciones (PACC) {cycle.Year}",
            Context = new ReportContextLabelDto
            {
                VersionName = $"Ciclo {cycle.Name} ({cycle.Year})",
                GeneratedAtUtc = DateTime.UtcNow,
                GeneratedByName = "Sistema PDS"
            },
            Columns = columns,
            Rows = rows
        };

        var xlsxRenderer = _reportRenderers.FirstOrDefault(r => r.Format == PDS.Domain.Enums.ReportFormat.Xlsx)
            ?? throw new InvalidOperationException("No se encontró el renderizador XlsxReportRenderer.");

        var rendered = xlsxRenderer.Render(table);
        return HttpResult<byte[]>.Success(rendered.Content);
    }

    private static RequirementDto MapRequirementDto(Requirement r)
    {
        return new RequirementDto
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
            AppliedRule = r.AppliedRule,
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

    public async Task<IHttpResult<ExpenseObjectsBoardPagedDto>> GetExpenseObjectsBoardAsync(
        int organizationId,
        Guid cycleId,
        int pageNumber = 1,
        int pageSize = 40,
        string? search = null)
    {
        var cycle = await _unitOfWork.Context.PlanningCycles
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == cycleId);

        if (cycle is null)
        {
            return HttpResult<ExpenseObjectsBoardPagedDto>.Failure(
                "Ciclo no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "cycleId", new[] { "El ciclo especificado no existe." } } },
                HttpStatusCode.NotFound);
        }

        var requirements = await _unitOfWork.Context.Requirements
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId && r.CycleId == cycleId && r.Status != "Merged")
            .ToListAsync();

        var ceilings = await _unitOfWork.Context.BudgetCeilings
            .AsNoTracking()
            .Where(b => b.OrganizationId == organizationId && (b.CycleId == cycleId || b.FiscalYear == cycle.Year))
            .ToListAsync();

        var ceilingMap = ceilings
            .Where(b => !string.IsNullOrWhiteSpace(b.ExpenseObjectCode))
            .GroupBy(b => b.ExpenseObjectCode)
            .ToDictionary(g => g.Key, g => g.First());

        var allProductGroups = requirements
            .GroupBy(r => new { r.MasterProductId, r.MasterProductCode, r.MasterProductName, r.UnspscCode, r.ExpenseObjectCode, r.ExpenseObjectName, r.AccountingAccountCode })
            .Select(g =>
            {
                var reqDtos = g.Select(MapRequirementDto).ToList();
                var totalQty = g.Sum(r => r.Quantity);
                var totalAmt = g.Sum(r => r.Quantity * r.EstimatedUnitPrice);
                var totalApproved = g.Where(r => r.Status == "Approved" || r.Status == "Reduced")
                                     .Sum(r => r.ApprovedAmount ?? (r.Quantity * r.EstimatedUnitPrice));
                var distinctAreas = g.Select(r => r.OriginAreaId).Distinct().Count();
                var c = !string.IsNullOrWhiteSpace(g.Key.ExpenseObjectCode) && ceilingMap.TryGetValue(g.Key.ExpenseObjectCode, out var foundC) ? foundC : null;

                return new ProductDemandGroupDto
                {
                    MasterProductId = g.Key.MasterProductId,
                    MasterProductCode = g.Key.MasterProductCode,
                    MasterProductName = g.Key.MasterProductName ?? "Artículo Maestro",
                    UnspscCode = g.Key.UnspscCode,
                    ExpenseObjectCode = g.Key.ExpenseObjectCode,
                    ExpenseObjectName = g.Key.ExpenseObjectName,
                    AccountingAccountCode = g.Key.AccountingAccountCode,
                    TotalQuantity = totalQty,
                    RequirementCount = g.Count(),
                    RequestingAreaCount = distinctAreas,
                    TotalAmount = totalAmt,
                    TotalApprovedAmount = totalApproved,
                    CeilingInitialAmount = c?.InitialAmount,
                    CeilingCurrentAmount = c?.CurrentAmount,
                    CeilingAvailableAmount = c?.AvailableAmount,
                    CeilingUtilizationPercentage = c != null && c.CurrentAmount > 0 ? Math.Round((c.PlannedAmount / c.CurrentAmount) * 100, 2) : 0,
                    CeilingStatus = c?.Status,
                    Requirements = reqDtos
                };
            })
            .ToList();

        var allExpenseObjectGroups = allProductGroups
            .GroupBy(p => new { Code = p.ExpenseObjectCode ?? "SIN_OBJETO", Name = p.ExpenseObjectName ?? "Sin Objeto del Gasto Especificado" })
            .Select(eg =>
            {
                var prods = eg.OrderBy(p => p.MasterProductName).ToList();
                var totalQty = prods.Sum(p => p.TotalQuantity);
                var totalAmt = prods.Sum(p => p.TotalAmount);
                var totalApproved = prods.Sum(p => p.TotalApprovedAmount);
                var reqCount = prods.Sum(p => p.RequirementCount);
                var requestingAreaCount = prods.SelectMany(p => p.Requirements).Select(r => r.OriginAreaId).Distinct().Count();
                var c = ceilingMap.TryGetValue(eg.Key.Code, out var foundC) ? foundC : null;

                return new ExpenseObjectDemandGroupDto
                {
                    ExpenseObjectCode = eg.Key.Code,
                    ExpenseObjectName = eg.Key.Name,
                    TotalQuantity = totalQty,
                    RequirementCount = reqCount,
                    RequestingAreaCount = requestingAreaCount,
                    TotalAmount = totalAmt,
                    TotalApprovedAmount = totalApproved,
                    CeilingInitialAmount = c?.InitialAmount,
                    CeilingCurrentAmount = c?.CurrentAmount,
                    CeilingAvailableAmount = c?.AvailableAmount,
                    CeilingUtilizationPercentage = c != null && c.CurrentAmount > 0 ? Math.Round((c.PlannedAmount / c.CurrentAmount) * 100, 2) : 0,
                    CeilingStatus = c?.Status,
                    ProductGroups = prods,
                    Requirements = prods.SelectMany(p => p.Requirements).ToList()
                };
            })
            .OrderBy(eg => eg.ExpenseObjectCode)
            .ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            allExpenseObjectGroups = allExpenseObjectGroups
                .Where(eg => eg.ExpenseObjectCode.ToLower().Contains(q) || (eg.ExpenseObjectName != null && eg.ExpenseObjectName.ToLower().Contains(q)))
                .ToList();
        }

        var totalAmount = allExpenseObjectGroups.Sum(eg => eg.TotalAmount);
        var totalApprovedAmount = allExpenseObjectGroups.Sum(eg => eg.TotalApprovedAmount);
        var totalRequirements = allExpenseObjectGroups.Sum(eg => eg.RequirementCount);

        var totalCount = allExpenseObjectGroups.Count;
        var validPageSize = pageSize > 0 ? pageSize : 40;
        var validPageNumber = pageNumber > 0 ? pageNumber : 1;
        var totalPages = (int)Math.Ceiling(totalCount / (double)validPageSize);

        var pagedGroups = allExpenseObjectGroups
            .Skip((validPageNumber - 1) * validPageSize)
            .Take(validPageSize)
            .ToList();

        var dto = new ExpenseObjectsBoardPagedDto
        {
            CycleId = cycle.Id,
            CycleName = cycle.Name,
            CycleYear = cycle.Year,
            TotalAmount = totalAmount,
            TotalApprovedAmount = totalApprovedAmount,
            TotalRequirements = totalRequirements,
            PageNumber = validPageNumber,
            PageSize = validPageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            ExpenseObjectGroups = pagedGroups
        };

        return HttpResult<ExpenseObjectsBoardPagedDto>.Success(dto);
    }

    public async Task<IHttpResult<ProductsBoardPagedDto>> GetProductsBoardAsync(
        int organizationId,
        Guid cycleId,
        Guid? consolidatorAreaId = null,
        int pageNumber = 1,
        int pageSize = 40,
        string? search = null)
    {
        var cycle = await _unitOfWork.Context.PlanningCycles
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == cycleId);

        if (cycle is null)
        {
            return HttpResult<ProductsBoardPagedDto>.Failure(
                "Ciclo no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "cycleId", new[] { "El ciclo especificado no existe." } } },
                HttpStatusCode.NotFound);
        }

        var query = _unitOfWork.Context.Requirements
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId && r.CycleId == cycleId && r.Status != "Merged");

        if (consolidatorAreaId.HasValue)
        {
            query = query.Where(r => r.ConsolidatorAreaId == consolidatorAreaId.Value);
        }

        var requirements = await query.ToListAsync();

        var ceilings = await _unitOfWork.Context.BudgetCeilings
            .AsNoTracking()
            .Where(b => b.OrganizationId == organizationId && (b.CycleId == cycleId || b.FiscalYear == cycle.Year))
            .ToListAsync();

        var ceilingMap = ceilings
            .Where(b => !string.IsNullOrWhiteSpace(b.ExpenseObjectCode))
            .GroupBy(b => b.ExpenseObjectCode)
            .ToDictionary(g => g.Key, g => g.First());

        var groups = requirements
            .GroupBy(r => new { r.MasterProductId, r.MasterProductCode, r.MasterProductName, r.UnspscCode, r.ExpenseObjectCode, r.ExpenseObjectName, r.AccountingAccountCode })
            .Select(g =>
            {
                var reqDtos = g.Select(MapRequirementDto).ToList();
                var totalQty = g.Sum(r => r.Quantity);
                var totalAmt = g.Sum(r => r.Quantity * r.EstimatedUnitPrice);
                var totalApproved = g.Where(r => r.Status == "Approved" || r.Status == "Reduced")
                                     .Sum(r => r.ApprovedAmount ?? (r.Quantity * r.EstimatedUnitPrice));
                var distinctAreas = g.Select(r => r.OriginAreaId).Distinct().Count();
                var c = !string.IsNullOrWhiteSpace(g.Key.ExpenseObjectCode) && ceilingMap.TryGetValue(g.Key.ExpenseObjectCode, out var foundC) ? foundC : null;

                return new ProductDemandGroupDto
                {
                    MasterProductId = g.Key.MasterProductId,
                    MasterProductCode = g.Key.MasterProductCode,
                    MasterProductName = g.Key.MasterProductName ?? "Artículo Maestro",
                    UnspscCode = g.Key.UnspscCode,
                    ExpenseObjectCode = g.Key.ExpenseObjectCode,
                    ExpenseObjectName = g.Key.ExpenseObjectName,
                    AccountingAccountCode = g.Key.AccountingAccountCode,
                    TotalQuantity = totalQty,
                    RequirementCount = g.Count(),
                    RequestingAreaCount = distinctAreas,
                    TotalAmount = totalAmt,
                    TotalApprovedAmount = totalApproved,
                    CeilingInitialAmount = c?.InitialAmount,
                    CeilingCurrentAmount = c?.CurrentAmount,
                    CeilingAvailableAmount = c?.AvailableAmount,
                    CeilingUtilizationPercentage = c != null && c.CurrentAmount > 0 ? Math.Round((c.PlannedAmount / c.CurrentAmount) * 100, 2) : 0,
                    CeilingStatus = c?.Status,
                    Requirements = reqDtos
                };
            })
            .OrderBy(g => g.MasterProductName)
            .ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            groups = groups
                .Where(g => (g.MasterProductCode != null && g.MasterProductCode.ToLower().Contains(q))
                         || g.MasterProductName.ToLower().Contains(q)
                         || (g.UnspscCode != null && g.UnspscCode.ToLower().Contains(q)))
                .ToList();
        }

        var totalAmount = groups.Sum(g => g.TotalAmount);
        var totalApprovedAmount = groups.Sum(g => g.TotalApprovedAmount);
        var totalRequirements = groups.Sum(g => g.RequirementCount);

        var totalCount = groups.Count;
        var validPageSize = pageSize > 0 ? pageSize : 40;
        var validPageNumber = pageNumber > 0 ? pageNumber : 1;
        var totalPages = (int)Math.Ceiling(totalCount / (double)validPageSize);

        var pagedGroups = groups
            .Skip((validPageNumber - 1) * validPageSize)
            .Take(validPageSize)
            .ToList();

        var dto = new ProductsBoardPagedDto
        {
            CycleId = cycle.Id,
            CycleName = cycle.Name,
            CycleYear = cycle.Year,
            TotalAmount = totalAmount,
            TotalApprovedAmount = totalApprovedAmount,
            TotalRequirements = totalRequirements,
            PageNumber = validPageNumber,
            PageSize = validPageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            ProductGroups = pagedGroups
        };

        return HttpResult<ProductsBoardPagedDto>.Success(dto);
    }

    public async Task<IHttpResult<ConsolidatorAreasBoardPagedDto>> GetConsolidatorAreasBoardAsync(
        int organizationId,
        Guid cycleId,
        int pageNumber = 1,
        int pageSize = 40,
        string? search = null)
    {
        var cycle = await _unitOfWork.Context.PlanningCycles
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == cycleId);

        if (cycle is null)
        {
            return HttpResult<ConsolidatorAreasBoardPagedDto>.Failure(
                "Ciclo no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "cycleId", new[] { "El ciclo especificado no existe." } } },
                HttpStatusCode.NotFound);
        }

        var plans = await _unitOfWork.Context.AnnualPlans
            .AsNoTracking()
            .Where(p => p.OrganizationId == organizationId && p.CycleId == cycleId)
            .ToListAsync();

        var plansByStatus = plans
            .GroupBy(p => p.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        var requirements = await _unitOfWork.Context.Requirements
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId && r.CycleId == cycleId && r.Status != "Merged")
            .ToListAsync();

        var groups = requirements
            .GroupBy(r => new { r.ConsolidatorAreaId, r.ConsolidatorAreaName })
            .Select(g =>
            {
                var reqDtos = g.Select(MapRequirementDto).ToList();
                var reqsByStatus = g.GroupBy(r => r.Status).ToDictionary(sg => sg.Key, sg => sg.Count());
                var total = g.Sum(r => r.ApprovedAmount ?? (r.Quantity * r.EstimatedUnitPrice));

                return new ConsolidatorGroupDto
                {
                    ConsolidatorAreaId = g.Key.ConsolidatorAreaId,
                    ConsolidatorAreaName = g.Key.ConsolidatorAreaName,
                    RequirementCount = g.Count(),
                    TotalAmount = total,
                    RequirementsByStatus = reqsByStatus,
                    Requirements = reqDtos
                };
            })
            .OrderBy(g => g.ConsolidatorAreaName)
            .ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            groups = groups
                .Where(g => g.ConsolidatorAreaName.ToLower().Contains(q))
                .ToList();
        }

        var totalApprovedAmount = requirements
            .Where(r => r.Status == "Approved" || r.Status == "Reduced")
            .Sum(r => r.ApprovedAmount ?? (r.Quantity * r.EstimatedUnitPrice));

        var totalCount = groups.Count;
        var validPageSize = pageSize > 0 ? pageSize : 40;
        var validPageNumber = pageNumber > 0 ? pageNumber : 1;
        var totalPages = (int)Math.Ceiling(totalCount / (double)validPageSize);

        var pagedGroups = groups
            .Skip((validPageNumber - 1) * validPageSize)
            .Take(validPageSize)
            .ToList();

        var dto = new ConsolidatorAreasBoardPagedDto
        {
            CycleId = cycle.Id,
            CycleName = cycle.Name,
            CycleYear = cycle.Year,
            PlansByStatus = plansByStatus,
            TotalApprovedAmount = totalApprovedAmount,
            PageNumber = validPageNumber,
            PageSize = validPageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            ConsolidatorGroups = pagedGroups
        };

        return HttpResult<ConsolidatorAreasBoardPagedDto>.Success(dto);
    }
}

