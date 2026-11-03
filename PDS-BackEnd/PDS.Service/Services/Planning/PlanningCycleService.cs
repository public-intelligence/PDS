using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PDS.Application.Data;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Models;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

public class PlanningCycleService : IPlanningCycleService
{
    private readonly IUnitOfWork<PDSDbContext> _unitOfWork;
    private readonly IPlanningActivityService _activityService;
    private readonly IPlanningConfigService _configService;

    public PlanningCycleService(
        IUnitOfWork<PDSDbContext> unitOfWork,
        IPlanningActivityService activityService,
        IPlanningConfigService configService)
    {
        _unitOfWork = unitOfWork;
        _activityService = activityService;
        _configService = configService;
    }

    public async Task<IHttpResult<IEnumerable<PlanningCycleDto>>> ListAsync(int organizationId)
    {
        var cycles = await _unitOfWork.Context.PlanningCycles
            .AsNoTracking()
            .Include(c => c.StrategicPlan)
            .Include(c => c.Template)
            .Include(c => c.Areas)
            .Include(c => c.Plans)
            .Where(c => c.OrganizationId == organizationId)
            .OrderByDescending(c => c.Year)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync();

        var result = cycles.Select(c => MapToDto(c));
        return HttpResult<IEnumerable<PlanningCycleDto>>.Success(result);
    }

    public async Task<IHttpResult<PlanningCycleDetailDto>> GetByIdAsync(int organizationId, Guid id)
    {
        var cycle = await _unitOfWork.Context.PlanningCycles
            .AsNoTracking()
            .Include(c => c.StrategicPlan)
            .Include(c => c.Template)
            .Include(c => c.Milestones)
            .Include(c => c.Areas)
            .Include(c => c.Plans)
            .Include(c => c.BudgetCeilings)
            .Include(c => c.AmendmentLogs)
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == id);

        if (cycle is null)
        {
            return HttpResult<PlanningCycleDetailDto>.Failure(
                "Ciclo no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { $"El ciclo con ID '{id}' no existe." } } },
                HttpStatusCode.NotFound);
        }

