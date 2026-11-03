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

namespace PDS.Service.Services.Planning;

public class RequirementService : IRequirementService
{
    private readonly IUnitOfWork<PDSDbContext> _unitOfWork;
    private readonly IRepository<Requirement> _requirementRepository;
    private readonly IRepository<PoaTask> _taskRepository;
    private readonly IRepository<PoaProduct> _productRepository;
    private readonly IRepository<AnnualPlan> _planRepository;
    private readonly IRepository<MasterProduct> _masterProductRepository;
    private readonly ISequenceService _sequenceService;
    private readonly IPlanningActivityService _activityService;

    public RequirementService(
        IUnitOfWork<PDSDbContext> unitOfWork,
        IRepository<Requirement> requirementRepository,
        IRepository<PoaTask> taskRepository,
        IRepository<PoaProduct> productRepository,
        IRepository<AnnualPlan> planRepository,
        IRepository<MasterProduct> masterProductRepository,
        ISequenceService sequenceService,
        IPlanningActivityService activityService)
    {
        _unitOfWork = unitOfWork;
        _requirementRepository = requirementRepository;
        _taskRepository = taskRepository;
        _productRepository = productRepository;
        _planRepository = planRepository;
        _masterProductRepository = masterProductRepository;
        _sequenceService = sequenceService;
        _activityService = activityService;
    }

    public async Task<IHttpResult<PagedResponseDto<RequirementDto>>> ListAsync(
        int organizationId,
        Guid? cycleId,
        Guid? consolidatorAreaId,
        Guid? originAreaId,
        string? status,
        PaginationFilterDto pagination)
    {
        var query = _unitOfWork.Context.Requirements
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId);

        if (cycleId.HasValue)
            query = query.Where(r => r.CycleId == cycleId.Value);

        if (consolidatorAreaId.HasValue)
            query = query.Where(r => r.ConsolidatorAreaId == consolidatorAreaId.Value);

        if (originAreaId.HasValue)
            query = query.Where(r => r.OriginAreaId == originAreaId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        var totalRecords = await query.CountAsync();
        var pageNumber = Math.Max(1, pagination.PageNumber);
        var pageSize = Math.Clamp(pagination.PageSize, 1, 200);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(MapToDto).ToList();
        var response = new PagedResponseDto<RequirementDto>(dtos, pageNumber, pageSize, totalRecords, null, null, null, null);

        return HttpResult<PagedResponseDto<RequirementDto>>.Success(response);
    }

    public async Task<IHttpResult<RequirementDetailDto>> GetByIdAsync(int organizationId, Guid id)
    {
        var req = await _unitOfWork.Context.Requirements
            .AsNoTracking()
            .Include(r => r.Comments)
            .Include(r => r.Decisions)
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Id == id);

        if (req is null)
        {
            return HttpResult<RequirementDetailDto>.Failure(
                "Requerimiento no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { $"El requerimiento '{id}' no existe." } } },
                HttpStatusCode.NotFound);
        }

