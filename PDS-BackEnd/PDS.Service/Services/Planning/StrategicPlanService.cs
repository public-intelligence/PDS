using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDS.Application.Data;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Models;
using PDS.Domain.Response;
using System.Net;

namespace PDS.Service.Services.Planning;

public sealed class StrategicPlanService : IStrategicPlanService
{
    private const string ModuleName = "PEI";

    private readonly IUnitOfWork<PDSDbContext> _uow;
    private readonly IRepository<StrategicPlan> _planRepository;
    private readonly IRepository<OrganizationalStructure> _structureRepository;
    private readonly IPlanningActivityService _activityService;
    private readonly ILogger<StrategicPlanService> _logger;

    public StrategicPlanService(
        IUnitOfWork<PDSDbContext> uow,
        IRepository<StrategicPlan> planRepository,
        IRepository<OrganizationalStructure> structureRepository,
        IPlanningActivityService activityService,
        ILogger<StrategicPlanService> logger)
    {
        _uow = uow;
        _planRepository = planRepository;
        _structureRepository = structureRepository;
        _activityService = activityService;
        _logger = logger;
    }

    /// <summary>Año de referencia: lo anterior o igual a él es historia inmutable.</summary>
    private static int CurrentYear => DateTime.Now.Year;

    // ───────────────────────── Lectura ─────────────────────────

    public async Task<IHttpResult<IEnumerable<StrategicPlanDto>>> ListAsync(int organizationId)
    {
        var result = await _planRepository.FilterAsync(
            p => p.OrganizationId == organizationId,
            q => q.Include(p => p.Focuses).OrderByDescending(p => p.VersionNumber));
        if (!result.IsSuccess)
            return Fail<IEnumerable<StrategicPlanDto>>("No se pudieron listar las versiones del PEI");

        var dtos = result.Value.Select(ToSummaryDto).ToList();
        return HttpResult<IEnumerable<StrategicPlanDto>>.Success(dtos);
    }

    public async Task<IHttpResult<StrategicPlanDetailDto>> GetByIdAsync(int organizationId, Guid id)
    {
        var plan = await LoadGraphAsync(organizationId, id);
        if (plan is null)
            return Fail<StrategicPlanDetailDto>("La versión del PEI no existe", HttpStatusCode.NotFound);

        return HttpResult<StrategicPlanDetailDto>.Success(ToDetailDto(plan));
    }

    // ───────────────────────── Versionado ─────────────────────────

    public async Task<IHttpResult<StrategicPlanDetailDto>> CreateInitialAsync(
        int organizationId, CreateStrategicPlanDto dto, Guid userId, string? userName)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Fail<StrategicPlanDetailDto>("El nombre del PEI es obligatorio");
        if (dto.PeriodEndYear < dto.PeriodStartYear)
            return Fail<StrategicPlanDetailDto>("El período del PEI es inválido");

        var draftExists = await _planRepository.AnyAsync(p => p.OrganizationId == organizationId && p.Status == "Draft");
        if (draftExists.IsSuccess && draftExists.Value)
            return Fail<StrategicPlanDetailDto>(
                "Ya existe un PEI o enmienda en elaboración (Borrador). Debe aprobarlo o descartarlo antes de crear un nuevo PEI.", HttpStatusCode.Conflict);

        var plan = new StrategicPlan
        {
            OrganizationId = organizationId,
            Name = dto.Name.Trim(),
            PeriodStartYear = dto.PeriodStartYear,
            PeriodEndYear = dto.PeriodEndYear,
            VersionNumber = 1,
            Status = "Draft",
            CreatedBy = userId,
            CreatedByName = userName
        };

        var createResult = await _planRepository.CreateAsync(plan);
        if (!createResult.IsSuccess)
            return Fail<StrategicPlanDetailDto>("No se pudo crear el PEI");

        await _activityService.LogAsync(organizationId, userId, userName, ModuleName,
            "Creó el PEI", $"PEI «{plan.Name}» ({plan.PeriodStartYear}-{plan.PeriodEndYear}) creado en elaboración.",
            nameof(StrategicPlan), plan.Id);

