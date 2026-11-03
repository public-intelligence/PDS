using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDS.Application.Data;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.LegalInstrument;
using PDS.Domain.DTOs.StructureVersion;
using PDS.Domain.Enums;
using PDS.Domain.Models;
using PDS.Domain.Response;
using System.Net;

namespace PDS.Service.Services;

public class StructureVersionService(
    ILogger<StructureVersionService> logger,
    IMapper mapper,
    IUnitOfWork<PDSDbContext> unitOfWork,
    IRepository<StructureVersion> versionRepository,
    IRepository<StructureVersionUnit> versionUnitRepository,
    IRepository<StructureVersionAuditLog> auditRepository,
    IRepository<OrganizationalStructure> structureRepository
) : IStructureVersionService
{
    private readonly ILogger<StructureVersionService> _logger = logger;
    private readonly IMapper _mapper = mapper;
    private readonly IUnitOfWork<PDSDbContext> _uow = unitOfWork;
    private readonly IRepository<StructureVersion> _versionRepository = versionRepository;
    private readonly IRepository<StructureVersionUnit> _versionUnitRepository = versionUnitRepository;
    private readonly IRepository<StructureVersionAuditLog> _auditRepository = auditRepository;
    private readonly IRepository<OrganizationalStructure> _structureRepository = structureRepository;

    // ───────────────────────── lecturas ─────────────────────────

    public async Task<IHttpResult<IEnumerable<StructureVersionSummaryDto>>> ListAsync(int organizationId, VersionStatus? status)
    {
        var result = await _versionRepository.FilterAsync(
            v => v.OrganizationId == organizationId && (status == null || v.Status == status),
            q => q.Include(v => v.LegalInstrument).OrderByDescending(v => v.CreatedAt)
        );
        if (!result.IsSuccess)
        {
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudieron listar las versiones"] } };
            return HttpResult<IEnumerable<StructureVersionSummaryDto>>.Failure("Error al listar versiones", detail, HttpStatusCode.BadRequest);
        }

        var dtos = _mapper.Map<IEnumerable<StructureVersionSummaryDto>>(result.Value);
        return HttpResult<IEnumerable<StructureVersionSummaryDto>>.Success(dtos);
    }

    public async Task<IHttpResult<StructureVersionDetailDto>> GetAsync(int organizationId, Guid id)
    {
        var result = await _versionRepository.GetAsync(
            v => v.OrganizationId == organizationId && v.Id == id,
            q => q.Include(v => v.LegalInstrument).Include(v => v.AuditLogs)
        );
        if (!result.IsSuccess)
            return FailDetail("No se pudo recuperar la versión");
        if (result.Value is null)
            return FailDetail("La versión no existe", HttpStatusCode.NotFound);

        var dto = _mapper.Map<StructureVersionDetailDto>(result.Value);
        dto = dto with { AuditLogs = dto.AuditLogs.OrderBy(a => a.Date).ToList() };
        return HttpResult<StructureVersionDetailDto>.Success(dto);
    }

    // ───────────────────────── flujo ─────────────────────────

    public async Task<IHttpResult<StructureVersionDetailDto>> CreateDraftAsync(
        int organizationId, Guid userId, string? userName, CreateStructureVersionDto dto)
    {
        // Solo una versión en curso (Draft/InReview/Approved) por organización.
        var openResult = await _versionRepository.AnyAsync(v =>
            v.OrganizationId == organizationId
            && (v.Status == VersionStatus.Draft || v.Status == VersionStatus.InReview || v.Status == VersionStatus.Approved));
        if (openResult.IsSuccess && openResult.Value)
            return FailDetail("Ya existe una versión en curso. Publíquela o recházela antes de crear otra.", HttpStatusCode.Conflict);

        var version = new StructureVersion
        {
            OrganizationId = organizationId,
            Name = dto.Name,
            Description = dto.Description,
            Status = VersionStatus.Draft,
            CreatedBy = userId,
            CreatedByName = userName
        };

        var createResult = await _versionRepository.CreateAsync(version);
        if (!createResult.IsSuccess)
            return FailDetail("No se pudo crear el borrador de versión");

        await AddAuditAsync(version.Id, VersionAction.Created, userId, userName, dto.Description);

        return await GetAsync(organizationId, version.Id);
    }

    public async Task<IHttpResult> SubmitReviewAsync(int organizationId, Guid id, Guid userId, string? userName, string? comment)
    {
        var versionResult = await GetTrackedAsync(organizationId, id);
        if (!versionResult.IsSuccess) return FailAction(versionResult.Error, versionResult.Code);
        var version = versionResult.Value!;

        if (version.Status != VersionStatus.Draft)
            return FailAction("Solo se puede enviar a revisión una versión en borrador.");

        version.Status = VersionStatus.InReview;
        await _versionRepository.UpdateAsync(version);
        await AddAuditAsync(version.Id, VersionAction.SubmittedForReview, userId, userName, comment);
        return HttpResult.Success();
    }

    public async Task<IHttpResult> ApproveAsync(int organizationId, Guid id, Guid userId, string? userName, string? comment)
    {
        var versionResult = await GetTrackedAsync(organizationId, id);
        if (!versionResult.IsSuccess) return FailAction(versionResult.Error, versionResult.Code);
        var version = versionResult.Value!;

        if (version.Status != VersionStatus.InReview)
            return FailAction("Solo se puede aprobar una versión en revisión.");

        version.Status = VersionStatus.Approved;
        version.ReviewedBy = userId;
        version.ReviewedByName = userName;
        version.ReviewedAt = DateTime.UtcNow;
        version.ApprovedBy = userId;
        version.ApprovedByName = userName;
        version.ApprovedAt = DateTime.UtcNow;
        await _versionRepository.UpdateAsync(version);
        await AddAuditAsync(version.Id, VersionAction.Approved, userId, userName, comment);
        return HttpResult.Success();
    }

    public async Task<IHttpResult> RejectAsync(int organizationId, Guid id, Guid userId, string? userName, string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return FailAction("Debe indicar el motivo del rechazo.");

        var versionResult = await GetTrackedAsync(organizationId, id);
        if (!versionResult.IsSuccess) return FailAction(versionResult.Error, versionResult.Code);
        var version = versionResult.Value!;

        if (version.Status != VersionStatus.InReview && version.Status != VersionStatus.Approved)
            return FailAction("Solo se puede rechazar una versión en revisión o aprobada.");

        // Regresa a borrador y limpia los actores de revisión/aprobación (el histórico queda en la bitácora).
        version.Status = VersionStatus.Draft;
        version.ReviewedBy = null;
        version.ReviewedByName = null;
        version.ReviewedAt = null;
        version.ApprovedBy = null;
        version.ApprovedByName = null;
        version.ApprovedAt = null;
        await _versionRepository.UpdateAsync(version);
        await AddAuditAsync(version.Id, VersionAction.Rejected, userId, userName, comment);
        return HttpResult.Success();
    }

    // ───────────────────────── publicación ─────────────────────────

    public async Task<IHttpResult<StructureVersionSummaryDto>> PublishAsync(
        int organizationId, Guid id, Guid userId, string? userName, PublishVersionDto dto)
    {
        if (!dto.LegalInstrumentId.HasValue && dto.LegalInstrument is null)
            return FailSummary("Debe asociar un instrumento legal para publicar.");

        var ctx = _uow.Context;

        var version = await ctx.StructureVersions.FirstOrDefaultAsync(v => v.OrganizationId == organizationId && v.Id == id);
        if (version is null)
            return FailSummary("La versión no existe", HttpStatusCode.NotFound);
        if (version.Status != VersionStatus.Approved)
            return FailSummary("Solo se puede publicar una versión aprobada.");

        Guid? legalInstrumentId = dto.LegalInstrumentId;
        if (legalInstrumentId.HasValue)
        {
            var exists = await ctx.LegalInstruments.AnyAsync(li =>
                li.Id == legalInstrumentId.Value && li.OrganizationId == organizationId && li.DeletedAt == null);
            if (!exists)
                return FailSummary("El instrumento legal indicado no existe.");
        }

        // Estructura operativa (la fotografía se materializa AHORA, al publicar).
        var structureResult = await _structureRepository.FilterAsync(
            os => os.OrganizationId == organizationId,
            q => q
                .Include(os => os.AreaType)
                .Include(os => os.Positions.Where(p => p.DeletedAt == null))
                    .ThenInclude(p => p.OccupationalGroup)
        );
        if (!structureResult.IsSuccess)
            return FailSummary("No se pudo recuperar la estructura operativa.");
        var units = structureResult.Value.ToList();
        if (units.Count == 0)
            return FailSummary("No se puede publicar una estructura vacía.");

        var levels = ComputeLevels(units);
        if (levels.Count != units.Count)
            return FailSummary("La estructura contiene un ciclo jerárquico y no puede publicarse.");

        var multiSupervisor = units.FirstOrDefault(u => (u.Positions ?? []).Count(p => p.IsManager) > 1);
        if (multiSupervisor is not null)
            return FailSummary($"La unidad '{multiSupervisor.Name}' tiene más de un cargo supervisor.");

        // La estrategia de reintentos de SQL Server no permite transacciones iniciadas manualmente
        // salvo que toda la unidad (begin/commit/rollback incluidos) se ejecute dentro de
        // CreateExecutionStrategy().ExecuteAsync, que reintenta el bloque completo ante fallos transitorios.
        var strategy = ctx.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
        var beginResult = await _uow.BeginTransactionAsync();
        if (!beginResult.IsSuccess)
            return FailSummary("No se pudo iniciar la transacción de publicación.");

        try
        {
            // Crear instrumento legal en línea, si aplica.
            if (!legalInstrumentId.HasValue && dto.LegalInstrument is not null)
            {
                var li = _mapper.Map<LegalInstrument>(dto.LegalInstrument);
                li.OrganizationId = organizationId;
                ctx.LegalInstruments.Add(li);
                await ctx.SaveChangesAsync();
                legalInstrumentId = li.Id;
            }

            // Bloqueo pesimista de la versión publicada con período abierto (serializa publicaciones por org).
            var publishedStatus = VersionStatus.Published.ToString();
            var prev = (await ctx.StructureVersions
                .FromSqlInterpolated($@"SELECT * FROM [Organization].[StructureVersions] WITH (UPDLOCK, HOLDLOCK)
                                        WHERE [OrganizationId] = {organizationId} AND [Status] = {publishedStatus} AND [EffectiveTo] IS NULL")
                .ToListAsync())
                .FirstOrDefault();

            if (prev is not null)
            {
                // Reemplazo inmediato/mismo día: la nueva versión puede entrar en vigencia el mismo día
                // que la vigente, pero nunca antes. Solo se bloquean fechas estrictamente anteriores.
                if (prev.EffectiveFrom.HasValue && dto.EffectiveFrom < prev.EffectiveFrom.Value)
                {
                    await _uow.RollbackAsync();
                    return FailSummary("La fecha de vigencia no puede ser anterior a la de la versión vigente.");
                }

                // Cierre del período de la versión anterior:
                //  • fecha posterior → termina el día antes de que inicie la nueva (períodos contiguos);
                //  • mismo día       → su período colapsa a su fecha de inicio. En ese día la resolución
                //                      por fecha desempata a favor del mayor Number (la nueva versión).
                prev.EffectiveTo = prev.EffectiveFrom.HasValue && dto.EffectiveFrom == prev.EffectiveFrom.Value
                    ? prev.EffectiveFrom.Value
                    : dto.EffectiveFrom.AddDays(-1);
                prev.Status = VersionStatus.Superseded;

                // Persistir el cierre de la versión anterior ANTES de abrir la nueva. El índice único
                // UX_StructureVersions_OneOpenPublishedPerOrg (Status='Published' AND EffectiveTo IS NULL)
                // se evalúa por sentencia: si EF escribiera la nueva versión (Published, EffectiveTo NULL)
                // antes de cerrar la anterior, coexistirían dos versiones vigentes y SQL Server abortaría
                // la transacción. Guardamos dentro de la misma transacción para mantener la atomicidad.
                await ctx.SaveChangesAsync();
            }

            var maxNumber = await ctx.StructureVersions
                .Where(v => v.OrganizationId == organizationId && v.Number != null)
                .MaxAsync(v => (int?)v.Number) ?? 0;
            var newNumber = maxNumber + 1;

            // Arrastre de plazas autorizadas desde la versión anterior + overrides de la solicitud.
            var carry = new Dictionary<Guid, int>();
            if (prev is not null)
            {
                var prevPositions = await ctx.StructureVersionPositions
                    .Where(p => p.StructureVersionId == prev.Id).ToListAsync();
                foreach (var p in prevPositions) carry[p.PositionId] = p.AuthorizedPosts;
            }
            var overrides = dto.AuthorizedPosts ?? [];

            var snapshotUnits = units.Select(os => new StructureVersionUnit
            {
                StructureVersionId = version.Id,
                SourceOrganizationalStructureId = os.Id,
                Code = os.Code,
                Name = os.Name,
                AreaTypeId = os.AreaTypeId,
                AreaTypeName = os.AreaType?.Name,
                Level = levels.TryGetValue(os.Id, out var lvl) ? lvl : 1,
                ParentSourceUnitId = os.ParentId
            }).ToList();
            ctx.StructureVersionUnits.AddRange(snapshotUnits);

            var snapshotPositions = new List<StructureVersionPosition>();
            foreach (var os in units)
            {
                foreach (var p in os.Positions ?? [])
                {
                    var authorized = overrides.TryGetValue(p.Id, out var ov)
                        ? ov
                        : carry.TryGetValue(p.Id, out var cv) ? cv : 1;
                    snapshotPositions.Add(new StructureVersionPosition
                    {
                        StructureVersionId = version.Id,
                        SourceOrganizationalStructureId = os.Id,
                        PositionId = p.Id,
                        PositionName = p.Name,
                        AuthorizedPosts = authorized,
                        IsSupervisor = p.IsManager,
                        OccupationalGroupId = p.OccupationalGroupId,
                        OccupationalGroupName = p.OccupationalGroup?.Name,
                        // RF-RE-03/04: fotografía de salario/grado para costo estimado fiel a la versión.
                        Grade = p.Grade,
                        MinSalary = p.MinSalary,
                        MaxSalary = p.MaxSalary
                    });
                }
            }
            ctx.StructureVersionPositions.AddRange(snapshotPositions);

            version.Status = VersionStatus.Published;
            version.Number = newNumber;
            version.PublishedBy = userId;
            version.PublishedByName = userName;
            version.PublishedAt = DateTime.UtcNow;
            version.EffectiveFrom = dto.EffectiveFrom;
            version.EffectiveTo = dto.EffectiveTo;
            version.IsPublic = dto.IsPublic;
            version.LegalInstrumentId = legalInstrumentId;

            ctx.StructureVersionAuditLogs.Add(new StructureVersionAuditLog
            {
                StructureVersionId = version.Id,
                Action = VersionAction.Published,
                UserId = userId,
                UserName = userName,
                Date = DateTime.UtcNow
            });
            if (prev is not null)
            {
                ctx.StructureVersionAuditLogs.Add(new StructureVersionAuditLog
                {
                    StructureVersionId = prev.Id,
                    Action = VersionAction.Superseded,
                    UserId = userId,
                    UserName = userName,
                    Date = DateTime.UtcNow,
                    Comment = $"Reemplazada por la versión #{newNumber}"
                });
            }

            await ctx.SaveChangesAsync();
            await _uow.CommitAsync();

            var summary = _mapper.Map<StructureVersionSummaryDto>(version);
            if (legalInstrumentId.HasValue)
            {
                var li = await ctx.LegalInstruments.FirstOrDefaultAsync(l => l.Id == legalInstrumentId.Value);
                summary = summary with { LegalInstrument = li is null ? null : _mapper.Map<LegalInstrumentDto>(li) };
            }
            return HttpResult<StructureVersionSummaryDto>.Success(summary);
        }
        catch (DbUpdateException ex)
        {
            await _uow.RollbackAsync();
            _logger.LogError(ex, "Error de base de datos al publicar la versión {VersionId}", id);

            // El nombre del índice/constraint vulnerado viaja en el mensaje del error de SQL Server.
            // Lo traducimos a un mensaje específico en lugar de un genérico ambiguo.
            var dbMessage = ex.InnerException?.Message ?? ex.Message;
            var message = dbMessage.Contains("UX_StructureVersions_OneOpenPublishedPerOrg")
                ? "Ya existe una versión vigente con período abierto. Cierre o reemplace la versión vigente antes de publicar."
                : dbMessage.Contains("UX_StructureVersions_NumberPerOrg")
                    ? "El número de versión ya existe para la organización."
                    : dbMessage.Contains("UX_StructureVersions_OneDraftPerOrg")
                        ? "Ya existe un borrador para la organización."
                        : "No se pudo publicar la versión por un conflicto de datos. Intente nuevamente.";
            return FailSummary(message, HttpStatusCode.Conflict);
        }
        catch (Exception ex)
        {
            await _uow.RollbackAsync();
            _logger.LogError(ex, "Error al publicar la versión {VersionId}", id);
            return FailSummary("Ocurrió un error al publicar la versión.");
        }
        });
    }

    // ───────────────────────── comparación (RF-VH-03) ─────────────────────────

    public async Task<IHttpResult<VersionComparisonDto>> CompareAsync(
        int organizationId, Guid? fromVersionId, DateOnly? fromDate, Guid? toVersionId, DateOnly? toDate)
    {
        var fromResult = await ResolveComparableVersionAsync(organizationId, fromVersionId, fromDate);
        if (!fromResult.IsSuccess) return FailCompare(fromResult.Error);
        var toResult = await ResolveComparableVersionAsync(organizationId, toVersionId, toDate);
        if (!toResult.IsSuccess) return FailCompare(toResult.Error);

        var fromVersion = fromResult.Value;
        var toVersion = toResult.Value;
        if (fromVersion is null || toVersion is null)
            return FailCompare("No se pudieron resolver ambas versiones a comparar.", HttpStatusCode.NotFound);

        var fromUnits = (await _versionUnitRepository.FilterAsync(u => u.StructureVersionId == fromVersion.Id)).Value.ToList();
        var toUnits = (await _versionUnitRepository.FilterAsync(u => u.StructureVersionId == toVersion.Id)).Value.ToList();
        if (fromUnits.Count == 0 || toUnits.Count == 0)
            return FailCompare("Ambas versiones deben estar publicadas (con fotografía) para compararse.");

        var fromMap = fromUnits.ToDictionary(u => u.SourceOrganizationalStructureId);
        var toMap = toUnits.ToDictionary(u => u.SourceOrganizationalStructureId);

        var changes = new List<UnitChangeDto>();
        int created = 0, deleted = 0, modified = 0, relocated = 0;

        foreach (var (key, unit) in toMap)
        {
            if (fromMap.ContainsKey(key)) continue;
            created++;
            changes.Add(new UnitChangeDto
            {
                SourceUnitId = key,
                ChangeType = ["Created"],
                After = ToSnapshotDto(unit),
                ToParentSourceUnitId = unit.ParentSourceUnitId
            });
        }

        foreach (var (key, unit) in fromMap)
        {
            if (toMap.ContainsKey(key)) continue;
            deleted++;
            changes.Add(new UnitChangeDto
            {
                SourceUnitId = key,
                ChangeType = ["Deleted"],
                Before = ToSnapshotDto(unit),
                FromParentSourceUnitId = unit.ParentSourceUnitId
            });
        }

        foreach (var (key, from) in fromMap)
        {
            if (!toMap.TryGetValue(key, out var to)) continue;

            var flags = new List<string>();
            var fieldChanges = new List<FieldChangeDto>();

            if (!string.Equals(from.Name, to.Name, StringComparison.Ordinal))
                fieldChanges.Add(new FieldChangeDto { Field = "Name", From = from.Name, To = to.Name });
            if (!string.Equals(from.Code, to.Code, StringComparison.Ordinal))
                fieldChanges.Add(new FieldChangeDto { Field = "Code", From = from.Code, To = to.Code });
            if (from.AreaTypeId != to.AreaTypeId)
                fieldChanges.Add(new FieldChangeDto { Field = "AreaType", From = from.AreaTypeName, To = to.AreaTypeName });

            if (fieldChanges.Count > 0) { flags.Add("Modified"); modified++; }

            var isRelocated = from.ParentSourceUnitId != to.ParentSourceUnitId;
            if (isRelocated) { flags.Add("Relocated"); relocated++; }

            if (flags.Count == 0) continue;

            changes.Add(new UnitChangeDto
            {
                SourceUnitId = key,
                ChangeType = flags,
                Before = ToSnapshotDto(from),
                After = ToSnapshotDto(to),
                FieldChanges = fieldChanges,
                FromParentSourceUnitId = isRelocated ? from.ParentSourceUnitId : null,
                ToParentSourceUnitId = isRelocated ? to.ParentSourceUnitId : null
            });
        }

        var dto = new VersionComparisonDto
        {
            FromVersion = _mapper.Map<StructureVersionSummaryDto>(fromVersion),
            ToVersion = _mapper.Map<StructureVersionSummaryDto>(toVersion),
            Summary = new ComparisonSummaryDto
            {
                CreatedCount = created,
                DeletedCount = deleted,
                ModifiedCount = modified,
                RelocatedCount = relocated
            },
            UnitChanges = changes
        };
        return HttpResult<VersionComparisonDto>.Success(dto);
    }

    // ───────────────────────── helpers ─────────────────────────

    private async Task<TrackedVersionResult> GetTrackedAsync(int organizationId, Guid id)
    {
        var version = await _uow.Context.StructureVersions
            .FirstOrDefaultAsync(v => v.OrganizationId == organizationId && v.Id == id);
        return version is null
            ? new TrackedVersionResult(false, null, "La versión no existe", HttpStatusCode.NotFound)
            : new TrackedVersionResult(true, version, string.Empty, HttpStatusCode.OK);
    }

    private async Task<IResult<StructureVersion?>> ResolveComparableVersionAsync(
        int organizationId, Guid? versionId, DateOnly? date)
    {
        if (versionId.HasValue)
        {
            var result = await _versionRepository.GetAsync(
                v => v.OrganizationId == organizationId && v.Id == versionId.Value,
                q => q.Include(v => v.LegalInstrument));
            if (!result.IsSuccess) return Result<StructureVersion?>.Failure(result.Error);
            return Result<StructureVersion?>.Success(result.Value);
        }

        if (date.HasValue)
        {
            var result = await _versionRepository.FilterAsync(
                v => v.OrganizationId == organizationId
                    && (v.Status == VersionStatus.Published || v.Status == VersionStatus.Superseded)
                    && v.EffectiveFrom != null && v.EffectiveFrom <= date.Value
                    && (v.EffectiveTo == null || v.EffectiveTo >= date.Value),
                q => q.Include(v => v.LegalInstrument)
                    .OrderByDescending(v => v.EffectiveFrom)
                    .ThenByDescending(v => v.Number));
            if (!result.IsSuccess) return Result<StructureVersion?>.Failure(result.Error);
            return Result<StructureVersion?>.Success(result.Value.FirstOrDefault());
        }

        return Result<StructureVersion?>.Failure("Indique una versión o una fecha para cada lado de la comparación.");
    }

    private async Task AddAuditAsync(Guid versionId, VersionAction action, Guid userId, string? userName, string? comment)
    {
        await _auditRepository.CreateAsync(new StructureVersionAuditLog
        {
            StructureVersionId = versionId,
            Action = action,
            UserId = userId,
            UserName = userName,
            Date = DateTime.UtcNow,
            Comment = comment
        });
    }

    /// <summary>Calcula la profundidad (raíz = 1) por BFS; las raíces son las unidades sin padre o cuyo padre no existe.</summary>
    private static Dictionary<Guid, int> ComputeLevels(List<OrganizationalStructure> units)
    {
        var childrenByParent = units.Where(u => u.ParentId.HasValue).ToLookup(u => u.ParentId!.Value);
        var levels = new Dictionary<Guid, int>();
        var queue = new Queue<(Guid id, int level)>();

        foreach (var root in units.Where(u => u.ParentId == null || units.All(p => p.Id != u.ParentId)))
            queue.Enqueue((root.Id, 1));

        while (queue.Count > 0)
        {
            var (id, level) = queue.Dequeue();
            if (!levels.TryAdd(id, level))
                continue;
            foreach (var child in childrenByParent[id])
                queue.Enqueue((child.Id, level + 1));
        }

        return levels;
    }

    private static UnitSnapshotDto ToSnapshotDto(StructureVersionUnit u) => new()
    {
        SourceUnitId = u.SourceOrganizationalStructureId,
        Code = u.Code,
        Name = u.Name,
        AreaTypeName = u.AreaTypeName,
        Level = u.Level,
        ParentSourceUnitId = u.ParentSourceUnitId
    };

    private sealed record TrackedVersionResult(bool IsSuccess, StructureVersion? Value, string Error, HttpStatusCode Code);

    private static HttpResult<StructureVersionDetailDto> FailDetail(string message, HttpStatusCode code = HttpStatusCode.BadRequest)
        => HttpResult<StructureVersionDetailDto>.Failure("Error con la versión",
            new Dictionary<string, IEnumerable<string>> { { "errors", [message] } }, code);

    private static HttpResult<StructureVersionSummaryDto> FailSummary(string message, HttpStatusCode code = HttpStatusCode.BadRequest)
        => HttpResult<StructureVersionSummaryDto>.Failure("Error con la versión",
            new Dictionary<string, IEnumerable<string>> { { "errors", [message] } }, code);

    private static HttpResult<VersionComparisonDto> FailCompare(string message, HttpStatusCode code = HttpStatusCode.BadRequest)
        => HttpResult<VersionComparisonDto>.Failure("Error al comparar versiones",
            new Dictionary<string, IEnumerable<string>> { { "errors", [message] } }, code);

    private static HttpResult FailAction(string message, HttpStatusCode code = HttpStatusCode.BadRequest)
        => HttpResult.Failure("No se pudo procesar la acción",
            new Dictionary<string, IEnumerable<string>> { { "errors", [message] } }, code);
}