        return HttpResult<RequirementDetailDto>.Success(MapToDetailDto(req));
    }

    public async Task<IHttpResult<RequirementDetailDto>> CreateFromTaskAsync(
        int organizationId,
        CreateRequirementDto dto,
        Guid userId,
        string? userName,
        Guid originAreaId,
        string originAreaName)
    {
        // 1. Validar Tarea
        var task = await _unitOfWork.Context.PoaTasks
            .Include(t => t.Product)
            .ThenInclude(p => p!.Plan)
            .ThenInclude(pl => pl!.Cycle)
            .FirstOrDefaultAsync(t => t.Id == dto.PoaTaskId && t.Product!.Plan!.OrganizationId == organizationId);

        if (task is null || task.Product is null || task.Product.Plan is null)
        {
            return HttpResult<RequirementDetailDto>.Failure(
                "Tarea no encontrada",
                new Dictionary<string, IEnumerable<string>> { { "poaTaskId", new[] { "La tarea POA no existe o no está vinculada a un plan de su organización." } } },
                HttpStatusCode.NotFound);
        }

        var plan = task.Product.Plan;
        var cycle = plan.Cycle;

        if (plan.OrganizationId != organizationId)
        {
            return HttpResult<RequirementDetailDto>.Failure(
                "Acceso no autorizado",
                new Dictionary<string, IEnumerable<string>> { { "originAreaId", new[] { "La tarea POA no pertenece a su organización." } } },
                HttpStatusCode.Forbidden);
        }

        var effectiveOriginAreaId = plan.AreaId != Guid.Empty ? plan.AreaId : (originAreaId != Guid.Empty ? originAreaId : Guid.Empty);
        var effectiveOriginAreaName = !string.IsNullOrWhiteSpace(plan.AreaName) ? plan.AreaName : (!string.IsNullOrWhiteSpace(originAreaName) ? originAreaName : "Área Origen");

        if (cycle is null)
        {
            return HttpResult<RequirementDetailDto>.Failure(
                "Ciclo no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "cycleId", new[] { "El plan no está vinculado a un ciclo de planificación activo." } } });
        }

        // 2. Validar Artículo Maestro y resolver área consolidadora
        var (product, consolidatorAreaId, consolidatorAreaName, appliedRule) = await ResolveFromMasterProductAsync(organizationId, dto.MasterProductId);

        if (product is null)
        {
            return HttpResult<RequirementDetailDto>.Failure(
                "Artículo no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "masterProductId", new[] { "El artículo seleccionado no existe." } } },
                HttpStatusCode.BadRequest);
        }

        if (consolidatorAreaId == Guid.Empty)
        {
            return HttpResult<RequirementDetailDto>.Failure(
                "Artículo no disponible",
                new Dictionary<string, IEnumerable<string>> { { "masterProductId", new[] { "El artículo no se encuentra activo o no tiene un área consolidadora resuelta." } } },
                HttpStatusCode.BadRequest);
        }

        // 3. Resolver precio de referencia
        var unitPrice = dto.EstimatedUnitPrice > 0 ? dto.EstimatedUnitPrice : (product.ReferencePrice ?? 0);

        // 4. Generar código secuencial
        var seq = await _sequenceService.NextAsync(organizationId, "REQUIREMENT", cycle.Year.ToString());
        var code = $"RQ-{cycle.Year}-{seq:D3}";

        var req = new Requirement
        {
            OrganizationId = organizationId,
            Code = code,
            CycleId = cycle.Id,
            PlanId = plan.Id,
            PoaProductId = task.ProductId,
            PoaTaskId = task.Id,
            OriginAreaId = effectiveOriginAreaId,
            OriginAreaName = effectiveOriginAreaName,
            ConsolidatorAreaId = consolidatorAreaId,
            ConsolidatorAreaName = consolidatorAreaName,
            AppliedRule = appliedRule,
            MasterProductId = product.Id,
            MasterProductCode = product.Code,
            MasterProductName = product.Name,
            UnspscCode = product.UnspscCode,
            UnspscName = product.UnspscName,
            ExpenseObjectCode = product.ExpenseObjectCode,
            ExpenseObjectName = product.ExpenseObjectName,
            AccountingAccountCode = product.AccountingAccountCode,
            AccountingAccountName = product.AccountingAccountName,
            ItemType = product.ItemType,
            Description = dto.Description.Trim(),
            Quantity = dto.Quantity,
            UnitOfMeasure = dto.UnitOfMeasure.Trim(),
            EstimatedUnitPrice = unitPrice,
            PriceConfirmed = dto.EstimatedUnitPrice > 0,
            Quarter = dto.Quarter,
            RequiredDate = dto.RequiredDate,
            FundingSource = dto.FundingSource,
            Priority = dto.Priority,
            Status = "New",
            CreatedBy = userId,
            CreatedByName = userName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _unitOfWork.Context.Requirements.Add(req);
        await _unitOfWork.Context.SaveChangesAsync();

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Requerimientos",
            "Creó requerimiento",
            $"Creó el requerimiento '{req.Code}' para la tarea '{task.Name}' ruteado a '{consolidatorAreaName}'",
            "Requirement",
            req.Id);

        return HttpResult<RequirementDetailDto>.Success(MapToDetailDto(req), HttpStatusCode.Created);
    }

    public async Task<IHttpResult<RequirementDetailDto>> UpdateAsync(
        int organizationId,
        Guid id,
        UpdateRequirementDto dto,
        Guid userId,
        string? userName,
        Guid userAreaId)
    {
        var req = await _unitOfWork.Context.Requirements
            .Include(r => r.Comments)
            .Include(r => r.Decisions)
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Id == id);

        if (req is null)
        {
            return HttpResult<RequirementDetailDto>.Failure(
                "Requerimiento no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { "El requerimiento no existe." } } },
                HttpStatusCode.NotFound);
        }

        // Solo la misma área origen puede editar (salvo accesos globales/admin sin area restrictiva)
        if (userAreaId != Guid.Empty && req.OriginAreaId != userAreaId)
        {
            return HttpResult<RequirementDetailDto>.Failure(
                "Acceso denegado",
                new Dictionary<string, IEnumerable<string>> { { "area", new[] { "Solo el área origen puede modificar este requerimiento." } } },
                HttpStatusCode.Forbidden);
        }

        // Solo editable en estado New o Returned
        if (req.Status != "New" && req.Status != "Returned")
        {
            return HttpResult<RequirementDetailDto>.Failure(
                "Estado no editable",
                new Dictionary<string, IEnumerable<string>> { { "status", new[] { $"No se puede editar un requerimiento en estado '{req.Status}'." } } },
                HttpStatusCode.Conflict);
        }

        // Si cambió el artículo maestro, re-rutear y actualizar snapshots
        if (req.MasterProductId != dto.MasterProductId)
        {
            var (product, consolidatorAreaId, consolidatorAreaName, appliedRule) = await ResolveFromMasterProductAsync(organizationId, dto.MasterProductId);
            if (product is null || consolidatorAreaId == Guid.Empty)
            {
                return HttpResult<RequirementDetailDto>.Failure(
                    "Artículo no disponible",
                    new Dictionary<string, IEnumerable<string>> { { "masterProductId", new[] { "El artículo no se encuentra activo o no tiene un área consolidadora resuelta." } } },
                    HttpStatusCode.BadRequest);
            }

            req.MasterProductId = product.Id;
            req.MasterProductCode = product.Code;
            req.MasterProductName = product.Name;
            req.UnspscCode = product.UnspscCode;
            req.UnspscName = product.UnspscName;
            req.ExpenseObjectCode = product.ExpenseObjectCode;
            req.ExpenseObjectName = product.ExpenseObjectName;
            req.AccountingAccountCode = product.AccountingAccountCode;
            req.AccountingAccountName = product.AccountingAccountName;
            req.ItemType = product.ItemType;
            req.ConsolidatorAreaId = consolidatorAreaId;
            req.ConsolidatorAreaName = consolidatorAreaName;
            req.AppliedRule = appliedRule;
        }

        req.Description = dto.Description.Trim();
        req.Quantity = dto.Quantity;
        req.UnitOfMeasure = dto.UnitOfMeasure.Trim();
        if (dto.EstimatedUnitPrice > 0)
        {
            req.EstimatedUnitPrice = dto.EstimatedUnitPrice;
            req.PriceConfirmed = true;
        }
        else
        {
            req.PriceConfirmed = false;
        }

        req.Quarter = dto.Quarter;
        req.RequiredDate = dto.RequiredDate;
        req.FundingSource = dto.FundingSource;
        req.Priority = dto.Priority;
        req.ApprovedAmount = null; // Resetea aprobaciones tras edición del origen
        req.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Context.SaveChangesAsync();

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Requerimientos",
            "Actualizó requerimiento",
            $"Actualizó el requerimiento '{req.Code}'",
            "Requirement",
            req.Id);

        return HttpResult<RequirementDetailDto>.Success(MapToDetailDto(req));
    }

    public async Task<IHttpResult> DeleteAsync(
        int organizationId,
        Guid id,
        Guid userId,
        string? userName,
        Guid userAreaId)
    {
        var req = await _unitOfWork.Context.Requirements
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Id == id);

        if (req is null)
        {
            return HttpResult.Failure(
                "Requerimiento no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { "El requerimiento no existe." } } },
                HttpStatusCode.NotFound);
        }

        if (userAreaId != Guid.Empty && req.OriginAreaId != userAreaId)
        {
            return HttpResult.Failure(
                "Acceso denegado",
                new Dictionary<string, IEnumerable<string>> { { "area", new[] { "Solo el área origen puede eliminar este requerimiento." } } },
                HttpStatusCode.Forbidden);
        }

        if (req.Status != "New" && req.Status != "Returned")
        {
            return HttpResult.Failure(
                "Eliminación no permitida",
                new Dictionary<string, IEnumerable<string>> { { "status", new[] { $"No se puede eliminar un requerimiento en estado '{req.Status}'." } } },
                HttpStatusCode.Conflict);
        }

        _unitOfWork.Context.Requirements.Remove(req);
        await _unitOfWork.Context.SaveChangesAsync();

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Requerimientos",
            "Eliminó requerimiento",
            $"Eliminó el requerimiento '{req.Code}'",
            "Requirement",
            req.Id);

        return HttpResult.Success();
    }

    public async Task<IHttpResult<RequirementDetailDto>> ConfirmPriceAsync(
        int organizationId,
        Guid id,
        decimal price,
        Guid userId,
        string? userName,
        IReadOnlyCollection<Guid> userConsolidatorAreaIds)
    {
        var req = await _unitOfWork.Context.Requirements
            .Include(r => r.Comments)
            .Include(r => r.Decisions)
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Id == id);

        if (req is null)
        {
            return HttpResult<RequirementDetailDto>.Failure(
                "Requerimiento no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { "El requerimiento no existe." } } },
                HttpStatusCode.NotFound);
        }

        if (userConsolidatorAreaIds.Count > 0 && !userConsolidatorAreaIds.Contains(req.ConsolidatorAreaId))
        {
            return HttpResult<RequirementDetailDto>.Failure(
                "Acceso denegado",
                new Dictionary<string, IEnumerable<string>> { { "area", new[] { "Solo el área consolidadora asignada puede confirmar el precio." } } },
                HttpStatusCode.Forbidden);
        }

        req.EstimatedUnitPrice = price;
        req.PriceConfirmed = true;
        req.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Context.SaveChangesAsync();

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Requerimientos",
            "Confirmó precio",
            $"Confirmó el precio unitario RD$ {price:N2} para el requerimiento '{req.Code}'",
            "Requirement",
            req.Id);

        return HttpResult<RequirementDetailDto>.Success(MapToDetailDto(req));
    }

    public async Task<IHttpResult<RequirementDetailDto>> DecideAsync(
        int organizationId,
        Guid id,
        RequirementDecisionDto dto,
        Guid userId,
        string? userName,
        IReadOnlyCollection<Guid> userConsolidatorAreaIds)
    {
        var req = await _unitOfWork.Context.Requirements
            .Include(r => r.Comments)
            .Include(r => r.Decisions)
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Id == id);

        if (req is null)
        {
            return HttpResult<RequirementDetailDto>.Failure(
                "Requerimiento no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { "El requerimiento no existe." } } },
                HttpStatusCode.NotFound);
        }

        if (userConsolidatorAreaIds.Count > 0 && !userConsolidatorAreaIds.Contains(req.ConsolidatorAreaId))
        {
            return HttpResult<RequirementDetailDto>.Failure(
                "Acceso no autorizado",
                new Dictionary<string, IEnumerable<string>> { { "userAreaId", new[] { "Solo el área consolidadora asignada puede decidir sobre este requerimiento." } } },
                HttpStatusCode.Forbidden);
        }

        var requestedAmount = req.ApprovedAmount ?? (req.Quantity * req.EstimatedUnitPrice);

        if (dto.Action == "Approve")
        {
            req.Status = "Approved";
            req.ApprovedAmount = dto.ApprovedAmount ?? requestedAmount;
        }
        else if (dto.Action == "Return")
        {
            req.Status = "Returned";
        }
        else if (dto.Action == "Reschedule")
        {
            req.Status = "Rescheduled";
            if (!string.IsNullOrWhiteSpace(dto.Quarter))
                req.Quarter = dto.Quarter;
        }
        else
        {
            return HttpResult<RequirementDetailDto>.Failure(
                "Acción inválida",
                new Dictionary<string, IEnumerable<string>> { { "action", new[] { "Acción no reconocida (debe ser Approve, Return o Reschedule)." } } });
        }

        req.UpdatedAt = DateTime.UtcNow;

        var decision = new PrioritizationDecision
        {
            RequirementId = req.Id,
            RequirementCode = req.Code,
            MasterProductId = req.MasterProductId,
            Type = dto.Action,
            RequestedAmount = requestedAmount,
            ApprovedAmount = req.ApprovedAmount,
            Reason = dto.Comment,
            DecidedBy = userId,
            DecidedByName = userName,
            DecidingAreaId = req.ConsolidatorAreaId,
            Date = DateTime.UtcNow,
            Level = "Consolidator"
        };
        req.Decisions.Add(decision);

        var comment = new RequirementComment
        {
            RequirementId = req.Id,
            UserId = userId,
            UserName = userName,
            AreaName = req.ConsolidatorAreaName,
            Date = DateTime.UtcNow,
            Text = $"[Decisión: {dto.Action}] {dto.Comment}"
        };
        req.Comments.Add(comment);

        await _unitOfWork.Context.SaveChangesAsync();

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Requerimientos",
            "Registró decisión",
            $"Registró decisión '{dto.Action}' sobre el requerimiento '{req.Code}'",
            "Requirement",
            req.Id);

        return HttpResult<RequirementDetailDto>.Success(MapToDetailDto(req));
    }

    public async Task<IHttpResult<RequirementDetailDto>> ReduceAsync(
        int organizationId,
        Guid id,
        decimal approvedAmount,
        string reason,
        Guid userId,
        string? userName,
        IReadOnlyCollection<Guid> userConsolidatorAreaIds)
    {
        var req = await _unitOfWork.Context.Requirements
            .Include(r => r.Comments)
            .Include(r => r.Decisions)
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Id == id);

        if (req is null)
        {
            return HttpResult<RequirementDetailDto>.Failure(
                "Requerimiento no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { "El requerimiento no existe." } } },
                HttpStatusCode.NotFound);
        }

        if (userConsolidatorAreaIds.Count > 0 && !userConsolidatorAreaIds.Contains(req.ConsolidatorAreaId))
        {
            return HttpResult<RequirementDetailDto>.Failure(
                "Acceso no autorizado",
                new Dictionary<string, IEnumerable<string>> { { "userAreaId", new[] { "Solo el área consolidadora asignada puede reducir este requerimiento." } } },
                HttpStatusCode.Forbidden);
        }

        var requestedAmount = req.ApprovedAmount ?? (req.Quantity * req.EstimatedUnitPrice);
        req.Status = "Reduced";
        req.ApprovedAmount = approvedAmount;
        req.UpdatedAt = DateTime.UtcNow;

        var decision = new PrioritizationDecision
        {
            RequirementId = req.Id,
            RequirementCode = req.Code,
            MasterProductId = req.MasterProductId,
            Type = "Reduction",
            RequestedAmount = requestedAmount,
            ApprovedAmount = approvedAmount,
            Reason = reason,
            DecidedBy = userId,
            DecidedByName = userName,
            DecidingAreaId = req.ConsolidatorAreaId,
            Date = DateTime.UtcNow,
            Level = "Consolidator"
        };
        req.Decisions.Add(decision);

        var comment = new RequirementComment
        {
            RequirementId = req.Id,
            UserId = userId,
            UserName = userName,
            AreaName = req.ConsolidatorAreaName,
            Date = DateTime.UtcNow,
            Text = $"[Reducción de monto a RD$ {approvedAmount:N2}] {reason}"
        };
        req.Comments.Add(comment);

        await _unitOfWork.Context.SaveChangesAsync();

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Requerimientos",
            "Redujo requerimiento",
            $"Redujo el monto del requerimiento '{req.Code}' a RD$ {approvedAmount:N2}",
            "Requirement",
            req.Id);

        return HttpResult<RequirementDetailDto>.Success(MapToDetailDto(req));
    }

    public async Task<IHttpResult<RequirementDetailDto>> MergeAsync(
        int organizationId,
        MergeRequirementsDto dto,
        Guid userId,
        string? userName,
        IReadOnlyCollection<Guid> userConsolidatorAreaIds)
    {
        if (dto.RequirementIds.Count < 2)
        {
            return HttpResult<RequirementDetailDto>.Failure(
                "Fusión no válida",
                new Dictionary<string, IEnumerable<string>> { { "requirementIds", new[] { "Se requieren al menos 2 requerimientos para fusionar." } } });
        }

        // Con EnableRetryOnFailure, una transacción manual solo se permite dentro de la estrategia de
        // ejecución, que reintenta el bloque completo (begin/commit incluidos) ante fallos transitorios.
        var strategy = _unitOfWork.Context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<IHttpResult<RequirementDetailDto>>(async () =>
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var reqs = await _unitOfWork.Context.Requirements
                    .Where(r => r.OrganizationId == organizationId && dto.RequirementIds.Contains(r.Id))
                    .ToListAsync();

                if (reqs.Count != dto.RequirementIds.Count)
                {
                    await _unitOfWork.RollbackAsync();
                    return HttpResult<RequirementDetailDto>.Failure(
                        "Requerimientos no encontrados",
                        new Dictionary<string, IEnumerable<string>> { { "requirementIds", new[] { "Uno o más requerimientos especificados no existen." } } });
                }

                if (userConsolidatorAreaIds.Count > 0 && reqs.Any(r => !userConsolidatorAreaIds.Contains(r.ConsolidatorAreaId)))
                {
                    await _unitOfWork.RollbackAsync();
                    return HttpResult<RequirementDetailDto>.Failure(
                        "Acceso no autorizado",
                        new Dictionary<string, IEnumerable<string>> { { "userAreaId", new[] { "Solo el área consolidadora asignada puede fusionar estos requerimientos." } } },
                        HttpStatusCode.Forbidden);
                }

                var first = reqs[0];

                if (reqs.Any(r => r.MasterProductId != first.MasterProductId))
                {
                    await _unitOfWork.RollbackAsync();
                    return HttpResult<RequirementDetailDto>.Failure(
                        "Fusión no válida",
                        new Dictionary<string, IEnumerable<string>> { { "masterProductId", new[] { "Solo se pueden fusionar requerimientos del mismo artículo maestro." } } });
                }

                if (reqs.Any(r => r.ConsolidatorAreaId != first.ConsolidatorAreaId))
                {
                    await _unitOfWork.RollbackAsync();
                    return HttpResult<RequirementDetailDto>.Failure(
                        "Fusión no válida",
                        new Dictionary<string, IEnumerable<string>> { { "consolidatorAreaId", new[] { "Solo se pueden fusionar requerimientos de la misma área consolidadora." } } });
                }

                if (reqs.Any(r => r.CycleId != first.CycleId))
                {
                    await _unitOfWork.RollbackAsync();
                    return HttpResult<RequirementDetailDto>.Failure(
                        "Fusión no válida",
                        new Dictionary<string, IEnumerable<string>> { { "cycleId", new[] { "Solo se pueden fusionar requerimientos del mismo ciclo." } } });
                }

                if (reqs.Any(r => !string.Equals(r.UnitOfMeasure, first.UnitOfMeasure, StringComparison.OrdinalIgnoreCase)))
                {
                    await _unitOfWork.RollbackAsync();
                    return HttpResult<RequirementDetailDto>.Failure(
                        "Fusión no válida",
                        new Dictionary<string, IEnumerable<string>> { { "unitOfMeasure", new[] { "Solo se pueden fusionar requerimientos con la misma unidad de medida." } } });
                }

                if (reqs.Any(r => r.Status == "Merged" || r.MergedIntoId.HasValue))
                {
                    await _unitOfWork.RollbackAsync();
                    return HttpResult<RequirementDetailDto>.Failure(
                        "Fusión no válida",
                        new Dictionary<string, IEnumerable<string>> { { "status", new[] { "No se pueden volver a fusionar requerimientos que ya fueron fusionados." } } });
                }

                var cycle = await _unitOfWork.Context.PlanningCycles.FirstOrDefaultAsync(c => c.Id == first.CycleId);
                var year = cycle?.Year ?? DateTime.UtcNow.Year;

                var seq = await _sequenceService.NextAsync(organizationId, "REQUIREMENT", year.ToString());
                var code = $"RQ-{year}-{seq:D3}";

                var mergedQty = reqs.Sum(r => r.Quantity);
                var totalCost = reqs.Sum(r => r.Quantity * r.EstimatedUnitPrice);
                var weightedUnitPrice = mergedQty > 0 ? Math.Round(totalCost / mergedQty, 2) : first.EstimatedUnitPrice;

                var earliestRequiredDate = reqs.Where(r => r.RequiredDate.HasValue).Min(r => r.RequiredDate);
                var fundingSources = reqs.Select(r => r.FundingSource).Where(f => !string.IsNullOrWhiteSpace(f)).Distinct().ToList();
                var fundingSource = fundingSources.Count > 0 ? string.Join(", ", fundingSources) : first.FundingSource;

                var mergedReq = new Requirement
                {
                    OrganizationId = organizationId,
                    Code = code,
                    CycleId = first.CycleId,
                    PlanId = first.PlanId,
                    PoaProductId = first.PoaProductId,
                    PoaTaskId = first.PoaTaskId,
                    OriginAreaId = first.OriginAreaId,
                    OriginAreaName = first.OriginAreaName,
                    ConsolidatorAreaId = first.ConsolidatorAreaId,
                    ConsolidatorAreaName = first.ConsolidatorAreaName,
                    AppliedRule = $"Fusionado de {reqs.Count} requerimientos: {string.Join(", ", reqs.Select(r => r.Code))}",
                    MasterProductId = first.MasterProductId,
                    MasterProductCode = first.MasterProductCode,
                    MasterProductName = first.MasterProductName,
                    UnspscCode = first.UnspscCode,
                    UnspscName = first.UnspscName,
                    ExpenseObjectCode = first.ExpenseObjectCode,
                    ExpenseObjectName = first.ExpenseObjectName,
                    AccountingAccountCode = first.AccountingAccountCode,
                    AccountingAccountName = first.AccountingAccountName,
                    ItemType = first.ItemType,
                    Description = dto.NewDescription,
                    Quantity = mergedQty,
                    UnitOfMeasure = first.UnitOfMeasure,
                    EstimatedUnitPrice = weightedUnitPrice,
                    PriceConfirmed = first.PriceConfirmed,
                    Quarter = first.Quarter,
                    RequiredDate = earliestRequiredDate,
                    FundingSource = fundingSource,
                    Priority = first.Priority,
                    Status = "InReview",
                    CreatedBy = userId,
                    CreatedByName = userName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _unitOfWork.Context.Requirements.Add(mergedReq);
                await _unitOfWork.Context.SaveChangesAsync();

                var decision = new PrioritizationDecision
                {
                    RequirementId = mergedReq.Id,
                    RequirementCode = mergedReq.Code,
                    MasterProductId = mergedReq.MasterProductId,
                    Type = "Merge",
                    RequestedAmount = totalCost,
                    ApprovedAmount = null,
                    Reason = $"Fusión de requerimientos {string.Join(", ", reqs.Select(r => r.Code))}. {dto.Reason}",
                    DecidedBy = userId,
                    DecidedByName = userName,
                    DecidingAreaId = mergedReq.ConsolidatorAreaId,
                    Date = DateTime.UtcNow,
                    Level = "Consolidator"
                };
                _unitOfWork.Context.PrioritizationDecisions.Add(decision);

                foreach (var r in reqs)
                {
                    r.Status = "Merged";
                    r.MergedIntoId = mergedReq.Id;
                    r.UpdatedAt = DateTime.UtcNow;
                }

                await _unitOfWork.Context.SaveChangesAsync();

                await _activityService.LogAsync(
                    organizationId,
                    userId,
                    userName,
                    "Requerimientos",
                    "Fusionó requerimientos",
                    $"Fusionó {reqs.Count} requerimientos en el nuevo requerimiento '{mergedReq.Code}'",
                    "Requirement",
                    mergedReq.Id);

                await _unitOfWork.CommitAsync();

                var createdDetail = await GetByIdAsync(organizationId, mergedReq.Id);
                return createdDetail;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                // Descartar lo rastreado en este intento: si la estrategia reintenta, las consultas
                // deben volver a leer el estado de la BD (p. ej. requerimientos aún sin fusionar).
                _unitOfWork.Context.ChangeTracker.Clear();
                throw;
            }
        });
    }

    public async Task<IHttpResult<IEnumerable<RequirementDto>>> ListByTaskAsync(int organizationId, Guid taskId)
    {
        var reqs = await _unitOfWork.Context.Requirements
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId && r.PoaTaskId == taskId && r.Status != "Merged")
            .ToListAsync();

        return HttpResult<IEnumerable<RequirementDto>>.Success(reqs.Select(MapToDto));
    }

    public async Task<IHttpResult<IEnumerable<PoaTaskOptionDto>>> GetAvailableTasksAsync(
        int organizationId,
        Guid? cycleId,
        Guid? areaId,
        Guid? productId,
        string? search)
    {
        var query = _unitOfWork.Context.PoaTasks
            .AsNoTracking()
            .Include(t => t.Product)
                .ThenInclude(p => p!.Plan)
            .Where(t => t.Product!.Plan!.OrganizationId == organizationId);

        if (cycleId.HasValue)
            query = query.Where(t => t.Product!.Plan!.CycleId == cycleId.Value);

        if (areaId.HasValue)
            query = query.Where(t => t.Product!.Plan!.AreaId == areaId.Value);

        if (productId.HasValue)
            query = query.Where(t => t.ProductId == productId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(t =>
                EF.Functions.Like(t.Name, $"%{term}%") ||
                EF.Functions.Like(t.Code, $"%{term}%") ||
                EF.Functions.Like(t.Product!.ProductName, $"%{term}%"));
        }

        var tasks = await query
            .OrderBy(t => t.Product!.Plan!.AreaName)
            .ThenBy(t => t.Product!.ProductName)
            .ThenBy(t => t.Code)
            .Select(t => new PoaTaskOptionDto
            {
                Id = t.Id,
                Code = t.Code,
                Name = t.Name,
                ProductId = t.ProductId,
                ProductCode = t.Product!.Code,
                ProductName = t.Product!.ProductName,
                PlanId = t.Product!.PlanId,
                CycleId = t.Product!.Plan!.CycleId,
                AreaId = t.Product!.Plan!.AreaId,
                AreaName = t.Product!.Plan!.AreaName
            })
            .ToListAsync();

        return HttpResult<IEnumerable<PoaTaskOptionDto>>.Success(tasks);
    }

    public async Task<IHttpResult<RequirementCommentDto>> AddCommentAsync(
        int organizationId,
        Guid id,
        string text,
        Guid userId,
        string? userName,
        string? areaName)
    {
        var req = await _unitOfWork.Context.Requirements
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Id == id);

        if (req is null)
        {
            return HttpResult<RequirementCommentDto>.Failure(
                "Requerimiento no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { "El requerimiento no existe." } } },
                HttpStatusCode.NotFound);
        }

        var comment = new RequirementComment
        {
            RequirementId = req.Id,
            UserId = userId,
            UserName = userName,
            AreaName = areaName ?? req.ConsolidatorAreaName,
            Date = DateTime.UtcNow,
            Text = text
        };

        _unitOfWork.Context.RequirementComments.Add(comment);
        await _unitOfWork.Context.SaveChangesAsync();

        return HttpResult<RequirementCommentDto>.Success(new RequirementCommentDto
        {
            Id = comment.Id,
            RequirementId = comment.RequirementId,
            UserId = comment.UserId,
            UserName = comment.UserName,
            AreaName = comment.AreaName,
            Date = comment.Date,
            Text = comment.Text
        });
    }

    private async Task<(MasterProduct? Product, Guid ConsolidatorAreaId, string ConsolidatorAreaName, string AppliedRule)> ResolveFromMasterProductAsync(
        int organizationId,
        Guid masterProductId)
    {
        var product = await _unitOfWork.Context.MasterProducts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == masterProductId && p.DeletedAt == null);

        if (product is null)
            return (null, Guid.Empty, string.Empty, string.Empty);

        if (product.State != "Active" || !product.ConsolidatorAreaId.HasValue || product.ConsolidatorAreaId.Value == Guid.Empty)
        {
            return (product, Guid.Empty, string.Empty, "Artículo no activo o sin área consolidadora resuelta");
        }

        var areaName = product.ConsolidatorAreaName ?? "Área Consolidadora";
        var appliedRule = product.ConsolidatorAreaSource switch
        {
            "Manual" => $"Área ratificada por Planificación para '{product.Code} — {product.Name}'",
            "Rule" => $"Regla {product.ConsolidatorRuleKey ?? "AREA"} v{product.ConsolidatorRuleVersion ?? 1} → {areaName} (confianza {product.ConsolidatorAreaConfidence ?? 80}%)",
            "Imported" => $"Asignación institucional importada de la Maestra para '{product.Code} — {product.Name}'",
            _ => $"Asignación para '{product.Code} — {product.Name}'"
        };

        return (product, product.ConsolidatorAreaId.Value, areaName, appliedRule);
    }

    private static RequirementDto MapToDto(Requirement r)
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

    private static RequirementDetailDto MapToDetailDto(Requirement r)
    {
        var baseDto = MapToDto(r);
        return new RequirementDetailDto
        {
            Id = baseDto.Id,
            Code = baseDto.Code,
            CycleId = baseDto.CycleId,
            PlanId = baseDto.PlanId,
            PoaProductId = baseDto.PoaProductId,
            PoaTaskId = baseDto.PoaTaskId,
            OriginAreaId = baseDto.OriginAreaId,
            OriginAreaName = baseDto.OriginAreaName,
            ConsolidatorAreaId = baseDto.ConsolidatorAreaId,
            ConsolidatorAreaName = baseDto.ConsolidatorAreaName,
            AppliedRule = baseDto.AppliedRule,
            MasterProductId = baseDto.MasterProductId,
            MasterProductCode = baseDto.MasterProductCode,
            MasterProductName = baseDto.MasterProductName,
            UnspscCode = baseDto.UnspscCode,
            UnspscName = baseDto.UnspscName,
            ExpenseObjectCode = baseDto.ExpenseObjectCode,
            ExpenseObjectName = baseDto.ExpenseObjectName,
            AccountingAccountCode = baseDto.AccountingAccountCode,
            AccountingAccountName = baseDto.AccountingAccountName,
            ItemType = baseDto.ItemType,
            Description = baseDto.Description,
            Quantity = baseDto.Quantity,
            UnitOfMeasure = baseDto.UnitOfMeasure,
            EstimatedUnitPrice = baseDto.EstimatedUnitPrice,
            PriceConfirmed = baseDto.PriceConfirmed,
            Quarter = baseDto.Quarter,
            RequiredDate = baseDto.RequiredDate,
            FundingSource = baseDto.FundingSource,
            Priority = baseDto.Priority,
            Status = baseDto.Status,
            ApprovedAmount = baseDto.ApprovedAmount,
            MergedIntoId = baseDto.MergedIntoId,
            CreatedBy = baseDto.CreatedBy,
            CreatedByName = baseDto.CreatedByName,
            CreatedAt = baseDto.CreatedAt,
            UpdatedAt = baseDto.UpdatedAt,
            Comments = r.Comments.Select(c => new RequirementCommentDto
            {
                Id = c.Id,
                RequirementId = c.RequirementId,
                UserId = c.UserId,
                UserName = c.UserName,
                AreaName = c.AreaName,
                Date = c.Date,
                Text = c.Text
            }).ToList(),
            Decisions = r.Decisions.Select(d => new PrioritizationDecisionDto
            {
                Id = d.Id,
                RequirementId = d.RequirementId,
                RequirementCode = d.RequirementCode,
                MasterProductId = d.MasterProductId,
                Type = d.Type,
                RequestedAmount = d.RequestedAmount,
                ApprovedAmount = d.ApprovedAmount,
                Reason = d.Reason,
                DecidedBy = d.DecidedBy,
                DecidedByName = d.DecidedByName,
                DecidingAreaId = d.DecidingAreaId,
                Date = d.Date,
                Level = d.Level
            }).ToList()
        };
    }
}
