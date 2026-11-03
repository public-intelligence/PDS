using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.OrgChart;
using PDS.Domain.DTOs.StructureVersion;
using PDS.Domain.Enums;
using PDS.Domain.Models;
using PDS.Domain.Response;
using System.Net;

namespace PDS.Service.Services;

public class OrgChartService(
    ILogger<OrgChartService> logger,
    IMapper mapper,
    IRepository<StructureVersion> versionRepository,
    IRepository<StructureVersionUnit> versionUnitRepository,
    IRepository<StructureVersionPosition> versionPositionRepository,
    IRepository<OrganizationalStructure> structureRepository,
    IRepository<ConsolidatorAreaProfile> consolidatorProfileRepository
) : IOrgChartService
{
    private readonly ILogger<OrgChartService> _logger = logger;
    private readonly IMapper _mapper = mapper;
    private readonly IRepository<StructureVersion> _versionRepository = versionRepository;
    private readonly IRepository<StructureVersionUnit> _versionUnitRepository = versionUnitRepository;
    private readonly IRepository<StructureVersionPosition> _versionPositionRepository = versionPositionRepository;
    private readonly IRepository<OrganizationalStructure> _structureRepository = structureRepository;
    private readonly IRepository<ConsolidatorAreaProfile> _consolidatorProfileRepository = consolidatorProfileRepository;

    /// <summary>
    /// Unidades marcadas como área consolidadora. Un perfil sin OrganizationalStructureId no
    /// marca nada: no hay forma de saber a qué unidad del organigrama corresponde.
    /// </summary>
    private async Task<HashSet<Guid>> GetConsolidatorUnitIdsAsync(int organizationId)
    {
        var result = await _consolidatorProfileRepository.FilterAsync(
            p => p.OrganizationId == organizationId
                && p.DeletedAt == null
                && p.OrganizationalStructureId != null);

        if (!result.IsSuccess || result.Value is null) return [];

        return [.. result.Value
            .Where(p => p.OrganizationalStructureId.HasValue)
            .Select(p => p.OrganizationalStructureId!.Value)];
    }

    public async Task<IHttpResult<OrgChartDto>> GetCurrentAsync(
        int organizationId, Guid? rootUnitId, int? maxLevel, bool includePositions, bool requirePublic)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await GetAsOfAsync(organizationId, today, rootUnitId, maxLevel, includePositions, requirePublic);
    }

    public async Task<IHttpResult<OrgChartDto>> GetAsOfAsync(
        int organizationId, DateOnly date, Guid? rootUnitId, int? maxLevel, bool includePositions, bool requirePublic)
    {
        // Una versión vigente en una fecha pasada ya puede estar Superseded: se acepta cualquier
        // versión que haya sido publicada (Published o Superseded) cuyo período contenga la fecha.
        var result = await _versionRepository.FilterAsync(
            v => v.OrganizationId == organizationId
                && (v.Status == VersionStatus.Published || v.Status == VersionStatus.Superseded)
                && (!requirePublic || v.IsPublic)
                && v.EffectiveFrom != null && v.EffectiveFrom <= date
                && (v.EffectiveTo == null || v.EffectiveTo >= date),
            q => q.Include(v => v.LegalInstrument)
                .OrderByDescending(v => v.EffectiveFrom)
                .ThenByDescending(v => v.Number)
        );
        if (!result.IsSuccess)
            return Fail("Error al obtener el organigrama", "No se pudo resolver la versión vigente para la fecha");

        var version = result.Value.FirstOrDefault();
        if (version is null)
            return Fail("Organigrama no disponible", $"No existe una versión vigente para la fecha {date:yyyy-MM-dd}", HttpStatusCode.NotFound);

        return await BuildChartAsync(organizationId, version, rootUnitId, maxLevel, includePositions);
    }

    public async Task<IHttpResult<OrgChartDto>> GetByVersionIdAsync(
        int organizationId, Guid versionId, Guid? rootUnitId, int? maxLevel, bool includePositions, bool requirePublic)
    {
        var versionResult = await _versionRepository.GetAsync(
            v => v.OrganizationId == organizationId && v.Id == versionId,
            q => q.Include(v => v.LegalInstrument)
        );
        if (!versionResult.IsSuccess)
            return Fail("Error al obtener el organigrama", "No se pudo recuperar la versión");

        var version = versionResult.Value;
        if (version is null)
            return Fail("Versión no encontrada", "La versión solicitada no existe", HttpStatusCode.NotFound);

        if (requirePublic && !(IsSnapshotStatus(version.Status) && version.IsPublic))
            return Fail("Versión no disponible", "La versión solicitada no está disponible públicamente", HttpStatusCode.NotFound);

        return await BuildChartAsync(organizationId, version, rootUnitId, maxLevel, includePositions);
    }

    public async Task<IHttpResult<OrgChartDto>> GetByNumberAsync(
        int organizationId, int number, Guid? rootUnitId, int? maxLevel, bool includePositions, bool requirePublic)
    {
        var versionResult = await _versionRepository.GetAsync(
            v => v.OrganizationId == organizationId && v.Number == number
                && (v.Status == VersionStatus.Published || v.Status == VersionStatus.Superseded)
                && (!requirePublic || v.IsPublic),
            q => q.Include(v => v.LegalInstrument)
        );
        if (!versionResult.IsSuccess)
            return Fail("Error al obtener el organigrama", "No se pudo recuperar la versión");

        var version = versionResult.Value;
        if (version is null)
            return Fail("Versión no encontrada", $"No existe una versión publicada con número {number}", HttpStatusCode.NotFound);

        return await BuildChartAsync(organizationId, version, rootUnitId, maxLevel, includePositions);
    }

    public async Task<IHttpResult<IEnumerable<StructureVersionSummaryDto>>> ListPublicVersionsAsync(int organizationId)
    {
        var result = await _versionRepository.FilterAsync(
            v => v.OrganizationId == organizationId && v.IsPublic
                && (v.Status == VersionStatus.Published || v.Status == VersionStatus.Superseded),
            q => q.Include(v => v.LegalInstrument).OrderByDescending(v => v.Number)
        );
        if (!result.IsSuccess)
        {
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudieron listar las versiones públicas"] } };
            return HttpResult<IEnumerable<StructureVersionSummaryDto>>.Failure("Error al listar versiones", detail, HttpStatusCode.BadRequest);
        }

        var dtos = _mapper.Map<IEnumerable<StructureVersionSummaryDto>>(result.Value);
        return HttpResult<IEnumerable<StructureVersionSummaryDto>>.Success(dtos);
    }

    // ───────────────────────── helpers ─────────────────────────

    private async Task<IHttpResult<OrgChartDto>> BuildChartAsync(
        int organizationId, StructureVersion version, Guid? rootUnitId, int? maxLevel, bool includePositions)
    {
        var nodesResult = await BuildNodesAsync(organizationId, version, includePositions);
        if (!nodesResult.IsSuccess)
            return Fail("Error al construir el organigrama", "No se pudieron construir los nodos del organigrama");

        var nodes = ApplyFilters(nodesResult.Value, rootUnitId, maxLevel);
        var dto = new OrgChartDto
        {
            Version = _mapper.Map<StructureVersionSummaryDto>(version),
            Nodes = nodes
        };
        return HttpResult<OrgChartDto>.Success(dto);
    }

    /// <summary>
    /// Versión publicada/superseded → lee la fotografía inmutable. Versión en curso → arma la lista
    /// a partir de la estructura operativa en vivo (vista previa).
    /// </summary>
    private async Task<IResult<List<OrgChartNodeDto>>> BuildNodesAsync(
        int organizationId, StructureVersion version, bool includePositions)
    {
        if (IsSnapshotStatus(version.Status))
        {
            var unitsResult = await _versionUnitRepository.FilterAsync(u => u.StructureVersionId == version.Id);
            if (!unitsResult.IsSuccess)
                return Result<List<OrgChartNodeDto>>.Failure(unitsResult.Error);

            List<StructureVersionPosition> positions = [];
            if (includePositions)
            {
                var posResult = await _versionPositionRepository.FilterAsync(p => p.StructureVersionId == version.Id);
                if (posResult.IsSuccess) positions = posResult.Value.ToList();
            }

            // La marca de consolidadora se resuelve contra el catálogo vigente, incluso sobre
            // una versión publicada: refleja quién consolida hoy, no en la fecha del corte.
            var snapshotConsolidatorIds = await GetConsolidatorUnitIdsAsync(organizationId);

            var nodes = unitsResult.Value.Select(u => new OrgChartNodeDto
            {
                SourceUnitId = u.SourceOrganizationalStructureId,
                ParentSourceUnitId = u.ParentSourceUnitId,
                Code = u.Code,
                Name = u.Name,
                AreaTypeId = u.AreaTypeId,
                AreaTypeName = u.AreaTypeName,
                Level = u.Level,
                IsConsolidatorArea = snapshotConsolidatorIds.Contains(u.SourceOrganizationalStructureId),
                Positions = positions
                    .Where(p => p.SourceOrganizationalStructureId == u.SourceOrganizationalStructureId)
                    .Select(MapSnapshotPosition).ToList()
            }).ToList();

            return Result<List<OrgChartNodeDto>>.Success(nodes);
        }

        return await BuildLiveNodesAsync(organizationId, includePositions);
    }

    private async Task<IResult<List<OrgChartNodeDto>>> BuildLiveNodesAsync(int organizationId, bool includePositions)
    {
        var result = await _structureRepository.FilterAsync(
            os => os.OrganizationId == organizationId,
            q => q
                .Include(os => os.AreaType)
                .Include(os => os.Positions.Where(p => p.DeletedAt == null))
                    .ThenInclude(p => p.OccupationalGroup)
        );
        if (!result.IsSuccess)
            return Result<List<OrgChartNodeDto>>.Failure(result.Error);

        var units = result.Value.ToList();
        var levels = ComputeLevels(units);
        var consolidatorIds = await GetConsolidatorUnitIdsAsync(organizationId);

        var nodes = units.Select(os => new OrgChartNodeDto
        {
            SourceUnitId = os.Id,
            ParentSourceUnitId = os.ParentId,
            Code = os.Code,
            Name = os.Name,
            AreaTypeId = os.AreaTypeId,
            AreaTypeName = os.AreaType?.Name,
            Level = levels.TryGetValue(os.Id, out var lvl) ? lvl : 1,
            IsConsolidatorArea = consolidatorIds.Contains(os.Id),
            // Las plazas autorizadas son atributo de la fotografía: en vista previa (no publicada) se
            // muestran como 1 por defecto; el valor autoritativo se fija al publicar.
            Positions = includePositions
                ? (os.Positions ?? []).Select(p => new OrgChartPositionDto
                {
                    PositionId = p.Id,
                    Name = p.Name,
                    IsSupervisor = p.IsManager,
                    AuthorizedPosts = 1,
                    OccupationalGroupName = p.OccupationalGroup?.Name
                }).ToList()
                : []
        }).ToList();

        return Result<List<OrgChartNodeDto>>.Success(nodes);
    }

    /// <summary>Calcula la profundidad (raíz = 1) por BFS desde las raíces; evita ciclos con un set visitado.</summary>
    private static Dictionary<Guid, int> ComputeLevels(List<OrganizationalStructure> units)
    {
        var childrenByParent = units.Where(u => u.ParentId.HasValue).ToLookup(u => u.ParentId!.Value);
        var levels = new Dictionary<Guid, int>();
        var queue = new Queue<(Guid id, int level)>();

        foreach (var root in units.Where(u => u.ParentId == null || !units.Any(p => p.Id == u.ParentId)))
            queue.Enqueue((root.Id, 1));

        while (queue.Count > 0)
        {
            var (id, level) = queue.Dequeue();
            if (!levels.TryAdd(id, level))
                continue; // ya visitado → corta ciclos.
            foreach (var child in childrenByParent[id])
                queue.Enqueue((child.Id, level + 1));
        }

        return levels;
    }

    /// <summary>
    /// Aplica el filtro de unidad raíz (subárbol) y de profundidad máxima (relativa a la raíz),
    /// y normaliza padres colgantes a NULL para que el árbol siempre sea reconstruible.
    /// </summary>
    private static List<OrgChartNodeDto> ApplyFilters(List<OrgChartNodeDto> nodes, Guid? rootUnitId, int? maxLevel)
    {
        var working = nodes;

        if (rootUnitId.HasValue)
        {
            var root = working.FirstOrDefault(n => n.SourceUnitId == rootUnitId.Value);
            if (root is null)
                return [];

            var childrenByParent = working.Where(n => n.ParentSourceUnitId.HasValue)
                .ToLookup(n => n.ParentSourceUnitId!.Value);

            var collected = new List<OrgChartNodeDto>();
            var visited = new HashSet<Guid>();
            var queue = new Queue<OrgChartNodeDto>();
            queue.Enqueue(root);
            var rootBaseLevel = root.Level;

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (!visited.Add(node.SourceUnitId))
                    continue;

                var rebasedLevel = node.Level - rootBaseLevel + 1;
                collected.Add(node with
                {
                    Level = rebasedLevel,
                    ParentSourceUnitId = node.SourceUnitId == root.SourceUnitId ? null : node.ParentSourceUnitId
                });

                foreach (var child in childrenByParent[node.SourceUnitId])
                    queue.Enqueue(child);
            }

            working = collected;
        }

        if (maxLevel.HasValue)
            working = working.Where(n => n.Level <= maxLevel.Value).ToList();

        // Normaliza padres ausentes (recortados por filtros / huérfanos) a NULL.
        var presentIds = working.Select(n => n.SourceUnitId).ToHashSet();
        return working
            .Select(n => n.ParentSourceUnitId.HasValue && !presentIds.Contains(n.ParentSourceUnitId.Value)
                ? n with { ParentSourceUnitId = null }
                : n)
            .ToList();
    }

    private static OrgChartPositionDto MapSnapshotPosition(StructureVersionPosition p) => new()
    {
        PositionId = p.PositionId,
        Name = p.PositionName,
        IsSupervisor = p.IsSupervisor,
        AuthorizedPosts = p.AuthorizedPosts,
        OccupationalGroupName = p.OccupationalGroupName
    };

    private static bool IsSnapshotStatus(VersionStatus status)
        => status == VersionStatus.Published || status == VersionStatus.Superseded;

    private static HttpResult<OrgChartDto> Fail(string title, string message, HttpStatusCode code = HttpStatusCode.BadRequest)
    {
        var detail = new Dictionary<string, IEnumerable<string>> { { "errors", [message] } };
        return HttpResult<OrgChartDto>.Failure(title, detail, code);
    }
}
