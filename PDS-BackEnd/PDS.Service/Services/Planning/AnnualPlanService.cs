using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDS.Application.Data;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Enums;
using PDS.Domain.Models;
using PDS.Domain.Response;
using System.Net;

namespace PDS.Service.Services.Planning;

public sealed class AnnualPlanService : IAnnualPlanService
{
    private const string ModuleName = "Plan anual";
    private const string ProductSequenceScope = "POA_PRODUCT";
    private const string TaskSequenceScope = "POA_TASK";

    private readonly IUnitOfWork<PDSDbContext> _unitOfWork;
    private readonly IPoaValidationService _validationService;
    private readonly ISequenceService _sequenceService;
    private readonly IPlanningActivityService _activityService;
    private readonly ILogger<AnnualPlanService> _logger;

    public AnnualPlanService(
        IUnitOfWork<PDSDbContext> unitOfWork,
        IPoaValidationService validationService,
        ISequenceService sequenceService,
        IPlanningActivityService activityService,
        ILogger<AnnualPlanService> logger)
    {
        _unitOfWork = unitOfWork;
        _validationService = validationService;
        _sequenceService = sequenceService;
        _activityService = activityService;
        _logger = logger;
    }

    // ───────────────────────── Lectura ─────────────────────────

    public async Task<IHttpResult<IEnumerable<AnnualPlanDto>>> ListAsync(
        int organizationId, Guid? cycleId, Guid? areaId, string? status)
    {
        var plans = await PlanGraphQuery(tracked: false)
            .Where(p => p.OrganizationId == organizationId
                && (cycleId == null || p.CycleId == cycleId)
                && (areaId == null || p.AreaId == areaId)
                && (status == null || p.Status == status))
            .OrderByDescending(p => p.Cycle!.Year).ThenBy(p => p.AreaName)
            .ToListAsync();

        var requiredColumnsByCycle = await RequiredColumnsByCycleAsync(plans.Select(p => p.CycleId).Distinct().ToList());

        var dtos = plans.Select(p => ToPlanDto(p, requiredColumnsByCycle.GetValueOrDefault(p.CycleId, []))).ToList();
        return HttpResult<IEnumerable<AnnualPlanDto>>.Success(dtos);
    }

    public async Task<IHttpResult<AnnualPlanWorkspaceDto>> GetWorkspaceAsync(int organizationId, Guid planId)
    {
        var plan = await PlanGraphQuery(tracked: false)
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == planId);
        if (plan is null)
            return Fail<AnnualPlanWorkspaceDto>("El plan no existe", HttpStatusCode.NotFound);

        var templateColumns = await TemplateColumnsAsync(plan.Cycle!.TemplateId);
        var requiredColumns = templateColumns.Where(c => c.Required && c.Visible).ToList();