        return HttpResult<PlanningCycleDetailDto>.Success(MapToDetailDto(cycle));
    }

    public async Task<IHttpResult<PlanningCycleDetailDto>> CreateDraftAsync(
        int organizationId,
        CreatePlanningCycleDto dto,
        Guid userId,
        string? userName)
    {
        // 1. Validar año único por organización
        var existingYear = await _unitOfWork.Context.PlanningCycles
            .AnyAsync(c => c.OrganizationId == organizationId && c.Year == dto.Year);
        if (existingYear)
        {
            return HttpResult<PlanningCycleDetailDto>.Failure(
                "Año duplicado",
                new Dictionary<string, IEnumerable<string>> { { "year", new[] { $"Ya existe un ciclo de planificación para el año {dto.Year}." } } },
                HttpStatusCode.Conflict);
        }

        // 2. Validar PEI referenciado (debe existir)
        var strategicPlan = await _unitOfWork.Context.StrategicPlans
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == dto.StrategicPlanId);
        if (strategicPlan is null)
        {
            return HttpResult<PlanningCycleDetailDto>.Failure(
                "PEI no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "strategicPlanId", new[] { "El PEI referenciado no existe." } } });
        }

        // 3. Validar Plantilla (debe existir)
        var template = await _unitOfWork.Context.PlanningTemplates
            .FirstOrDefaultAsync(t => t.OrganizationId == organizationId && t.Id == dto.TemplateId);
        if (template is null)
        {
            return HttpResult<PlanningCycleDetailDto>.Failure(
                "Plantilla no encontrada",
                new Dictionary<string, IEnumerable<string>> { { "templateId", new[] { "La plantilla referenciada no existe." } } });
        }

        // 4. Resolver nombres de áreas elegibles
        var areaIds = dto.AreaIds.Distinct().ToList();
        var areaEntities = await _unitOfWork.Context.OrganizationalStructures
            .Where(a => a.OrganizationId == organizationId && areaIds.Contains(a.Id) && a.DeletedAt == null)
            .ToListAsync();

        var cycle = new PlanningCycle
        {
            OrganizationId = organizationId,
            Name = dto.Name.Trim(),
            Year = dto.Year,
            EndYear = dto.EndYear,
            Status = "Draft",
            StrategicPlanId = dto.StrategicPlanId,
            TemplateId = dto.TemplateId,
            CreatedBy = userId,
            CreatedByName = userName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var m in dto.Milestones)
        {
            cycle.Milestones.Add(new PlanningCycleMilestone
            {
                Name = m.Name.Trim(),
                Date = m.Date,
                Kind = m.Kind
            });
        }

        foreach (var areaId in areaIds)
        {
            var area = areaEntities.FirstOrDefault(a => a.Id == areaId);
            cycle.Areas.Add(new PlanningCycleArea
            {
                AreaId = areaId,
                AreaName = area?.Name ?? "Área no encontrada"
            });
        }

        _unitOfWork.Context.PlanningCycles.Add(cycle);
        await _unitOfWork.Context.SaveChangesAsync();

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Ciclos",
            "Creó ciclo en borrador",
            $"Creó el ciclo '{cycle.Name}' ({cycle.Year})",
            "PlanningCycle",
            cycle.Id);

        // Volver a consultar con relaciones para mapear
        var created = await _unitOfWork.Context.PlanningCycles
            .Include(c => c.StrategicPlan)
            .Include(c => c.Template)
            .Include(c => c.Milestones)
            .Include(c => c.Areas)
            .Include(c => c.Plans)
            .FirstAsync(c => c.Id == cycle.Id);

        return HttpResult<PlanningCycleDetailDto>.Success(MapToDetailDto(created), HttpStatusCode.Created);
    }

    public async Task<IHttpResult<PlanningCycleDetailDto>> UpdateDraftAsync(
        int organizationId,
        Guid id,
        UpdatePlanningCycleDto dto,
        Guid userId,
        string? userName)
    {
        var cycle = await _unitOfWork.Context.PlanningCycles
            .Include(c => c.Milestones)
            .Include(c => c.Areas)
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == id);

        if (cycle is null)
        {
            return HttpResult<PlanningCycleDetailDto>.Failure(
                "Ciclo no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { $"El ciclo con ID '{id}' no existe." } } },
                HttpStatusCode.NotFound);
        }

        if (cycle.Status == "Published" && string.IsNullOrWhiteSpace(dto.Justification))
        {
            return HttpResult<PlanningCycleDetailDto>.Failure(
                "Justificación requerida",
                new Dictionary<string, IEnumerable<string>> { { "justification", new[] { "Se requiere una justificación motivada para modificar un ciclo en ejecución/publicado." } } },
                HttpStatusCode.BadRequest);
        }

        var isPublishedUpdate = cycle.Status == "Published";
        var oldValuesSummary = $"Nombre: {cycle.Name}, Año: {cycle.Year}, Áreas: {cycle.Areas.Count}";

        // Check year conflict if year changed
        if (cycle.Year != dto.Year)
        {
            var yearExists = await _unitOfWork.Context.PlanningCycles
                .AnyAsync(c => c.OrganizationId == organizationId && c.Year == dto.Year && c.Id != id);
            if (yearExists)
            {
                return HttpResult<PlanningCycleDetailDto>.Failure(
                    "Año duplicado",
                    new Dictionary<string, IEnumerable<string>> { { "year", new[] { $"Ya existe un ciclo para el año {dto.Year}." } } },
                    HttpStatusCode.Conflict);
            }
        }

        cycle.Name = dto.Name.Trim();
        cycle.Year = dto.Year;
        cycle.EndYear = dto.EndYear;
        cycle.StrategicPlanId = dto.StrategicPlanId;
        cycle.TemplateId = dto.TemplateId;
        cycle.UpdatedAt = DateTime.UtcNow;

        // Reemplazar hitos
        _unitOfWork.Context.PlanningCycleMilestones.RemoveRange(cycle.Milestones);
        cycle.Milestones.Clear();
        foreach (var m in dto.Milestones)
        {
            cycle.Milestones.Add(new PlanningCycleMilestone
            {
                Name = m.Name.Trim(),
                Date = m.Date,
                Kind = m.Kind
            });
        }

        // Reemplazar áreas
        _unitOfWork.Context.PlanningCycleAreas.RemoveRange(cycle.Areas);
        cycle.Areas.Clear();
        var areaIds = dto.AreaIds.Distinct().ToList();
        var areaEntities = await _unitOfWork.Context.OrganizationalStructures
            .Where(a => a.OrganizationId == organizationId && areaIds.Contains(a.Id) && a.DeletedAt == null)
            .ToListAsync();

        foreach (var areaId in areaIds)
        {
            var area = areaEntities.FirstOrDefault(a => a.Id == areaId);
            cycle.Areas.Add(new PlanningCycleArea
            {
                AreaId = areaId,
                AreaName = area?.Name ?? "Área no encontrada"
            });
        }

        if (isPublishedUpdate && !string.IsNullOrWhiteSpace(dto.Justification))
        {
            var newValuesSummary = $"Nombre: {cycle.Name}, Año: {cycle.Year}, Áreas: {cycle.Areas.Count}";
            _unitOfWork.Context.PlanningCycleAmendmentLogs.Add(new PlanningCycleAmendmentLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                CycleId = cycle.Id,
                AmendmentType = "CycleUpdate",
                Justification = dto.Justification.Trim(),
                PreviousValuesJson = oldValuesSummary,
                NewValuesJson = newValuesSummary,
                CreatedBy = userId,
                CreatedByName = userName,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _unitOfWork.Context.SaveChangesAsync();

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Ciclos",
            "Actualizó borrador de ciclo",
            $"Actualizó el ciclo '{cycle.Name}'",
            "PlanningCycle",
            cycle.Id);

        var updated = await _unitOfWork.Context.PlanningCycles
            .Include(c => c.StrategicPlan)
            .Include(c => c.Template)
            .Include(c => c.Milestones)
            .Include(c => c.Areas)
            .Include(c => c.Plans)
            .FirstAsync(c => c.Id == cycle.Id);

        return HttpResult<PlanningCycleDetailDto>.Success(MapToDetailDto(updated));
    }

    public async Task<IHttpResult<PlanningCycleDetailDto>> PublishAsync(
        int organizationId,
        Guid id,
        Guid userId,
        string? userName)
    {
        // Con EnableRetryOnFailure, una transacción manual solo se permite dentro de la estrategia de
        // ejecución, que reintenta el bloque completo (begin/commit incluidos) ante fallos transitorios.
        var strategy = _unitOfWork.Context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<IHttpResult<PlanningCycleDetailDto>>(async () =>
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var cycle = await _unitOfWork.Context.PlanningCycles
                    .Include(c => c.StrategicPlan)
                    .Include(c => c.Template)
                    .Include(c => c.Milestones)
                    .Include(c => c.Areas)
                    .Include(c => c.Plans)
                    .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == id);

                if (cycle is null)
                {
                    await _unitOfWork.RollbackAsync();
                    return HttpResult<PlanningCycleDetailDto>.Failure(
                        "Ciclo no encontrado",
                        new Dictionary<string, IEnumerable<string>> { { "id", new[] { "El ciclo no existe." } } },
                        HttpStatusCode.NotFound);
                }

                if (cycle.Status != "Draft")
                {
                    await _unitOfWork.RollbackAsync();
                    return HttpResult<PlanningCycleDetailDto>.Failure(
                        "Estado inválido",
                        new Dictionary<string, IEnumerable<string>> { { "status", new[] { "Solo se pueden publicar ciclos en estado Borrador." } } },
                        HttpStatusCode.Conflict);
                }

                // Validar PEI Current
                var pei = await _unitOfWork.Context.StrategicPlans
                    .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == cycle.StrategicPlanId);
                if (pei is null || pei.Status != "Current")
                {
                    await _unitOfWork.RollbackAsync();
                    return HttpResult<PlanningCycleDetailDto>.Failure(
                        "PEI no vigente",
                        new Dictionary<string, IEnumerable<string>> { { "strategicPlanId", new[] { "El ciclo debe estar vinculado a una versión del PEI en estado Vigente (Current)." } } },
                        HttpStatusCode.BadRequest);
                }

                // Validar plantilla activa
                var template = await _unitOfWork.Context.PlanningTemplates
                    .FirstOrDefaultAsync(t => t.OrganizationId == organizationId && t.Id == cycle.TemplateId);
                if (template is null || !template.IsActive)
                {
                    await _unitOfWork.RollbackAsync();
                    return HttpResult<PlanningCycleDetailDto>.Failure(
                        "Plantilla inactiva",
                        new Dictionary<string, IEnumerable<string>> { { "templateId", new[] { "La plantilla seleccionada no está activa." } } },
                        HttpStatusCode.BadRequest);
                }

                // Validar >= 1 área
                if (cycle.Areas.Count == 0)
                {
                    await _unitOfWork.RollbackAsync();
                    return HttpResult<PlanningCycleDetailDto>.Failure(
                        "Sin áreas",
                        new Dictionary<string, IEnumerable<string>> { { "areas", new[] { "El ciclo debe incluir al menos un área." } } },
                        HttpStatusCode.BadRequest);
                }

                // Validar hito FormulationDeadline
                var deadlineMilestone = cycle.Milestones
                    .FirstOrDefault(m => string.Equals(m.Kind, "FormulationDeadline", StringComparison.OrdinalIgnoreCase));
                if (deadlineMilestone is null)
                {
                    await _unitOfWork.RollbackAsync();
                    return HttpResult<PlanningCycleDetailDto>.Failure(
                        "Sin fecha límite",
                        new Dictionary<string, IEnumerable<string>> { { "milestones", new[] { "El ciclo debe definir el hito de límite de formulación (FormulationDeadline)." } } },
                        HttpStatusCode.BadRequest);
                }

                // Actualizar ciclo
                cycle.Status = "Published";
                cycle.PublishedAt = DateTime.UtcNow;
                cycle.PublishedBy = userId;
                cycle.PublishedByName = userName;
                cycle.UpdatedAt = DateTime.UtcNow;

                var peiGraph = await _unitOfWork.Context.StrategicPlans
                    .AsNoTracking()
                    .Include(p => p.Focuses)
                        .ThenInclude(f => f.Objectives)
                        .ThenInclude(o => o.Products)
                            .ThenInclude(pr => pr.MeasureUnits)
                                .ThenInclude(mu => mu.AnnualTargets)
                    .Include(p => p.Focuses)
                        .ThenInclude(f => f.Objectives)
                        .ThenInclude(o => o.Products)
                            .ThenInclude(pr => pr.InvolvedAreas)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(p => p.Id == cycle.StrategicPlanId);

                // Crear Planes Anuales (POA) para cada área alcanzada
                foreach (var area in cycle.Areas)
                {
                    // Verificar que no exista ya un plan para este ciclo y área
                    var existingPlan = await _unitOfWork.Context.AnnualPlans
                        .AnyAsync(p => p.CycleId == cycle.Id && p.AreaId == area.AreaId);
                    if (!existingPlan)
                    {
                        var plan = new AnnualPlan
                        {
                            OrganizationId = organizationId,
                            CycleId = cycle.Id,
                            AreaId = area.AreaId,
                            AreaName = area.AreaName,
                            Name = $"POA {cycle.Year} — {area.AreaName}",
                            Status = "Sin iniciar",
                            Version = 1,
                            DueDate = deadlineMilestone.Date,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        if (peiGraph is not null)
                        {
                            var seq = 0;
                            foreach (var focus in peiGraph.Focuses.OrderBy(f => f.Order))
                            {
                                foreach (var objective in focus.Objectives.OrderBy(o => o.Order))
                                {
                                    foreach (var product in objective.Products.OrderBy(p => p.Order))
                                    {
                                        var matchesResp = product.MeasureUnits.Any(u => u.ResponsibleAreaId == area.AreaId || string.Equals(u.ResponsibleAreaName, area.AreaName, StringComparison.OrdinalIgnoreCase));
                                        var matchesInv = product.InvolvedAreas.Any(ia => ia.AreaId == area.AreaId || string.Equals(ia.AreaName, area.AreaName, StringComparison.OrdinalIgnoreCase));

                                        if (!matchesResp && !matchesInv) continue;

                                        var matchingUnits = product.MeasureUnits
                                            .Where(u => u.ResponsibleAreaId == area.AreaId || string.Equals(u.ResponsibleAreaName, area.AreaName, StringComparison.OrdinalIgnoreCase) || matchesInv)
                                            .ToList();

                                        var poaProduct = new PoaProduct
                                        {
                                            Id = Guid.NewGuid(),
                                            PlanId = plan.Id,
                                            Code = $"EJ-{(++seq):D2}",
                                            Origin = "Pei",
                                            StrategicFocusId = focus.Id,
                                            StrategicObjectiveId = objective.Id,
                                            StrategicProductId = product.Id,
                                            ProductName = product.Name,
                                            Order = seq,
                                            CreatedAt = DateTime.UtcNow,
                                            UpdatedAt = DateTime.UtcNow
                                        };

                                        foreach (var unit in matchingUnits)
                                        {
                                            var targetVal = unit.AnnualTargets.FirstOrDefault(t => t.Year == cycle.Year)?.Value ?? 0;
                                            var isPct = unit.Name.Contains("porcentaje", StringComparison.OrdinalIgnoreCase) || unit.Name.Contains("%");

                                            poaProduct.MeasureUnits.Add(new PoaMeasureUnit
                                            {
                                                Id = Guid.NewGuid(),
                                                ProductId = poaProduct.Id,
                                                Name = unit.Name,
                                                TargetValue = targetVal,
                                                TargetType = isPct ? "Percentage" : "Absolute",
                                                ResponsibleAreaId = unit.ResponsibleAreaId ?? area.AreaId,
                                                ResponsibleAreaName = unit.ResponsibleAreaName ?? area.AreaName,
                                                Q1 = 0, Q2 = 0, Q3 = 0, Q4 = 0
                                            });
                                        }

                                        foreach (var invArea in product.InvolvedAreas)
                                        {
                                            poaProduct.InvolvedAreas.Add(new PoaProductInvolvedArea
                                            {
                                                Id = Guid.NewGuid(),
                                                ProductId = poaProduct.Id,
                                                AreaId = invArea.AreaId,
                                                AreaName = invArea.AreaName
                                            });
                                        }

                                        plan.Products.Add(poaProduct);
                                    }
                                }
                            }
                        }

                        _unitOfWork.Context.AnnualPlans.Add(plan);
                    }
                }

                // Pre-cargar e instanciar Techos Presupuestarios del Ciclo a partir de la Maestra (CycleId == null)
                var masterCeilings = await _unitOfWork.Context.BudgetCeilings
                    .AsNoTracking()
                    .Where(c => c.OrganizationId == organizationId && c.FiscalYear == cycle.Year && c.CycleId == null && c.DeletedAt == null)
                    .ToListAsync();

                foreach (var master in masterCeilings)
                {
                    var existingCycleCeiling = await _unitOfWork.Context.BudgetCeilings
                        .AnyAsync(c => c.OrganizationId == organizationId && c.CycleId == cycle.Id && c.ExpenseObjectId == master.ExpenseObjectId && c.FundingSource == master.FundingSource && c.DeletedAt == null);

                    if (!existingCycleCeiling)
                    {
                        var cycleCeiling = new BudgetCeiling
                        {
                            Id = Guid.NewGuid(),
                            OrganizationId = organizationId,
                            FiscalYear = cycle.Year,
                            CycleId = cycle.Id,
                            ExpenseObjectId = master.ExpenseObjectId,
                            ExpenseObjectCode = master.ExpenseObjectCode,
                            ExpenseObjectName = master.ExpenseObjectName,
                            FundingSource = master.FundingSource,
                            BudgetProgram = master.BudgetProgram,
                            InitialAmount = master.InitialAmount,
                            CurrentAmount = master.InitialAmount,
                            ReservedAmount = 0,
                            PlannedAmount = 0,
                            CommittedAmount = 0,
                            ExecutedAmount = 0,
                            AvailableAmount = master.InitialAmount,
                            ThresholdPercentage = master.ThresholdPercentage,
                            ValidationBehavior = master.ValidationBehavior,
                            Status = "Green",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _unitOfWork.Context.BudgetCeilings.Add(cycleCeiling);
                    }
                }

                await _unitOfWork.Context.SaveChangesAsync();

                await _activityService.LogAsync(
                    organizationId,
                    userId,
                    userName,
                    "Ciclos",
                    "Publicó el ciclo",
                    $"Publicó el ciclo '{cycle.Name}' ({cycle.Year}) con {cycle.Areas.Count} planes de área generados",
                    "PlanningCycle",
                    cycle.Id);

                await _unitOfWork.CommitAsync();

                var publishedCycle = await _unitOfWork.Context.PlanningCycles
                    .AsNoTracking()
                    .Include(c => c.StrategicPlan)
                    .Include(c => c.Template)
                    .Include(c => c.Milestones)
                    .Include(c => c.Areas)
                    .Include(c => c.Plans)
                    .FirstAsync(c => c.Id == cycle.Id);

                return HttpResult<PlanningCycleDetailDto>.Success(MapToDetailDto(publishedCycle));
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                // Descartar lo rastreado en este intento: si la estrategia reintenta, las consultas
                // deben volver a leer el estado de la BD (p. ej. el ciclo aún en "Draft").
                _unitOfWork.Context.ChangeTracker.Clear();
                throw;
            }
        });
    }

    public async Task<IHttpResult> CloseAsync(
        int organizationId,
        Guid id,
        Guid userId,
        string? userName)
    {
        var cycle = await _unitOfWork.Context.PlanningCycles
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == id);

        if (cycle is null)
        {
            return HttpResult.Failure(
                "Ciclo no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { "El ciclo no existe." } } },
                HttpStatusCode.NotFound);
        }

        if (cycle.Status != "Published")
        {
            return HttpResult.Failure(
                "Estado inválido",
                new Dictionary<string, IEnumerable<string>> { { "status", new[] { "Solo se pueden cerrar ciclos en estado Publicado." } } },
                HttpStatusCode.Conflict);
        }

        var unapprovedCount = await _unitOfWork.Context.AnnualPlans
            .CountAsync(p => p.OrganizationId == organizationId && p.CycleId == id && p.Status != "Approved");

        if (unapprovedCount > 0)
        {
            return HttpResult.Failure(
                "Planes no aprobados",
                new Dictionary<string, IEnumerable<string>> { { "plans", new[] { $"No se puede cerrar el ciclo porque existen {unapprovedCount} plan(es) aún no aprobados." } } },
                HttpStatusCode.Conflict);
        }

        cycle.Status = "Closed";
        cycle.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Context.SaveChangesAsync();

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Ciclos",
            "Cerró el ciclo",
            $"Cerró el ciclo '{cycle.Name}' ({cycle.Year})",
            "PlanningCycle",
            cycle.Id);

        return HttpResult.Success();
    }

    public async Task<IHttpResult> ReopenAsync(
        int organizationId,
        Guid id,
        Guid userId,
        string? userName)
    {
        var cycle = await _unitOfWork.Context.PlanningCycles
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == id);

        if (cycle is null)
        {
            return HttpResult.Failure(
                "Ciclo no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { "El ciclo no existe." } } },
                HttpStatusCode.NotFound);
        }

        if (cycle.Status != "Closed")
        {
            return HttpResult.Failure(
                "Estado inválido",
                new Dictionary<string, IEnumerable<string>> { { "status", new[] { "Solo se pueden reabrir ciclos cerrados." } } },
                HttpStatusCode.Conflict);
        }

        cycle.Status = "Published";
        cycle.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Context.SaveChangesAsync();

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Ciclos",
            "Reabrió el ciclo",
            $"Reabrió el ciclo '{cycle.Name}' ({cycle.Year})",
            "PlanningCycle",
            cycle.Id);

        return HttpResult.Success();
    }

    public async Task<IHttpResult<IEnumerable<EligibleAreaDto>>> GetEligibleAreasAsync(int organizationId)
    {
        var configResult = await _configService.GetAreaConfigAsync(organizationId);
        if (!configResult.IsSuccess || configResult.Value is null)
        {
            return HttpResult<IEnumerable<EligibleAreaDto>>.Success(Enumerable.Empty<EligibleAreaDto>());
        }

        var eligibleAreas = configResult.Value.QualifyingAreas
            .Where(a => a.QualifiesForPlanning)
            .OrderBy(a => a.Name)
            .Select(a => new EligibleAreaDto
            {
                AreaId = a.Id,
                AreaName = a.Name
            })
            .ToList();

        return HttpResult<IEnumerable<EligibleAreaDto>>.Success(eligibleAreas);
    }

    private static PlanningCycleDto MapToDto(PlanningCycle c)
    {
        var planCounts = c.Plans
            .GroupBy(p => p.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        return new PlanningCycleDto
        {
            Id = c.Id,
            Name = c.Name,
            Year = c.Year,
            EndYear = c.EndYear,
            Status = c.Status,
            StrategicPlanId = c.StrategicPlanId,
            StrategicPlanName = c.StrategicPlan?.Name,
            TemplateId = c.TemplateId,
            TemplateName = c.Template?.Name,
            PublishedAt = c.PublishedAt,
            AreaCount = c.Areas.Count,
            PlanCountsByStatus = planCounts
        };
    }

    private static PlanningCycleDetailDto MapToDetailDto(PlanningCycle c)
    {
        var baseDto = MapToDto(c);

        return new PlanningCycleDetailDto
        {
            Id = baseDto.Id,
            Name = baseDto.Name,
            Year = baseDto.Year,
            EndYear = baseDto.EndYear,
            Status = baseDto.Status,
            StrategicPlanId = baseDto.StrategicPlanId,
            StrategicPlanName = baseDto.StrategicPlanName,
            TemplateId = baseDto.TemplateId,
            TemplateName = baseDto.TemplateName,
            PublishedAt = baseDto.PublishedAt,
            AreaCount = baseDto.AreaCount,
            PlanCountsByStatus = baseDto.PlanCountsByStatus,
            CreatedBy = c.CreatedBy,
            CreatedByName = c.CreatedByName,
            CreatedAt = c.CreatedAt,
            Milestones = c.Milestones.Select(m => new PlanningCycleMilestoneDto
            {
                Id = m.Id,
                Name = m.Name,
                Date = m.Date,
                Kind = m.Kind
            }).ToList(),
            Areas = c.Areas.Select(a => new PlanningCycleAreaDto
            {
                Id = a.Id,
                AreaId = a.AreaId,
                AreaName = a.AreaName
            }).ToList(),
            BudgetCeilings = c.BudgetCeilings.Select(b => new BudgetCeilingDto
            {
                Id = b.Id,
                OrganizationId = b.OrganizationId,
                FiscalYear = b.FiscalYear,
                CycleId = b.CycleId,
                ExpenseObjectId = b.ExpenseObjectId,
                ExpenseObjectCode = b.ExpenseObjectCode,
                ExpenseObjectName = b.ExpenseObjectName,
                FundingSource = b.FundingSource,
                BudgetProgram = b.BudgetProgram,
                InitialAmount = b.InitialAmount,
                CurrentAmount = b.CurrentAmount,
                ReservedAmount = b.ReservedAmount,
                PlannedAmount = b.PlannedAmount,
                CommittedAmount = b.CommittedAmount,
                ExecutedAmount = b.ExecutedAmount,
                AvailableAmount = b.AvailableAmount,
                ThresholdPercentage = b.ThresholdPercentage,
                ValidationBehavior = b.ValidationBehavior,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            }).ToList(),
            Amendments = c.AmendmentLogs.OrderByDescending(a => a.CreatedAt).Select(a => new PlanningCycleAmendmentLogDto
            {
                Id = a.Id,
                AmendmentType = a.AmendmentType,
                Justification = a.Justification,
                PreviousValuesJson = a.PreviousValuesJson,
                NewValuesJson = a.NewValuesJson,
                CreatedBy = a.CreatedBy,
                CreatedByName = a.CreatedByName,
                CreatedAt = a.CreatedAt
            }).ToList()
        };
    }
}