        return await GetByIdAsync(organizationId, createResult.Value.Id);
    }

    public async Task<IHttpResult<StrategicPlanDetailDto>> StartAmendmentAsync(
        int organizationId, CreateAmendmentDto dto, Guid userId, string? userName)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Trim().Length < 15)
            return Fail<StrategicPlanDetailDto>(
                "La justificación de la enmienda es obligatoria (mínimo 15 caracteres)");

        var draftExists = await _planRepository.AnyAsync(
            p => p.OrganizationId == organizationId && p.Status == "Draft");
        if (draftExists.IsSuccess && draftExists.Value)
            return Fail<StrategicPlanDetailDto>("Ya hay una enmienda en elaboración", HttpStatusCode.Conflict);

        var current = await LoadGraphAsync(organizationId, status: "Current");
        if (current is null)
            return Fail<StrategicPlanDetailDto>("No existe una versión vigente del PEI que enmendar", HttpStatusCode.Conflict);

        // Clonado del grafo completo conservando LineageId (identidad estable entre versiones).
        var draft = new StrategicPlan
        {
            OrganizationId = organizationId,
            Name = current.Name,
            PeriodStartYear = current.PeriodStartYear,
            PeriodEndYear = current.PeriodEndYear,
            VersionNumber = current.VersionNumber + 1,
            Status = "Draft",
            Reason = dto.Reason.Trim(),
            CreatedBy = userId,
            CreatedByName = userName,
            PreviousVersionId = current.Id
        };

        foreach (var focus in current.Focuses.OrderBy(f => f.Order))
        {
            var focusClone = new StrategicFocus
            {
                LineageId = focus.LineageId,
                Name = focus.Name,
                EndAlignment = focus.EndAlignment,
                Order = focus.Order
            };

            foreach (var objective in focus.Objectives.OrderBy(o => o.Order))
            {
                var objectiveClone = new StrategicObjective
                {
                    LineageId = objective.LineageId,
                    Code = objective.Code,
                    Name = objective.Name,
                    Order = objective.Order
                };

                foreach (var product in objective.Products.OrderBy(p => p.Order))
                {
                    var productClone = new StrategicProduct
                    {
                        LineageId = product.LineageId,
                        Name = product.Name,
                        DiscontinuedFromYear = product.DiscontinuedFromYear,
                        DiscontinuationReason = product.DiscontinuationReason,
                        Order = product.Order
                    };

                    foreach (var area in product.InvolvedAreas)
                    {
                        productClone.InvolvedAreas.Add(new StrategicProductInvolvedArea
                        {
                            AreaId = area.AreaId,
                            AreaName = area.AreaName
                        });
                    }

                    foreach (var unit in product.MeasureUnits.OrderBy(u => u.Order))
                    {
                        var unitClone = new StrategicMeasureUnit
                        {
                            LineageId = unit.LineageId,
                            Name = unit.Name,
                            ResponsibleAreaId = unit.ResponsibleAreaId,
                            ResponsibleAreaName = unit.ResponsibleAreaName,
                            DiscontinuedFromYear = unit.DiscontinuedFromYear,
                            DiscontinuationReason = unit.DiscontinuationReason,
                            Order = unit.Order
                        };

                        foreach (var target in unit.AnnualTargets)
                        {
                            unitClone.AnnualTargets.Add(new StrategicAnnualTarget
                            {
                                Year = target.Year,
                                Value = target.Value
                            });
                        }

                        productClone.MeasureUnits.Add(unitClone);
                    }

                    objectiveClone.Products.Add(productClone);
                }

                focusClone.Objectives.Add(objectiveClone);
            }

            draft.Focuses.Add(focusClone);
        }

        // Un único SaveChanges ya es atómico (EF lo ejecuta en su propia transacción) y la estrategia de
        // reintentos lo reintenta completo; una transacción manual fallaría con EnableRetryOnFailure.
        try
        {
            _uow.Context.StrategicPlans.Add(draft);
            await _uow.Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al clonar el PEI para la enmienda (org {OrgId}).", organizationId);
            return Fail<StrategicPlanDetailDto>("No se pudo abrir la enmienda del PEI");
        }

        await _activityService.LogAsync(organizationId, userId, userName, ModuleName,
            "Abrió una enmienda del PEI",
            $"Enmienda v{draft.VersionNumber}.0 abierta. Justificación: {draft.Reason}",
            nameof(StrategicPlan), draft.Id);

        return await GetByIdAsync(organizationId, draft.Id);
    }

    public async Task<IHttpResult> DiscardDraftAsync(int organizationId, Guid userId, string? userName)
    {
        var draftResult = await _planRepository.GetAsync(
            p => p.OrganizationId == organizationId && p.Status == "Draft");
        if (!draftResult.IsSuccess || draftResult.Value is null)
            return HttpResult.Failure("No hay enmienda en elaboración",
                Errors("No hay una enmienda en elaboración que descartar"), HttpStatusCode.NotFound);

        var draft = draftResult.Value;

        // Única ruta de borrado del grafo: la cascada de BD elimina focos/objetivos/productos/
        // unidades/metas/cambios del Draft.
        var deleteResult = await _planRepository.DeleteAsync(draft);
        if (!deleteResult.IsSuccess)
            return HttpResult.Failure("No se pudo descartar la enmienda",
                Errors("No se pudo descartar la enmienda"), HttpStatusCode.BadRequest);

        await _activityService.LogAsync(organizationId, userId, userName, ModuleName,
            "Descartó la enmienda del PEI", $"Se descartó la enmienda v{draft.VersionNumber}.0.",
            nameof(StrategicPlan), draft.Id);

        return HttpResult.Success();
    }

    public async Task<IHttpResult<StrategicPlanDto>> ApproveDraftAsync(int organizationId, Guid userId, string? userName)
    {
        // Con EnableRetryOnFailure, una transacción manual solo se permite dentro de la estrategia de
        // ejecución, que reintenta el bloque completo (begin/commit incluidos) ante fallos transitorios.
        var strategy = _uow.Context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<IHttpResult<StrategicPlanDto>>(async () =>
        {
            var beginResult = await _uow.BeginTransactionAsync();
            if (!beginResult.IsSuccess)
                return Fail<StrategicPlanDto>("No se pudo iniciar la transacción de aprobación");

            try
            {
                var ctx = _uow.Context;

                var draft = await ctx.StrategicPlans
                    .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Status == "Draft");
                if (draft is null)
                {
                    await _uow.RollbackAsync();
                    return Fail<StrategicPlanDto>("No hay una enmienda en elaboración que aprobar", HttpStatusCode.NotFound);
                }

                var current = await ctx.StrategicPlans
                    .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Status == "Current");

                // El índice único UX_StrategicPlans_OneCurrentPerOrg exige congelar la vigente ANTES
                // de promover el Draft.
                if (current is not null)
                {
                    current.Status = "Historical";
                    await ctx.SaveChangesAsync();
                }

                draft.Status = "Current";
                draft.ApprovedBy = userId;
                draft.ApprovedByName = userName;
                draft.ApprovedAt = DateTime.UtcNow;
                await ctx.SaveChangesAsync();

                await _uow.CommitAsync();

                await _activityService.LogAsync(organizationId, userId, userName, ModuleName,
                    "Aprobó la enmienda del PEI",
                    $"v{draft.VersionNumber}.0 aprobada; la versión anterior quedó histórica.",
                    nameof(StrategicPlan), draft.Id);

                return HttpResult<StrategicPlanDto>.Success(ToSummaryDto(draft));
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                _logger.LogError(ex, "Error al aprobar la enmienda del PEI (org {OrgId}).", organizationId);
                return Fail<StrategicPlanDto>("No se pudo aprobar la enmienda del PEI");
            }
        });
    }

    public async Task<IHttpResult<StrategicPlanDto>> UpdatePlanAsync(
        int organizationId, Guid planId, UpdateStrategicPlanDto dto, Guid userId, string? userName)
    {
        var draftResult = await GetDraftAsync(organizationId, planId);
        if (!draftResult.IsSuccess)
            return Fail<StrategicPlanDto>(draftResult.Error.Title, draftResult.StatusCode);
        var draft = draftResult.Value;

        if (!string.IsNullOrWhiteSpace(dto.Name))
            draft.Name = dto.Name.Trim();
        if (dto.PeriodStartYear.HasValue)
            draft.PeriodStartYear = dto.PeriodStartYear.Value;
        if (dto.PeriodEndYear.HasValue)
            draft.PeriodEndYear = dto.PeriodEndYear.Value;
        if (draft.PeriodEndYear < draft.PeriodStartYear)
            return Fail<StrategicPlanDto>("El período del PEI es inválido");

        var updateResult = await _planRepository.UpdateAsync(draft);
        if (!updateResult.IsSuccess)
            return Fail<StrategicPlanDto>("No se pudo actualizar el PEI");

        await _activityService.LogAsync(organizationId, userId, userName, ModuleName,
            "Editó la cabecera del PEI", $"PEI «{draft.Name}» actualizado.", nameof(StrategicPlan), draft.Id);

        return HttpResult<StrategicPlanDto>.Success(ToSummaryDto(draft));
    }

    // ───────────────────────── Focos ─────────────────────────

    public async Task<IHttpResult<StrategicFocusDto>> AddFocusAsync(
        int organizationId, Guid planId, CreateStrategicFocusDto dto, Guid userId, string? userName)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Fail<StrategicFocusDto>("El nombre del foco es obligatorio");

        var planResult = await GetDraftGraphAsync(organizationId, planId);
        if (!planResult.IsSuccess)
            return Fail<StrategicFocusDto>(planResult.Error.Title, planResult.StatusCode);
        var plan = planResult.Value;

        var focus = new StrategicFocus
        {
            PlanId = plan.Id,
            LineageId = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            EndAlignment = string.IsNullOrWhiteSpace(dto.EndAlignment)
                ? "Alineación END 2030 por definir"
                : dto.EndAlignment.Trim(),
            Order = plan.Focuses.Count == 0 ? 1 : plan.Focuses.Max(f => f.Order) + 1
        };

        try
        {
            _uow.Context.StrategicFocuses.Add(focus);
            await RegisterChangeAsync(plan.Id, userId, userName, "Added", "Focus", $"Foco «{focus.Name}» agregado.");
            await _uow.Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al agregar el foco (plan {PlanId}).", planId);
            return Fail<StrategicFocusDto>("No se pudo agregar el foco");
        }

        await LogActivityAsync(organizationId, userId, userName, "Nuevo foco estratégico",
            $"Foco «{focus.Name}» agregado.", plan.Id);

        return HttpResult<StrategicFocusDto>.Success(ToFocusDto(focus), HttpStatusCode.Created);
    }

    public async Task<IHttpResult<StrategicFocusDto>> UpdateFocusAsync(
        int organizationId, Guid planId, Guid focusId, UpdateStrategicFocusDto dto, Guid userId, string? userName)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Fail<StrategicFocusDto>("El nombre del foco es obligatorio");

        var planResult = await GetDraftGraphAsync(organizationId, planId);
        if (!planResult.IsSuccess)
            return Fail<StrategicFocusDto>(planResult.Error.Title, planResult.StatusCode);

        var focus = planResult.Value.Focuses.FirstOrDefault(f => f.Id == focusId);
        if (focus is null)
            return Fail<StrategicFocusDto>("El foco no existe", HttpStatusCode.NotFound);

        focus.Name = dto.Name.Trim();
        if (dto.EndAlignment is not null)
            focus.EndAlignment = dto.EndAlignment.Trim();

        try
        {
            await RegisterChangeAsync(planId, userId, userName, "Edited", "Focus", $"Foco renombrado a «{focus.Name}».");
            await _uow.Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar el foco {FocusId}.", focusId);
            return Fail<StrategicFocusDto>("No se pudo editar el foco");
        }

        await LogActivityAsync(organizationId, userId, userName, "Edición de foco",
            $"Foco renombrado a «{focus.Name}».", planId);

        return HttpResult<StrategicFocusDto>.Success(ToFocusDto(focus));
    }

    public async Task<IHttpResult> DeleteFocusAsync(
        int organizationId, Guid planId, Guid focusId, Guid userId, string? userName)
    {
        var planResult = await GetDraftGraphAsync(organizationId, planId);
        if (!planResult.IsSuccess)
            return HttpResult.Failure(planResult.Error.Title, planResult.Error.Detail, planResult.StatusCode);

        var focus = planResult.Value.Focuses.FirstOrDefault(f => f.Id == focusId);
        if (focus is null)
            return HttpResult.Failure("El foco no existe", Errors("El foco no existe"), HttpStatusCode.NotFound);

        if (focus.Objectives.Any(o => o.Products.Any(HasHistory)))
            return HttpResult.Failure("El foco tiene historia",
                Errors("El foco tiene metas comprometidas en años transcurridos; descontinúe sus productos en lugar de eliminarlo"),
                HttpStatusCode.Conflict);

        try
        {
            _uow.Context.StrategicFocuses.Remove(focus);
            await RegisterChangeAsync(planId, userId, userName, "Edited", "Focus",
                $"Foco «{focus.Name}» eliminado (sin metas ejecutadas).");
            await _uow.Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar el foco {FocusId}.", focusId);
            return HttpResult.Failure("No se pudo eliminar el foco", Errors("No se pudo eliminar el foco"), HttpStatusCode.BadRequest);
        }

        await LogActivityAsync(organizationId, userId, userName, "Eliminación de foco",
            $"Foco «{focus.Name}» eliminado.", planId);

        return HttpResult.Success();
    }

    // ───────────────────────── Objetivos ─────────────────────────

    public async Task<IHttpResult<StrategicObjectiveDto>> AddObjectiveAsync(
        int organizationId, Guid planId, Guid focusId, CreateStrategicObjectiveDto dto, Guid userId, string? userName)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Fail<StrategicObjectiveDto>("El nombre del objetivo es obligatorio");

        var planResult = await GetDraftGraphAsync(organizationId, planId);
        if (!planResult.IsSuccess)
            return Fail<StrategicObjectiveDto>(planResult.Error.Title, planResult.StatusCode);
        var plan = planResult.Value;

        var focus = plan.Focuses.FirstOrDefault(f => f.Id == focusId);
        if (focus is null)
            return Fail<StrategicObjectiveDto>("El foco no existe", HttpStatusCode.NotFound);

        // Código "OE {posiciónFoco}.{n}" según la posición al momento del alta (igual que el mock:
        // no se renumera tras eliminaciones).
        var focusPosition = plan.Focuses.OrderBy(f => f.Order).ToList().FindIndex(f => f.Id == focusId) + 1;
        var objective = new StrategicObjective
        {
            FocusId = focus.Id,
            LineageId = Guid.NewGuid(),
            Code = $"OE {focusPosition}.{focus.Objectives.Count + 1}",
            Name = dto.Name.Trim(),
            Order = focus.Objectives.Count == 0 ? 1 : focus.Objectives.Max(o => o.Order) + 1
        };

        try
        {
            _uow.Context.StrategicObjectives.Add(objective);
            await RegisterChangeAsync(planId, userId, userName, "Added", "Objective", $"Objetivo «{objective.Name}» agregado.");
            await _uow.Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al agregar el objetivo (foco {FocusId}).", focusId);
            return Fail<StrategicObjectiveDto>("No se pudo agregar el objetivo");
        }

        await LogActivityAsync(organizationId, userId, userName, "Nuevo objetivo estratégico",
            $"Objetivo «{objective.Name}» agregado.", planId);

        return HttpResult<StrategicObjectiveDto>.Success(ToObjectiveDto(objective), HttpStatusCode.Created);
    }

    public async Task<IHttpResult<StrategicObjectiveDto>> UpdateObjectiveAsync(
        int organizationId, Guid planId, Guid objectiveId, UpdateStrategicObjectiveDto dto, Guid userId, string? userName)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Fail<StrategicObjectiveDto>("El nombre del objetivo es obligatorio");

        var planResult = await GetDraftGraphAsync(organizationId, planId);
        if (!planResult.IsSuccess)
            return Fail<StrategicObjectiveDto>(planResult.Error.Title, planResult.StatusCode);

        var objective = FindObjective(planResult.Value, objectiveId);
        if (objective is null)
            return Fail<StrategicObjectiveDto>("El objetivo no existe", HttpStatusCode.NotFound);

        objective.Name = dto.Name.Trim();

        try
        {
            await RegisterChangeAsync(planId, userId, userName, "Edited", "Objective",
                $"Objetivo renombrado a «{objective.Name}».");
            await _uow.Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar el objetivo {ObjectiveId}.", objectiveId);
            return Fail<StrategicObjectiveDto>("No se pudo editar el objetivo");
        }

        await LogActivityAsync(organizationId, userId, userName, "Edición de objetivo",
            $"Objetivo renombrado a «{objective.Name}».", planId);

        return HttpResult<StrategicObjectiveDto>.Success(ToObjectiveDto(objective));
    }

    public async Task<IHttpResult> DeleteObjectiveAsync(
        int organizationId, Guid planId, Guid objectiveId, Guid userId, string? userName)
    {
        var planResult = await GetDraftGraphAsync(organizationId, planId);
        if (!planResult.IsSuccess)
            return HttpResult.Failure(planResult.Error.Title, planResult.Error.Detail, planResult.StatusCode);

        var objective = FindObjective(planResult.Value, objectiveId);
        if (objective is null)
            return HttpResult.Failure("El objetivo no existe", Errors("El objetivo no existe"), HttpStatusCode.NotFound);

        if (objective.Products.Any(HasHistory))
            return HttpResult.Failure("El objetivo tiene historia",
                Errors("El objetivo tiene metas comprometidas en años transcurridos; descontinúe sus productos en lugar de eliminarlo"),
                HttpStatusCode.Conflict);

        try
        {
            _uow.Context.StrategicObjectives.Remove(objective);
            await RegisterChangeAsync(planId, userId, userName, "Edited", "Objective",
                $"Objetivo «{objective.Name}» eliminado (sin metas ejecutadas).");
            await _uow.Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar el objetivo {ObjectiveId}.", objectiveId);
            return HttpResult.Failure("No se pudo eliminar el objetivo",
                Errors("No se pudo eliminar el objetivo"), HttpStatusCode.BadRequest);
        }

        await LogActivityAsync(organizationId, userId, userName, "Eliminación de objetivo",
            $"Objetivo «{objective.Name}» eliminado.", planId);

        return HttpResult.Success();
    }

    // ───────────────────────── Productos ─────────────────────────

    public async Task<IHttpResult<StrategicProductDto>> AddProductAsync(
        int organizationId, Guid planId, Guid objectiveId, CreateStrategicProductDto dto, Guid userId, string? userName)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Fail<StrategicProductDto>("El nombre del producto es obligatorio");

        var planResult = await GetDraftGraphAsync(organizationId, planId);
        if (!planResult.IsSuccess)
            return Fail<StrategicProductDto>(planResult.Error.Title, planResult.StatusCode);

        var objective = FindObjective(planResult.Value, objectiveId);
        if (objective is null)
            return Fail<StrategicProductDto>("El objetivo no existe", HttpStatusCode.NotFound);

        var product = new StrategicProduct
        {
            ObjectiveId = objective.Id,
            LineageId = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Order = objective.Products.Count == 0 ? 1 : objective.Products.Max(p => p.Order) + 1
        };

        try
        {
            _uow.Context.StrategicProducts.Add(product);
            await RegisterChangeAsync(planId, userId, userName, "Added", "Product", $"Producto «{product.Name}» agregado.");
            await _uow.Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al agregar el producto (objetivo {ObjectiveId}).", objectiveId);
            return Fail<StrategicProductDto>("No se pudo agregar el producto");
        }

        await LogActivityAsync(organizationId, userId, userName, "Nuevo producto estratégico",
            $"Producto «{product.Name}» agregado.", planId);

        return HttpResult<StrategicProductDto>.Success(ToProductDto(product), HttpStatusCode.Created);
    }

    public async Task<IHttpResult<StrategicProductDto>> UpdateProductAsync(
        int organizationId, Guid planId, Guid productId, UpdateStrategicProductDto dto, Guid userId, string? userName)
    {
        var planResult = await GetDraftGraphAsync(organizationId, planId);
        if (!planResult.IsSuccess)
            return Fail<StrategicProductDto>(planResult.Error.Title, planResult.StatusCode);

        var product = FindProduct(planResult.Value, productId);
        if (product is null)
            return Fail<StrategicProductDto>("El producto no existe", HttpStatusCode.NotFound);

        if (!string.IsNullOrWhiteSpace(dto.Name))
            product.Name = dto.Name.Trim();

        if (dto.InvolvedAreaIds is not null)
        {
            // Reemplazo total de la lista, con nombre snapshot resuelto contra el organigrama.
            var areasResult = await _structureRepository.FilterAsync(
                s => s.OrganizationId == organizationId && dto.InvolvedAreaIds.Contains(s.Id));
            if (!areasResult.IsSuccess)
                return Fail<StrategicProductDto>("No se pudieron resolver las áreas involucradas");

            var areas = areasResult.Value.ToDictionary(a => a.Id, a => a.Name);
            if (dto.InvolvedAreaIds.Any(id => !areas.ContainsKey(id)))
                return Fail<StrategicProductDto>("Una de las áreas involucradas no existe en el organigrama");

            product.InvolvedAreas.Clear();
            foreach (var areaId in dto.InvolvedAreaIds.Distinct())
            {
                product.InvolvedAreas.Add(new StrategicProductInvolvedArea
                {
                    ProductId = product.Id,
                    AreaId = areaId,
                    AreaName = areas[areaId]
                });
            }
        }

        try
        {
            await RegisterChangeAsync(planId, userId, userName, "Edited", "Product",
                $"Producto «{product.Name}» actualizado.");
            await _uow.Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar el producto {ProductId}.", productId);
            return Fail<StrategicProductDto>("No se pudo editar el producto");
        }

        await LogActivityAsync(organizationId, userId, userName, "Edición de producto",
            $"Producto «{product.Name}» actualizado.", planId);

        return HttpResult<StrategicProductDto>.Success(ToProductDto(product));
    }

    public async Task<IHttpResult> DeleteProductAsync(
        int organizationId, Guid planId, Guid productId, Guid userId, string? userName)
    {
        var planResult = await GetDraftGraphAsync(organizationId, planId);
        if (!planResult.IsSuccess)
            return HttpResult.Failure(planResult.Error.Title, planResult.Error.Detail, planResult.StatusCode);

        var product = FindProduct(planResult.Value, productId);
        if (product is null)
            return HttpResult.Failure("El producto no existe", Errors("El producto no existe"), HttpStatusCode.NotFound);

        if (HasHistory(product))
            return HttpResult.Failure("El producto tiene historia",
                Errors("El producto tiene metas comprometidas en años transcurridos; descontinúelo en lugar de eliminarlo"),
                HttpStatusCode.Conflict);

        try
        {
            _uow.Context.StrategicProducts.Remove(product);
            await RegisterChangeAsync(planId, userId, userName, "Edited", "Product",
                $"Producto «{product.Name}» eliminado (sin metas ejecutadas).");
            await _uow.Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar el producto {ProductId}.", productId);
            return HttpResult.Failure("No se pudo eliminar el producto",
                Errors("No se pudo eliminar el producto"), HttpStatusCode.BadRequest);
        }

        await LogActivityAsync(organizationId, userId, userName, "Eliminación de producto",
            $"Producto «{product.Name}» eliminado.", planId);

        return HttpResult.Success();
    }

    public async Task<IHttpResult<StrategicProductDto>> DiscontinueProductAsync(
        int organizationId, Guid planId, Guid productId, DiscontinueDto dto, Guid userId, string? userName)
    {
        var validation = ValidateDiscontinue(dto);
        if (validation is not null)
            return Fail<StrategicProductDto>(validation);

        var planResult = await GetDraftGraphAsync(organizationId, planId);
        if (!planResult.IsSuccess)
            return Fail<StrategicProductDto>(planResult.Error.Title, planResult.StatusCode);

        var product = FindProduct(planResult.Value, productId);
        if (product is null)
            return Fail<StrategicProductDto>("El producto no existe", HttpStatusCode.NotFound);

        product.DiscontinuedFromYear = dto.FromYear;
        product.DiscontinuationReason = dto.Reason.Trim();

        try
        {
            await RegisterChangeAsync(planId, userId, userName, "Discontinued", "Product",
                $"«{product.Name}» se descontinúa desde {dto.FromYear}. Motivo: {dto.Reason.Trim()}");
            await _uow.Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al descontinuar el producto {ProductId}.", productId);
            return Fail<StrategicProductDto>("No se pudo descontinuar el producto");
        }

        await LogActivityAsync(organizationId, userId, userName, "Descontinuación de producto",
            $"«{product.Name}» descontinuado desde {dto.FromYear}.", planId);

        return HttpResult<StrategicProductDto>.Success(ToProductDto(product));
    }

    // ───────────────────────── Unidades de medida ─────────────────────────

    public async Task<IHttpResult<StrategicProductDto>> ReplaceMeasureUnitsAsync(
        int organizationId, Guid planId, Guid productId, ReplaceMeasureUnitsDto dto, Guid userId, string? userName)
    {
        var planResult = await GetDraftGraphAsync(organizationId, planId);
        if (!planResult.IsSuccess)
            return Fail<StrategicProductDto>(planResult.Error.Title, planResult.StatusCode);
        var plan = planResult.Value;
        var isDraft = plan.Status is "FormulatingDraft" or "Draft";

        var product = FindProduct(plan, productId);
        if (product is null)
            return Fail<StrategicProductDto>("El producto no existe", HttpStatusCode.NotFound);

        if (dto.Units.Any(u => string.IsNullOrWhiteSpace(u.Name)))
            return Fail<StrategicProductDto>("Toda unidad de medida debe tener nombre");

        var duplicateNames = dto.Units.GroupBy(u => u.Name.Trim(), StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (duplicateNames is not null)
            return Fail<StrategicProductDto>($"No se permiten unidades de medida duplicadas con el nombre «{duplicateNames.Key}» en el mismo producto");

        var validYears = Enumerable.Range(plan.PeriodStartYear, plan.PeriodEndYear - plan.PeriodStartYear + 1).ToHashSet();
        if (dto.Units.SelectMany(u => u.Targets).Any(t => !validYears.Contains(t.Year)))
            return Fail<StrategicProductDto>(
                $"Las metas deben corresponder a años del período {plan.PeriodStartYear}-{plan.PeriodEndYear}");

        // Nombres snapshot de las áreas responsables.
        var areaIds = dto.Units.Where(u => u.ResponsibleAreaId.HasValue)
            .Select(u => u.ResponsibleAreaId!.Value).Distinct().ToList();
        var areaNames = new Dictionary<Guid, string>();
        if (areaIds.Count > 0)
        {
            var areasResult = await _structureRepository.FilterAsync(
                s => s.OrganizationId == organizationId && areaIds.Contains(s.Id));
            if (!areasResult.IsSuccess)
                return Fail<StrategicProductDto>("No se pudieron resolver las áreas responsables");
            areaNames = areasResult.Value.ToDictionary(a => a.Id, a => a.Name);
            if (areaIds.Any(id => !areaNames.ContainsKey(id)))
                return Fail<StrategicProductDto>("Un área responsable no existe en el organigrama");
        }

        try
        {
            var ctx = _uow.Context;
            var keptIds = new HashSet<Guid>();
            var order = 0;

            foreach (var upsert in dto.Units)
            {
                order++;
                var existing = upsert.Id.HasValue
                    ? product.MeasureUnits.FirstOrDefault(u => u.Id == upsert.Id.Value)
                    : null;

                if (existing is null)
                {
                    var unit = new StrategicMeasureUnit
                    {
                        ProductId = product.Id,
                        LineageId = Guid.NewGuid(),
                        Name = upsert.Name.Trim(),
                        ResponsibleAreaId = upsert.ResponsibleAreaId,
                        ResponsibleAreaName = upsert.ResponsibleAreaId.HasValue ? areaNames[upsert.ResponsibleAreaId.Value] : null,
                        Order = order
                    };
                    var targetYears = isDraft ? upsert.Targets : upsert.Targets.Where(t => t.Year >= CurrentYear);
                    foreach (var target in targetYears)
                        unit.AnnualTargets.Add(new StrategicAnnualTarget { Year = target.Year, Value = target.Value });

                    ctx.StrategicMeasureUnits.Add(unit);
                    continue;
                }

                keptIds.Add(existing.Id);
                existing.Name = upsert.Name.Trim();
                existing.ResponsibleAreaId = upsert.ResponsibleAreaId;
                existing.ResponsibleAreaName = upsert.ResponsibleAreaId.HasValue ? areaNames[upsert.ResponsibleAreaId.Value] : null;
                existing.Order = order;

                // Metas: las de años transcurridos se PRESERVAN desde lo guardado si el PEI está activo.
                var allowedTargets = isDraft ? upsert.Targets : upsert.Targets.Where(t => t.Year >= CurrentYear);
                foreach (var target in allowedTargets)
                {
                    var stored = existing.AnnualTargets.FirstOrDefault(t => t.Year == target.Year);
                    if (stored is null)
                        existing.AnnualTargets.Add(new StrategicAnnualTarget { MeasureUnitId = existing.Id, Year = target.Year, Value = target.Value });
                    else
                        stored.Value = target.Value;
                }

                // Metas futuras que el cliente ya no envía se eliminan; las pasadas quedan.
                var sentYears = upsert.Targets.Select(t => t.Year).ToHashSet();
                foreach (var stale in existing.AnnualTargets.Where(t => t.Year > CurrentYear && !sentYears.Contains(t.Year)).ToList())
                    existing.AnnualTargets.Remove(stale);
            }

            // Unidades que el cliente quitó: sin historia se eliminan; con historia se
            // REINCORPORAN (quedan intactas — para retirarlas está la descontinuación).
            foreach (var unit in product.MeasureUnits.Where(u => u.Id != Guid.Empty && !keptIds.Contains(u.Id)).ToList())
            {
                if (!HasHistory(unit))
                    ctx.StrategicMeasureUnits.Remove(unit);
            }

            await RegisterChangeAsync(planId, userId, userName, "Edited", "MeasureUnit",
                $"Unidades de «{product.Name}» actualizadas (metas de años transcurridos preservadas).");
            await ctx.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reemplazar unidades del producto {ProductId}.", productId);
            return Fail<StrategicProductDto>("No se pudieron guardar las unidades de medida");
        }

        await LogActivityAsync(organizationId, userId, userName, "Edición de unidades de medida",
            $"Unidades de «{product.Name}» actualizadas.", planId);

        return await GetProductAsync(organizationId, planId, productId);
    }

    public async Task<IHttpResult<StrategicProductDto>> DiscontinueMeasureUnitAsync(
        int organizationId, Guid planId, Guid unitId, DiscontinueDto dto, Guid userId, string? userName)
    {
        var validation = ValidateDiscontinue(dto);
        if (validation is not null)
            return Fail<StrategicProductDto>(validation);

        var planResult = await GetDraftGraphAsync(organizationId, planId);
        if (!planResult.IsSuccess)
            return Fail<StrategicProductDto>(planResult.Error.Title, planResult.StatusCode);

        var unit = planResult.Value.Focuses
            .SelectMany(f => f.Objectives)
            .SelectMany(o => o.Products)
            .SelectMany(p => p.MeasureUnits)
            .FirstOrDefault(u => u.Id == unitId);
        if (unit is null)
            return Fail<StrategicProductDto>("La unidad de medida no existe", HttpStatusCode.NotFound);

        unit.DiscontinuedFromYear = dto.FromYear;
        unit.DiscontinuationReason = dto.Reason.Trim();

        try
        {
            await RegisterChangeAsync(planId, userId, userName, "Discontinued", "MeasureUnit",
                $"Unidad «{unit.Name}» descontinuada desde {dto.FromYear}. Motivo: {dto.Reason.Trim()}");
            await _uow.Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al descontinuar la unidad {UnitId}.", unitId);
            return Fail<StrategicProductDto>("No se pudo descontinuar la unidad de medida");
        }

        await LogActivityAsync(organizationId, userId, userName, "Descontinuación de unidad de medida",
            $"Unidad «{unit.Name}» descontinuada desde {dto.FromYear}.", planId);

        return await GetProductAsync(organizationId, planId, unit.ProductId);
    }

    // ───────────────────────── Helpers ─────────────────────────

    private async Task<StrategicPlan?> LoadGraphAsync(int organizationId, Guid? id = null, string? status = null)
    {
        var result = await _planRepository.GetAsync(
            p => p.OrganizationId == organizationId
                && (id == null || p.Id == id)
                && (status == null || p.Status == status),
            q => q
                .Include(p => p.Focuses).ThenInclude(f => f.Objectives)
                    .ThenInclude(o => o.Products).ThenInclude(pr => pr.MeasureUnits)
                        .ThenInclude(u => u.AnnualTargets)
                .Include(p => p.Focuses).ThenInclude(f => f.Objectives)
                    .ThenInclude(o => o.Products).ThenInclude(pr => pr.InvolvedAreas)
                .Include(p => p.Changes)
                .AsSplitQuery());

        return result.IsSuccess ? result.Value : null;
    }

    /// <summary>Carga el plan (sin grafo) y verifica que sea el Draft editable de la organización.</summary>
    private async Task<IHttpResult<StrategicPlan>> GetDraftAsync(int organizationId, Guid planId)
    {
        var result = await _planRepository.GetAsync(
            p => p.OrganizationId == organizationId && p.Id == planId);
        if (!result.IsSuccess)
            return Fail<StrategicPlan>("No se pudo recuperar el PEI");
        if (result.Value is null)
            return Fail<StrategicPlan>("La versión del PEI no existe", HttpStatusCode.NotFound);
        if (result.Value.Status != "Draft")
            return Fail<StrategicPlan>(
                "Solo la enmienda en elaboración es editable; las versiones vigente e histórica son inmutables",
                HttpStatusCode.Conflict);

        return HttpResult<StrategicPlan>.Success(result.Value);
    }

    /// <summary>Como <see cref="GetDraftAsync"/> pero con el grafo completo, vía el DbContext compartido
    /// del UnitOfWork para que las mutaciones sobre las navegaciones se persistan con SaveChanges.</summary>
    private async Task<IHttpResult<StrategicPlan>> GetDraftGraphAsync(int organizationId, Guid planId)
    {
        var plan = await _uow.Context.StrategicPlans
            .Include(p => p.Focuses).ThenInclude(f => f.Objectives)
                .ThenInclude(o => o.Products).ThenInclude(pr => pr.MeasureUnits)
                    .ThenInclude(u => u.AnnualTargets)
            .Include(p => p.Focuses).ThenInclude(f => f.Objectives)
                .ThenInclude(o => o.Products).ThenInclude(pr => pr.InvolvedAreas)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == planId);

        if (plan is null)
            return Fail<StrategicPlan>("La versión del PEI no existe", HttpStatusCode.NotFound);
        if (plan.Status != "Draft")
            return Fail<StrategicPlan>(
                "Solo la enmienda en elaboración es editable; las versiones vigente e histórica son inmutables",
                HttpStatusCode.Conflict);

        return HttpResult<StrategicPlan>.Success(plan);
    }

    private async Task<IHttpResult<StrategicProductDto>> GetProductAsync(int organizationId, Guid planId, Guid productId)
    {
        var plan = await LoadGraphAsync(organizationId, planId);
        var product = plan is null ? null : FindProduct(plan, productId);
        if (product is null)
            return Fail<StrategicProductDto>("El producto no existe", HttpStatusCode.NotFound);
        return HttpResult<StrategicProductDto>.Success(ToProductDto(product));
    }

    private static StrategicObjective? FindObjective(StrategicPlan plan, Guid objectiveId)
        => plan.Focuses.SelectMany(f => f.Objectives).FirstOrDefault(o => o.Id == objectiveId);

    private static StrategicProduct? FindProduct(StrategicPlan plan, Guid productId)
        => plan.Focuses.SelectMany(f => f.Objectives).SelectMany(o => o.Products)
            .FirstOrDefault(p => p.Id == productId);

    /// <summary>Una unidad tiene historia si comprometió metas (> 0) en años ya transcurridos.</summary>
    private static bool HasHistory(StrategicMeasureUnit unit)
        => unit.AnnualTargets.Any(t => t.Year <= CurrentYear && t.Value > 0);

    private static bool HasHistory(StrategicProduct product)
        => product.MeasureUnits.Any(HasHistory);

    private static string? ValidateDiscontinue(DiscontinueDto dto)
    {
        if (dto.FromYear <= CurrentYear)
            return "La descontinuación debe aplicar desde un año futuro";
        if (string.IsNullOrWhiteSpace(dto.Reason))
            return "El motivo de la descontinuación es obligatorio";
        return null;
    }

    /// <summary>Anota el registro de cambios de la versión (se persiste junto al SaveChanges de la mutación).</summary>
    private Task RegisterChangeAsync(Guid planId, Guid userId, string? userName, string changeType, string level, string description)
    {
        _uow.Context.StrategicPlanChanges.Add(new StrategicPlanChange
        {
            PlanId = planId,
            UserId = userId,
            UserName = userName,
            ChangeType = changeType,
            Level = level,
            Description = description
        });
        return Task.CompletedTask;
    }

    private Task LogActivityAsync(int organizationId, Guid userId, string? userName, string action, string detail, Guid planId)
        => _activityService.LogAsync(organizationId, userId, userName, ModuleName, action, detail,
            nameof(StrategicPlan), planId);

    // ───────────────────────── Mapeo manual (grafo ordenado) ─────────────────────────

    private static StrategicPlanDto ToSummaryDto(StrategicPlan plan) => new()
    {
        Id = plan.Id,
        Name = plan.Name,
        PeriodStartYear = plan.PeriodStartYear,
        PeriodEndYear = plan.PeriodEndYear,
        VersionNumber = plan.VersionNumber,
        Status = plan.Status,
        Reason = plan.Reason,
        CreatedByName = plan.CreatedByName,
        CreatedAt = plan.CreatedAt,
        ApprovedByName = plan.ApprovedByName,
        ApprovedAt = plan.ApprovedAt,
        PreviousVersionId = plan.PreviousVersionId,
        FocusCount = plan.Focuses.Count
    };

    private static StrategicPlanDetailDto ToDetailDto(StrategicPlan plan) => new()
    {
        Id = plan.Id,
        Name = plan.Name,
        PeriodStartYear = plan.PeriodStartYear,
        PeriodEndYear = plan.PeriodEndYear,
        VersionNumber = plan.VersionNumber,
        Status = plan.Status,
        Reason = plan.Reason,
        CreatedByName = plan.CreatedByName,
        CreatedAt = plan.CreatedAt,
        ApprovedByName = plan.ApprovedByName,
        ApprovedAt = plan.ApprovedAt,
        PreviousVersionId = plan.PreviousVersionId,
        FocusCount = plan.Focuses.Count,
        Focuses = plan.Focuses.OrderBy(f => f.Order).Select(ToFocusDto).ToList(),
        Changes = plan.Changes.OrderByDescending(c => c.Date).Select(c => new StrategicPlanChangeDto
        {
            Date = c.Date,
            UserName = c.UserName,
            ChangeType = c.ChangeType,
            Level = c.Level,
            Description = c.Description
        }).ToList()
    };

    private static StrategicFocusDto ToFocusDto(StrategicFocus focus) => new()
    {
        Id = focus.Id,
        LineageId = focus.LineageId,
        Name = focus.Name,
        EndAlignment = focus.EndAlignment,
        Order = focus.Order,
        Objectives = focus.Objectives.OrderBy(o => o.Order).Select(ToObjectiveDto).ToList()
    };

    private static StrategicObjectiveDto ToObjectiveDto(StrategicObjective objective) => new()
    {
        Id = objective.Id,
        LineageId = objective.LineageId,
        Code = objective.Code,
        Name = objective.Name,
        Order = objective.Order,
        Products = objective.Products.OrderBy(p => p.Order).Select(ToProductDto).ToList()
    };

    private static StrategicProductDto ToProductDto(StrategicProduct product) => new()
    {
        Id = product.Id,
        LineageId = product.LineageId,
        Name = product.Name,
        DiscontinuedFromYear = product.DiscontinuedFromYear,
        DiscontinuationReason = product.DiscontinuationReason,
        Order = product.Order,
        InvolvedAreas = product.InvolvedAreas
            .Select(a => new StrategicInvolvedAreaDto { AreaId = a.AreaId, AreaName = a.AreaName })
            .ToList(),
        MeasureUnits = product.MeasureUnits.OrderBy(u => u.Order).Select(u => new StrategicMeasureUnitDto
        {
            Id = u.Id,
            LineageId = u.LineageId,
            Name = u.Name,
            ResponsibleAreaId = u.ResponsibleAreaId,
            ResponsibleAreaName = u.ResponsibleAreaName,
            DiscontinuedFromYear = u.DiscontinuedFromYear,
            DiscontinuationReason = u.DiscontinuationReason,
            Order = u.Order,
            Targets = u.AnnualTargets.OrderBy(t => t.Year)
                .Select(t => new StrategicAnnualTargetDto { Year = t.Year, Value = t.Value })
                .ToList()
        }).ToList()
    };

    private static Dictionary<string, IEnumerable<string>> Errors(string message)
        => new() { { "errors", [message] } };

    private static HttpResult<T> Fail<T>(string message, HttpStatusCode status = HttpStatusCode.BadRequest)
        => HttpResult<T>.Failure(message, Errors(message), status);
}