        return HttpResult<AnnualPlanWorkspaceDto>.Success(new AnnualPlanWorkspaceDto
        {
            Id = plan.Id,
            CycleId = plan.CycleId,
            CycleName = plan.Cycle.Name,
            CycleYear = plan.Cycle.Year,
            CycleStatus = plan.Cycle.Status,
            AreaId = plan.AreaId,
            AreaName = plan.AreaName,
            Name = plan.Name,
            Status = plan.Status,
            Version = plan.Version,
            DueDate = plan.DueDate,
            GeneralObservation = plan.GeneralObservation,
            ProductCount = plan.Products.Count,
            IncompleteProductCount = plan.Products.Count(p => _validationService.GetProductIssues(p, requiredColumns).Count > 0),
            StrategicPlanId = plan.Cycle.StrategicPlanId,
            TemplateColumns = templateColumns.Where(c => c.Visible).OrderBy(c => c.Order).Select(ToColumnDto).ToList(),
            Products = plan.Products.OrderBy(p => p.Order).Select(p => ToProductDto(p, requiredColumns, templateColumns)).ToList(),
            ReadOnly = IsCycleClosed(plan.Cycle),
            RowVersion = Convert.ToBase64String(plan.RowVersion)
        });
    }

    public async Task<IHttpResult<PlanValidationDto>> GetValidationAsync(int organizationId, Guid planId)
    {
        var plan = await PlanGraphQuery(tracked: false)
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == planId);
        if (plan is null)
            return Fail<PlanValidationDto>("El plan no existe", HttpStatusCode.NotFound);

        var requiredColumns = (await TemplateColumnsAsync(plan.Cycle!.TemplateId))
            .Where(c => c.Required && c.Visible).ToList();

        var products = plan.Products.OrderBy(p => p.Order).Select(p => new ProductIssuesDto
        {
            ProductId = p.Id,
            Code = p.Code,
            ProductName = p.ProductName,
            Issues = _validationService.GetProductIssues(p, requiredColumns)
        }).ToList();

        return HttpResult<PlanValidationDto>.Success(new PlanValidationDto
        {
            PlanId = plan.Id,
            IsValid = products.All(p => p.Issues.Count == 0),
            Products = products
        });
    }

    public async Task<IHttpResult<PlanAccessInfo>> GetAccessInfoAsync(int organizationId, Guid planId)
    {
        var info = await _unitOfWork.Context.AnnualPlans
            .AsNoTracking()
            .Where(p => p.OrganizationId == organizationId && p.Id == planId)
            .Select(p => new PlanAccessInfo
            {
                AreaId = p.AreaId,
                Status = p.Status,
                CycleClosed = p.Cycle!.Status == "Closed"
            })
            .FirstOrDefaultAsync();

        return info is null
            ? Fail<PlanAccessInfo>("El plan no existe", HttpStatusCode.NotFound)
            : HttpResult<PlanAccessInfo>.Success(info);
    }

    // ───────────────────────── Productos ─────────────────────────

    public async Task<IHttpResult<PoaProductDto>> AddProductAsync(
        int organizationId, Guid planId, Guid userId, string? userName)
    {
        var planResult = await GetEditablePlanAsync(organizationId, planId);
        if (!planResult.IsSuccess)
            return Fail<PoaProductDto>(planResult.Error.Title, planResult.StatusCode);
        var plan = planResult.Value;

        // Prefijo del código a partir del nombre del área (patrón addRow del mock: "TR-01").
        var prefix = new string(plan.AreaName.Where(char.IsLetter).Take(2).ToArray()).ToUpperInvariant();
        if (prefix.Length == 0) prefix = "PR";
        var number = await _sequenceService.NextAsync(organizationId, ProductSequenceScope, plan.Id.ToString());

        var product = new PoaProduct
        {
            PlanId = plan.Id,
            Code = $"{prefix}-{number:D2}",
            Origin = "Pei",
            ProductName = string.Empty,
            Order = plan.Products.Count == 0 ? 1 : plan.Products.Max(p => p.Order) + 1
        };

        // Unidad vacía por defecto con el área del plan como responsable (mock).
        product.MeasureUnits.Add(new PoaMeasureUnit
        {
            Name = string.Empty,
            TargetValue = 0,
            TargetType = "Absolute",
            ResponsibleAreaId = plan.AreaId,
            ResponsibleAreaName = plan.AreaName,
            Q1 = 0, Q2 = 0, Q3 = 0, Q4 = 0
        });

        try
        {
            _unitOfWork.Context.PoaProducts.Add(product);
            BumpPlanIfNotStarted(plan);
            await _unitOfWork.Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al agregar producto al plan {PlanId}.", planId);
            return Fail<PoaProductDto>("No se pudo agregar el producto");
        }

        await _activityService.LogAsync(organizationId, userId, userName, ModuleName,
            "Agregó un producto", $"{plan.Name}: producto {product.Code} agregado.", nameof(AnnualPlan), plan.Id);

        var templateColumns = await TemplateColumnsAsync(plan.Cycle!.TemplateId);
        return HttpResult<PoaProductDto>.Success(
            ToProductDto(product, templateColumns.Where(c => c.Required && c.Visible).ToList(), templateColumns),
            HttpStatusCode.Created);
    }

    public async Task<IHttpResult<PoaProductDto>> UpdateProductAsync(
        int organizationId, Guid planId, Guid productId, UpdatePoaProductDto dto, Guid userId, string? userName)
    {
        var planResult = await GetEditablePlanAsync(organizationId, planId);
        if (!planResult.IsSuccess)
            return Fail<PoaProductDto>(planResult.Error.Title, planResult.StatusCode);
        var plan = planResult.Value;

        var product = plan.Products.FirstOrDefault(p => p.Id == productId);
        if (product is null)
            return Fail<PoaProductDto>("El producto no existe", HttpStatusCode.NotFound);

        // Concurrencia optimista: si el cliente trae el token y no coincide → 409.
        if (!string.IsNullOrEmpty(dto.RowVersion))
        {
            var incoming = Convert.FromBase64String(dto.RowVersion);
            if (!incoming.SequenceEqual(product.RowVersion))
                return Fail<PoaProductDto>(
                    "El producto fue modificado por otro usuario; recargue la fila", HttpStatusCode.Conflict);
        }

        var ctx = _unitOfWork.Context;

        if (dto.Origin is not null)
        {
            if (dto.Origin is not ("Pei" or "Operational"))
                return Fail<PoaProductDto>("Origen inválido: use Pei u Operational");
            product.Origin = dto.Origin;
        }

        if (dto.ClearAlignment == true)
        {
            product.StrategicFocusId = null;
            product.StrategicObjectiveId = null;
            product.StrategicProductId = null;
        }
        else if (dto.StrategicFocusId.HasValue)
        {
            product.StrategicFocusId = dto.StrategicFocusId;
            product.StrategicObjectiveId = dto.StrategicObjectiveId;
            product.StrategicProductId = dto.StrategicProductId;
        }

        if (dto.ProductName is not null) product.ProductName = dto.ProductName.Trim();
        if (dto.OperationalJustification is not null) product.OperationalJustification = dto.OperationalJustification;
        if (dto.Observation is not null) product.Observation = dto.Observation;

        if (dto.MeasureUnits is not null)
        {
            var reqAreaIds = dto.MeasureUnits.Where(u => u.ResponsibleAreaId.HasValue).Select(u => u.ResponsibleAreaId!.Value).Distinct().ToList();
            var areaNames = await AreaNamesAsync(organizationId, reqAreaIds);

            var missingAreaId = reqAreaIds.FirstOrDefault(id => !areaNames.ContainsKey(id));
            if (missingAreaId != Guid.Empty)
            {
                return HttpResult<PoaProductDto>.Failure(
                    "Área responsable inválida",
                    new Dictionary<string, IEnumerable<string>> { { "responsibleAreaId", new[] { $"El área responsable '{missingAreaId}' no existe en su organización." } } });
            }

            var keptIds = new HashSet<Guid>();
            foreach (var upsert in dto.MeasureUnits)
            {
                var existing = upsert.Id.HasValue
                    ? product.MeasureUnits.FirstOrDefault(u => u.Id == upsert.Id.Value)
                    : null;

                if (existing is null)
                {
                    var unit = new PoaMeasureUnit
                    {
                        ProductId = product.Id,
                        Name = upsert.Name.Trim(),
                        TargetValue = upsert.TargetValue,
                        TargetType = upsert.TargetType,
                        VerificationMeans = upsert.VerificationMeans,
                        ResponsibleAreaId = upsert.ResponsibleAreaId,
                        ResponsibleAreaName = upsert.ResponsibleAreaId.HasValue
                            ? areaNames.GetValueOrDefault(upsert.ResponsibleAreaId.Value)
                            : null,
                        Q1 = upsert.Q1, Q2 = upsert.Q2, Q3 = upsert.Q3, Q4 = upsert.Q4
                    };
                    product.MeasureUnits.Add(unit);
                    ctx.PoaMeasureUnits.Add(unit);
                }
                else
                {
                    keptIds.Add(existing.Id);
                    existing.Name = upsert.Name.Trim();
                    existing.TargetValue = upsert.TargetValue;
                    existing.TargetType = upsert.TargetType;
                    existing.VerificationMeans = upsert.VerificationMeans;
                    existing.ResponsibleAreaId = upsert.ResponsibleAreaId;
                    existing.ResponsibleAreaName = upsert.ResponsibleAreaId.HasValue
                        ? areaNames.GetValueOrDefault(upsert.ResponsibleAreaId.Value)
                        : null;
                    existing.Q1 = upsert.Q1;
                    existing.Q2 = upsert.Q2;
                    existing.Q3 = upsert.Q3;
                    existing.Q4 = upsert.Q4;
                }
            }

            foreach (var removed in product.MeasureUnits.Where(u => u.Id != Guid.Empty && !keptIds.Contains(u.Id)
                         && !dto.MeasureUnits.Any(x => x.Id == u.Id)).ToList())
            {
                if (!dto.MeasureUnits.Any(x => x.Id == removed.Id))
                {
                    product.MeasureUnits.Remove(removed);
                    ctx.PoaMeasureUnits.Remove(removed);
                }
            }
        }

        if (dto.Tasks is not null)
        {
            var keptIds = new HashSet<Guid>();
            foreach (var upsert in dto.Tasks)
            {
                var existing = upsert.Id.HasValue
                    ? product.Tasks.FirstOrDefault(t => t.Id == upsert.Id.Value)
                    : null;

                if (existing is null)
                {
                    var number = await _sequenceService.NextAsync(organizationId, TaskSequenceScope, product.Id.ToString());
                    var task = new PoaTask
                    {
                        ProductId = product.Id,
                        Code = $"TR-{number:D2}",
                        Name = upsert.Name.Trim(),
                        IsContinuous = upsert.IsContinuous,
                        MonthsMask = ToMonthsMask(upsert.Months)
                    };
                    product.Tasks.Add(task);
                    ctx.PoaTasks.Add(task);
                }
                else
                {
                    var newName = upsert.Name.Trim();
                    if (!string.Equals(existing.Name, newName, StringComparison.Ordinal))
                    {
                        var activeReqCodes = await ctx.Requirements
                            .Where(r => r.OrganizationId == organizationId && r.PoaTaskId == existing.Id && r.Status != "Merged")
                            .Select(r => r.Code)
                            .ToListAsync();

                        if (activeReqCodes.Count > 0)
                        {
                            return HttpResult<PoaProductDto>.Failure(
                                "Tarea con requerimientos activos",
                                new Dictionary<string, IEnumerable<string>> { { "tasks", new[] { $"No se puede renombrar la tarea '{existing.Name}' porque tiene requerimientos activos vinculados: {string.Join(", ", activeReqCodes)}" } } },
                                HttpStatusCode.Conflict);
                        }
                    }

                    keptIds.Add(existing.Id);
                    existing.Name = newName;
                    existing.IsContinuous = upsert.IsContinuous;
                    existing.MonthsMask = ToMonthsMask(upsert.Months);
                }
            }

            var removedTaskIds = product.Tasks
                .Where(t => t.Id != Guid.Empty && !keptIds.Contains(t.Id) && !dto.Tasks.Any(x => x.Id == t.Id))
                .Select(t => t.Id)
                .ToList();

            if (removedTaskIds.Count > 0)
            {
                var activeReqCodes = await ctx.Requirements
                    .Where(r => r.OrganizationId == organizationId && removedTaskIds.Contains(r.PoaTaskId) && r.Status != "Merged")
                    .Select(r => r.Code)
                    .ToListAsync();

                if (activeReqCodes.Count > 0)
                {
                    return HttpResult<PoaProductDto>.Failure(
                        "Tarea con requerimientos activos",
                        new Dictionary<string, IEnumerable<string>> { { "tasks", new[] { $"No se puede eliminar la tarea porque tiene requerimientos activos vinculados: {string.Join(", ", activeReqCodes)}" } } },
                        HttpStatusCode.Conflict);
                }
            }

            foreach (var removed in product.Tasks.Where(t => t.Id != Guid.Empty && !keptIds.Contains(t.Id)
                         && !dto.Tasks.Any(x => x.Id == t.Id)).ToList())
            {
                product.Tasks.Remove(removed);
                ctx.PoaTasks.Remove(removed);
            }
        }

        if (dto.MunicipioIds is not null)
        {
            var municipios = await ctx.Municipios
                .Where(m => dto.MunicipioIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => m.Nombre);
            if (dto.MunicipioIds.Any(id => !municipios.ContainsKey(id)))
                return Fail<PoaProductDto>("Un municipio indicado no existe en el catálogo territorial");

            foreach (var removed in product.Municipalities.ToList())
            {
                if (!dto.MunicipioIds.Contains(removed.MunicipioId))
                {
                    product.Municipalities.Remove(removed);
                    ctx.PoaProductMunicipalities.Remove(removed);
                }
            }

            foreach (var municipioId in dto.MunicipioIds.Distinct())
            {
                if (!product.Municipalities.Any(m => m.MunicipioId == municipioId))
                {
                    var link = new PoaProductMunicipality
                    {
                        ProductId = product.Id,
                        MunicipioId = municipioId,
                        MunicipioName = municipios[municipioId]
                    };
                    product.Municipalities.Add(link);
                    ctx.PoaProductMunicipalities.Add(link);
                }
            }
        }

        if (dto.InvolvedAreaIds is not null)
        {
            var areaNames = await AreaNamesAsync(organizationId, dto.InvolvedAreaIds);
            if (dto.InvolvedAreaIds.Any(id => !areaNames.ContainsKey(id)))
                return Fail<PoaProductDto>("Un área involucrada no existe en el organigrama");

            foreach (var removed in product.InvolvedAreas.ToList())
            {
                if (!dto.InvolvedAreaIds.Contains(removed.AreaId))
                {
                    product.InvolvedAreas.Remove(removed);
                    ctx.PoaProductInvolvedAreas.Remove(removed);
                }
            }

            foreach (var areaId in dto.InvolvedAreaIds.Distinct())
            {
                if (!product.InvolvedAreas.Any(a => a.AreaId == areaId))
                {
                    var link = new PoaProductInvolvedArea
                    {
                        ProductId = product.Id,
                        AreaId = areaId,
                        AreaName = areaNames[areaId]
                    };
                    product.InvolvedAreas.Add(link);
                    ctx.PoaProductInvolvedAreas.Add(link);
                }
            }
        }

        if (dto.CustomValues is not null)
        {
            var columns = await TemplateColumnsAsync(plan.Cycle!.TemplateId);
            foreach (var (technicalName, value) in dto.CustomValues)
            {
                var column = columns.FirstOrDefault(c =>
                    string.Equals(c.TechnicalName, technicalName, StringComparison.OrdinalIgnoreCase));
                if (column is null)
                    continue; // Columna desconocida: se ignora (la plantilla pudo cambiar).

                var existing = product.CustomValues.FirstOrDefault(cv => cv.TemplateColumnId == column.Id);
                if (existing is null)
                {
                    var customValue = new PoaProductCustomValue
                    {
                        ProductId = product.Id,
                        TemplateColumnId = column.Id,
                        Value = value
                    };
                    product.CustomValues.Add(customValue);
                    ctx.PoaProductCustomValues.Add(customValue);
                }
                else
                {
                    existing.Value = value;
                }
            }
        }

        try
        {
            product.UpdatedAt = DateTime.UtcNow;
            BumpPlanIfNotStarted(plan);
            await ctx.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Fail<PoaProductDto>("El producto fue modificado por otro usuario; recargue la fila", HttpStatusCode.Conflict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar el producto {ProductId}.", productId);
            return Fail<PoaProductDto>("No se pudo actualizar el producto");
        }

        var allColumns = await TemplateColumnsAsync(plan.Cycle!.TemplateId);
        return HttpResult<PoaProductDto>.Success(
            ToProductDto(product, allColumns.Where(c => c.Required && c.Visible).ToList(), allColumns));
    }

    public async Task<IHttpResult> DeleteProductAsync(
        int organizationId, Guid planId, Guid productId, Guid userId, string? userName)
    {
        var planResult = await GetEditablePlanAsync(organizationId, planId);
        if (!planResult.IsSuccess)
            return HttpResult.Failure(planResult.Error.Title, planResult.Error.Detail, planResult.StatusCode);
        var plan = planResult.Value;

        var product = plan.Products.FirstOrDefault(p => p.Id == productId);
        if (product is null)
            return HttpResult.Failure("El producto no existe", Errors("El producto no existe"), HttpStatusCode.NotFound);

        var taskIds = product.Tasks.Select(t => t.Id).ToList();
        var activeReqCodes = await _unitOfWork.Context.Requirements
            .Where(r => r.OrganizationId == organizationId && (r.PoaProductId == product.Id || taskIds.Contains(r.PoaTaskId)) && r.Status != "Merged")
            .Select(r => r.Code)
            .ToListAsync();

        if (activeReqCodes.Count > 0)
        {
            return HttpResult.Failure(
                "Producto con requerimientos activos",
                new Dictionary<string, IEnumerable<string>> { { "product", new[] { $"No se puede eliminar el producto porque tiene requerimientos activos vinculados: {string.Join(", ", activeReqCodes)}" } } },
                HttpStatusCode.Conflict);
        }

        try
        {
            _unitOfWork.Context.PoaProducts.Remove(product);
            await _unitOfWork.Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar el producto {ProductId}.", productId);
            return HttpResult.Failure("No se pudo eliminar el producto",
                Errors("No se pudo eliminar el producto"), HttpStatusCode.BadRequest);
        }

        await _activityService.LogAsync(organizationId, userId, userName, ModuleName,
            "Eliminó un producto", $"{plan.Name}: producto {product.Code} eliminado.", nameof(AnnualPlan), plan.Id);

        return HttpResult.Success();
    }

    // ───────────────────────── Workflow ─────────────────────────

    public async Task<IHttpResult<AnnualPlanDto>> SubmitAsync(int organizationId, Guid planId, Guid userId, string? userName)
    {
        var plan = await PlanGraphQuery(tracked: true)
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == planId);
        if (plan is null)
            return Fail<AnnualPlanDto>("El plan no existe", HttpStatusCode.NotFound);
        if (IsCycleClosed(plan.Cycle!))
            return Fail<AnnualPlanDto>("El ciclo está cerrado", HttpStatusCode.Conflict);
        if (!AnnualPlanStatuses.Editables.Contains(plan.Status))
            return Fail<AnnualPlanDto>($"El plan no puede someterse desde el estado «{plan.Status}»", HttpStatusCode.Conflict);

        var requiredColumns = (await TemplateColumnsAsync(plan.Cycle!.TemplateId))
            .Where(c => c.Required && c.Visible).ToList();

        var pending = plan.Products
            .OrderBy(p => p.Order)
            .Select(p => (p.Code, Issues: _validationService.GetProductIssues(p, requiredColumns)))
            .Where(x => x.Issues.Count > 0)
            .ToList();

        if (plan.Products.Count == 0)
            return Fail<AnnualPlanDto>("El plan no tiene productos");

        if (pending.Count > 0)
        {
            var detail = new Dictionary<string, IEnumerable<string>>
            {
                { "errors", pending.Select(p => $"{p.Code}: {string.Join(" · ", p.Issues)}").ToList() }
            };
            return HttpResult<AnnualPlanDto>.Failure(
                "El plan tiene productos incompletos", detail, HttpStatusCode.BadRequest);
        }

        plan.Status = AnnualPlanStatuses.ListoParaRevision;
        plan.Version += 1;
        plan.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _unitOfWork.Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al someter el plan {PlanId}.", planId);
            return Fail<AnnualPlanDto>("No se pudo someter el plan");
        }

        await _activityService.LogAsync(organizationId, userId, userName, ModuleName,
            "Sometió el plan a revisión", $"«{plan.Name}» sometido (v{plan.Version}).", nameof(AnnualPlan), plan.Id);

        return HttpResult<AnnualPlanDto>.Success(ToPlanDto(plan, requiredColumns));
    }

    public async Task<IHttpResult<AnnualPlanDto>> ReturnAsync(
        int organizationId, Guid planId, ReturnPlanDto dto, Guid userId, string? userName)
    {
        if (string.IsNullOrWhiteSpace(dto.Observation))
            return Fail<AnnualPlanDto>("La observación es obligatoria al devolver un plan");

        return await TransitionAsync(organizationId, planId, AnnualPlanStatuses.ListoParaRevision,
            AnnualPlanStatuses.Observado, userId, userName, "Devolvió el plan con observaciones",
            plan =>
            {
                plan.GeneralObservation = dto.Observation.Trim();
                return $"«{plan.Name}»: {dto.Observation.Trim()}";
            });
    }

    public Task<IHttpResult<AnnualPlanDto>> ApproveAsync(int organizationId, Guid planId, Guid userId, string? userName)
        => TransitionAsync(organizationId, planId, AnnualPlanStatuses.ListoParaRevision,
            AnnualPlanStatuses.Aprobado, userId, userName, "Aprobó el plan",
            plan =>
            {
                plan.GeneralObservation = null;
                return $"«{plan.Name}» aprobado.";
            });

    private async Task<IHttpResult<AnnualPlanDto>> TransitionAsync(
        int organizationId, Guid planId, string fromStatus, string toStatus,
        Guid userId, string? userName, string action, Func<AnnualPlan, string> apply)
    {
        var plan = await _unitOfWork.Context.AnnualPlans
            .Include(p => p.Cycle)
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == planId);
        if (plan is null)
            return Fail<AnnualPlanDto>("El plan no existe", HttpStatusCode.NotFound);
        if (IsCycleClosed(plan.Cycle!))
            return Fail<AnnualPlanDto>("El ciclo está cerrado", HttpStatusCode.Conflict);
        if (plan.Status != fromStatus)
            return Fail<AnnualPlanDto>(
                $"La operación requiere el estado «{fromStatus}» (actual: «{plan.Status}»)", HttpStatusCode.Conflict);

        plan.Status = toStatus;
        plan.UpdatedAt = DateTime.UtcNow;
        var detail = apply(plan);

        try
        {
            await _unitOfWork.Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en la transición del plan {PlanId} a {Status}.", planId, toStatus);
            return Fail<AnnualPlanDto>("No se pudo actualizar el estado del plan");
        }

        await _activityService.LogAsync(organizationId, userId, userName, ModuleName, action, detail,
            nameof(AnnualPlan), plan.Id);

        return HttpResult<AnnualPlanDto>.Success(ToPlanDto(plan, []));
    }

    // ───────────────────────── Helpers ─────────────────────────

    private IQueryable<AnnualPlan> PlanGraphQuery(bool tracked)
    {
        IQueryable<AnnualPlan> query = _unitOfWork.Context.AnnualPlans
            .Include(p => p.Cycle)
            .Include(p => p.Products).ThenInclude(pr => pr.MeasureUnits)
            .Include(p => p.Products).ThenInclude(pr => pr.Tasks)
            .Include(p => p.Products).ThenInclude(pr => pr.Municipalities)
            .Include(p => p.Products).ThenInclude(pr => pr.InvolvedAreas)
            .Include(p => p.Products).ThenInclude(pr => pr.CustomValues)
            .AsSplitQuery();

        return tracked ? query : query.AsNoTracking();
    }

    /// <summary>Plan con grafo, verificado como editable (estado + ciclo abierto), tracked.</summary>
    private async Task<IHttpResult<AnnualPlan>> GetEditablePlanAsync(int organizationId, Guid planId)
    {
        var plan = await PlanGraphQuery(tracked: true)
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == planId);
        if (plan is null)
            return Fail<AnnualPlan>("El plan no existe", HttpStatusCode.NotFound);
        if (IsCycleClosed(plan.Cycle!))
            return Fail<AnnualPlan>("El ciclo está cerrado: el plan es de solo lectura", HttpStatusCode.Conflict);
        if (!AnnualPlanStatuses.Editables.Contains(plan.Status))
            return Fail<AnnualPlan>(
                $"El plan no es editable en estado «{plan.Status}»", HttpStatusCode.Conflict);

        return HttpResult<AnnualPlan>.Success(plan);
    }

    private static bool IsCycleClosed(PlanningCycle cycle)
        => string.Equals(cycle.Status, "Closed", StringComparison.OrdinalIgnoreCase);

    private static void BumpPlanIfNotStarted(AnnualPlan plan)
    {
        if (plan.Status == AnnualPlanStatuses.SinIniciar)
        {
            plan.Status = AnnualPlanStatuses.Borrador;
            plan.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task<List<PlanningTemplateColumn>> TemplateColumnsAsync(Guid templateId)
        => await _unitOfWork.Context.PlanningTemplateColumns
            .AsNoTracking()
            .Where(c => c.TemplateId == templateId && c.DeletedAt == null)
            .OrderBy(c => c.Order)
            .ToListAsync();

    private async Task<Dictionary<Guid, List<PlanningTemplateColumn>>> RequiredColumnsByCycleAsync(List<Guid> cycleIds)
    {
        var cycles = await _unitOfWork.Context.PlanningCycles
            .AsNoTracking()
            .Where(c => cycleIds.Contains(c.Id))
            .Select(c => new { c.Id, c.TemplateId })
            .ToListAsync();

        var templateIds = cycles.Select(c => c.TemplateId).Distinct().ToList();
        var columns = await _unitOfWork.Context.PlanningTemplateColumns
            .AsNoTracking()
            .Where(c => templateIds.Contains(c.TemplateId) && c.DeletedAt == null && c.Required && c.Visible)
            .ToListAsync();

        return cycles.ToDictionary(
            c => c.Id,
            c => columns.Where(col => col.TemplateId == c.TemplateId).ToList());
    }

    private async Task<Dictionary<Guid, string>> AreaNamesAsync(int organizationId, IEnumerable<Guid> areaIds)
    {
        var ids = areaIds.Distinct().ToList();
        if (ids.Count == 0) return [];
        return await _unitOfWork.Context.OrganizationalStructures
            .AsNoTracking()
            .Where(s => s.OrganizationId == organizationId && ids.Contains(s.Id) && s.DeletedAt == null)
            .ToDictionaryAsync(s => s.Id, s => s.Name);
    }

    private static short ToMonthsMask(IReadOnlyCollection<int> months)
    {
        short mask = 0;
        foreach (var month in months.Where(m => m is >= 0 and <= 11))
            mask |= (short)(1 << month);
        return mask;
    }

    internal static IReadOnlyCollection<int> ToMonths(short mask)
        => Enumerable.Range(0, 12).Where(m => (mask & (1 << m)) != 0).ToList();

    private AnnualPlanDto ToPlanDto(AnnualPlan plan, IReadOnlyCollection<PlanningTemplateColumn> requiredColumns) => new()
    {
        Id = plan.Id,
        CycleId = plan.CycleId,
        CycleName = plan.Cycle?.Name ?? string.Empty,
        CycleYear = plan.Cycle?.Year ?? 0,
        CycleStatus = plan.Cycle?.Status ?? string.Empty,
        AreaId = plan.AreaId,
        AreaName = plan.AreaName,
        Name = plan.Name,
        Status = plan.Status,
        Version = plan.Version,
        DueDate = plan.DueDate,
        GeneralObservation = plan.GeneralObservation,
        ProductCount = plan.Products.Count,
        IncompleteProductCount = plan.Products.Count(p => _validationService.GetProductIssues(p, requiredColumns).Count > 0)
    };

    private PoaProductDto ToProductDto(
        PoaProduct product,
        IReadOnlyCollection<PlanningTemplateColumn> requiredColumns,
        IReadOnlyCollection<PlanningTemplateColumn> allColumns) => new()
    {
        Id = product.Id,
        Code = product.Code,
        Origin = product.Origin,
        StrategicFocusId = product.StrategicFocusId,
        StrategicObjectiveId = product.StrategicObjectiveId,
        StrategicProductId = product.StrategicProductId,
        ProductName = product.ProductName,
        OperationalJustification = product.OperationalJustification,
        Observation = product.Observation,
        Order = product.Order,
        MeasureUnits = product.MeasureUnits.Select(u => new PoaMeasureUnitDto
        {
            Id = u.Id,
            Name = u.Name,
            TargetValue = u.TargetValue,
            TargetType = u.TargetType,
            VerificationMeans = u.VerificationMeans,
            ResponsibleAreaId = u.ResponsibleAreaId,
            ResponsibleAreaName = u.ResponsibleAreaName,
            Q1 = u.Q1, Q2 = u.Q2, Q3 = u.Q3, Q4 = u.Q4
        }).ToList(),
        Tasks = product.Tasks.OrderBy(t => t.Code).Select(t => new PoaTaskDto
        {
            Id = t.Id,
            Code = t.Code,
            Name = t.Name,
            IsContinuous = t.IsContinuous,
            Months = ToMonths(t.MonthsMask)
        }).ToList(),
        Municipalities = product.Municipalities.Select(m => new PoaMunicipalityDto
        {
            MunicipioId = m.MunicipioId,
            MunicipioName = m.MunicipioName
        }).ToList(),
        InvolvedAreas = product.InvolvedAreas.Select(a => new StrategicInvolvedAreaDto
        {
            AreaId = a.AreaId,
            AreaName = a.AreaName
        }).ToList(),
        CustomValues = product.CustomValues
            .Select(cv => new
            {
                Technical = allColumns.FirstOrDefault(c => c.Id == cv.TemplateColumnId)?.TechnicalName,
                cv.Value
            })
            .Where(x => x.Technical is not null && x.Value is not null)
            .ToDictionary(x => x.Technical!, x => x.Value!),
        Issues = _validationService.GetProductIssues(product, requiredColumns),
        RowVersion = Convert.ToBase64String(product.RowVersion)
    };

    private static PlanningTemplateColumnDto ToColumnDto(PlanningTemplateColumn column) => new()
    {
        Id = column.Id,
        VisibleName = column.VisibleName,
        TechnicalName = column.TechnicalName,
        Type = column.Type,
        Required = column.Required,
        Visible = column.Visible,
        Origin = column.Origin,
        OptionsJson = column.OptionsJson,
        Order = column.Order,
        Version = column.Version,
        CreatedAt = column.CreatedAt
    };

    private static Dictionary<string, IEnumerable<string>> Errors(string message)
        => new() { { "errors", [message] } };

    private static HttpResult<T> Fail<T>(string message, HttpStatusCode status = HttpStatusCode.BadRequest)
        => HttpResult<T>.Failure(message, Errors(message), status);
}
