using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Models;
using PDS.Domain.Response;
using PDS.Service.Authorization;
using PDS.Service.Settings;

namespace PDS.Service.Services.Planning;

/// <summary>
/// Resolución del contexto: personnel_id → GOBID Personnel (positionId) → Position local →
/// OrganizationalStructure → ascenso por ParentId hasta la unidad de nivel 1 (la que formula
/// plan propio; la raíz no tiene padre). PDS es dueño del organigrama: GOBID solo aporta el cargo.
/// </summary>
public sealed class PlanningContextService : IPlanningContextService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);
    private const int MaxHierarchyDepth = 20;

    private readonly IGobidPersonnelClient _personnelClient;
    private readonly IRepository<Position> _positionRepository;
    private readonly IRepository<OrganizationalStructure> _structureRepository;
    private readonly IRepository<ConsolidatorAreaProfile> _consolidatorProfileRepository;
    private readonly IPlanningConfigService _configService;
    private readonly IMemoryCache _cache;
    private readonly PlanningDevOverridesSettings _devOverrides;
    private readonly ILogger<PlanningContextService> _logger;

    public PlanningContextService(
        IGobidPersonnelClient personnelClient,
        IRepository<Position> positionRepository,
        IRepository<OrganizationalStructure> structureRepository,
        IRepository<ConsolidatorAreaProfile> consolidatorProfileRepository,
        IPlanningConfigService configService,
        IMemoryCache cache,
        IOptions<PlanningDevOverridesSettings> devOverrides,
        ILogger<PlanningContextService> logger)
    {
        _personnelClient = personnelClient;
        _positionRepository = positionRepository;
        _structureRepository = structureRepository;
        _consolidatorProfileRepository = consolidatorProfileRepository;
        _configService = configService;
        _cache = cache;
        _devOverrides = devOverrides.Value;
        _logger = logger;
    }

    public async Task<IHttpResult<PlanningContextDto>> GetContextAsync(
        int organizationId,
        Guid userId,
        string? userName,
        string? personnelId,
        IReadOnlyCollection<string>? tokenPermissions,
        CancellationToken ct = default)
    {
        var cacheKey = CacheKey(organizationId, userId, personnelId);
        if (_cache.TryGetValue<PlanningContextDto>(cacheKey, out var cached) && cached is not null)
        {
            if (tokenPermissions is { Count: > 0 })
            {
                var missing = tokenPermissions.Where(p => !cached.Permissions.Contains(p, StringComparer.OrdinalIgnoreCase)).ToList();
                if (missing.Count > 0)
                {
                    var updated = new HashSet<string>(cached.Permissions, StringComparer.OrdinalIgnoreCase);
                    foreach (var p in tokenPermissions) updated.Add(p);
                    cached = cached with { Permissions = updated.ToArray() };
                    _cache.Set(cacheKey, cached, CacheTtl);
                }
            }
            return HttpResult<PlanningContextDto>.Success(cached);
        }

        var context = await BuildContextAsync(organizationId, userId, userName, personnelId, tokenPermissions, ct);

        _cache.Set(cacheKey, context, CacheTtl);
        return HttpResult<PlanningContextDto>.Success(context);
    }

    public void InvalidateContext(int organizationId, Guid userId, string? personnelId = null)
    {
        _cache.Remove(CacheKey(organizationId, userId, personnelId));
        _cache.Remove(CacheKey(organizationId, userId, null));
    }

    private async Task<PlanningContextDto> BuildContextAsync(
        int organizationId,
        Guid userId,
        string? userName,
        string? personnelId,
        IReadOnlyCollection<string>? tokenPermissions,
        CancellationToken ct)
    {
        var permissions = new HashSet<string>(tokenPermissions ?? [], StringComparer.OrdinalIgnoreCase);

        var devOverride = _devOverrides.Enabled
            ? _devOverrides.Users.FirstOrDefault(u => u.UserId == userId)
            : null;

        if (devOverride?.GrantAllPermissions == true)
        {
            foreach (var permission in PlanningPermissions.All)
                permissions.Add(permission);
        }

        Position? position = null;
        OrganizationalStructure? unit = null;
        OrganizationalStructure? planArea = null;
        IReadOnlyList<OrganizationalStructure> chain = [];
        string? positionNameFromGobid = null;

        if (!string.IsNullOrWhiteSpace(personnelId))
        {
            var personnelResult = await _personnelClient.GetPersonnelAsync(personnelId!, ct);
            var personnel = personnelResult.IsSuccess ? personnelResult.Value : null;

            if (personnel is null)
            {
                _logger.LogWarning(
                    "No se pudo resolver el personal {PersonnelId} en GOBID; el contexto queda sin área.", personnelId);
            }

            // Vía preferente: la unidad que GOBID adjunta al cargo. Su id sí coincide con el
            // organigrama de PDS, mientras que el positionId de GOBID puede no existir en la
            // tabla Positions local — usarlo como única fuente dejaba al usuario sin área.
            Guid? unitId = Guid.TryParse(personnel?.OrganizationalStructureId, out var structureId) && structureId != Guid.Empty
                ? structureId
                : null;

            positionNameFromGobid = personnel?.PositionName;
            position = personnel is null ? null : await ResolvePositionAsync(personnel, organizationId);

            // Respaldo: el cargo local, para organizaciones cuyo organigrama sí tiene los cargos.
            unitId ??= position?.OrganizationalStructureId;

            if (unitId is { } resolvedUnitId)
            {
                (unit, planArea, chain) = await ResolveAreasAsync(organizationId, resolvedUnitId, ct);
            }
        }
        else
        {
            _logger.LogInformation(
                "El token del usuario {UserId} no trae personnel_id/person_id; el contexto queda sin área.", userId);
        }

        // El override de área se aplica al final: permite simular ser un área concreta en pruebas.
        var planAreaId = devOverride?.PlanAreaId ?? planArea?.Id;
        var planAreaName = devOverride?.PlanAreaId is not null
            ? await ResolveAreaNameAsync(organizationId, devOverride.PlanAreaId.Value) ?? planArea?.Name
            : planArea?.Name;

        var eligibleAreaIds = await ResolveEligibleAreaIdsAsync(organizationId, chain, planAreaId, devOverride?.PlanAreaId, ct);
        var consolidatorAreaIds = await ResolveConsolidatorAreaIdsAsync(organizationId, eligibleAreaIds, unit?.Id, ct);
        var (eligibleAreas, consolidatorAreas) = await ResolveAreaInfosAsync(organizationId, eligibleAreaIds, consolidatorAreaIds, chain, ct);

        return new PlanningContextDto
        {
            UserId = userId,
            UserName = userName,
            PositionId = position?.Id,
            // Si el cargo no está en el catálogo local, al menos se muestra el nombre que da GOBID.
            PositionName = position?.Name ?? positionNameFromGobid,
            UnitAreaId = unit?.Id,
            UnitAreaName = unit?.Name,
            PlanAreaId = planAreaId,
            PlanAreaName = planAreaName,
            EligibleAreaIds = eligibleAreaIds,
            EligibleAreas = eligibleAreas,
            ConsolidatorAreaIds = consolidatorAreaIds,
            ConsolidatorAreas = consolidatorAreas,
            Permissions = permissions.ToArray(),
            IsDevOverride = devOverride is not null
        };
    }

    /// <summary>
    /// Áreas sobre las que el usuario puede actuar: las de su cadena (unidad propia → nivel 1)
    /// que califican para planificar según el guardrail. Se incluye siempre el área de plan
    /// resuelta (y la del override de desarrollo) para no dejar sin acceso a quien sí es su dueño.
    /// </summary>
    private async Task<IReadOnlyCollection<Guid>> ResolveEligibleAreaIdsAsync(
        int organizationId,
        IReadOnlyList<OrganizationalStructure> chain,
        Guid? planAreaId,
        Guid? overrideAreaId,
        CancellationToken ct)
    {
        var eligible = new HashSet<Guid>();

        if (chain.Count > 0)
        {
            try
            {
                var qualifying = await _configService.GetQualifyingAreaIdsAsync(organizationId, ct);
                foreach (var area in chain.Where(a => qualifying.Contains(a.Id)))
                    eligible.Add(area.Id);
            }
            catch (Exception ex)
            {
                // Sin guardrail legible se cae al comportamiento anterior (solo el área de plan),
                // que es más restrictivo: nunca se amplía el acceso por un fallo de configuración.
                _logger.LogWarning(ex,
                    "No se pudo leer el guardrail de áreas de la organización {OrgId}; se usa solo el área de plan.", organizationId);
            }
        }

        if (planAreaId.HasValue) eligible.Add(planAreaId.Value);
        if (overrideAreaId.HasValue) eligible.Add(overrideAreaId.Value);

        return eligible;
    }

    /// <summary>
    /// Pertenencia a un área consolidadora: existe un ConsolidatorAreaProfile enlazado a alguna
    /// de las áreas del usuario. Un perfil sin OrganizationalStructureId no otorga pertenencia
    /// (falla cerrado): sin enlace no hay forma de saber a quién representa.
    /// </summary>
    private async Task<IReadOnlyCollection<Guid>> ResolveConsolidatorAreaIdsAsync(
        int organizationId,
        IReadOnlyCollection<Guid> eligibleAreaIds,
        Guid? unitAreaId,
        CancellationToken ct)
    {
        var candidates = new HashSet<Guid>(eligibleAreaIds);
        if (unitAreaId.HasValue) candidates.Add(unitAreaId.Value);
        if (candidates.Count == 0) return [];

        var result = await _consolidatorProfileRepository.FilterAsync(
            p => p.OrganizationId == organizationId
                && p.DeletedAt == null
                && p.OrganizationalStructureId != null
                && candidates.Contains(p.OrganizationalStructureId.Value));

        if (!result.IsSuccess || result.Value is null) return [];

        return result.Value
            .Where(p => p.OrganizationalStructureId.HasValue)
            .Select(p => p.OrganizationalStructureId!.Value)
            .Distinct()
            .ToArray();
    }

    /// <summary>
    /// El positionId de GOBID hace match con Position.Id (Guid) de PDS (confirmado con negocio).
    /// Plan B defensivo: si GOBID devolviera un entero, se intenta contra Position.Code.
    /// </summary>
    private async Task<Position?> ResolvePositionAsync(GobidPersonnelDto personnel, int organizationId)
    {
        var personnelId = personnel.PersonnelId;
        var rawPositionId = personnel.PositionId;
        if (string.IsNullOrWhiteSpace(rawPositionId))
        {
            _logger.LogWarning("GOBID no devolvió positionId para el personal {PersonnelId}.", personnelId);
            return null;
        }

        if (Guid.TryParse(rawPositionId, out var positionGuid))
        {
            var byId = await _positionRepository.GetAsync(p => p.Id == positionGuid && p.OrganizationalStructure!.OrganizationId == organizationId);
            if (byId.IsSuccess && byId.Value is not null)
                return byId.Value;
        }

        if (int.TryParse(rawPositionId, out var positionCode))
        {
            var byCode = await _positionRepository.GetAsync(p => p.Code == positionCode && p.OrganizationalStructure!.OrganizationId == organizationId);
            if (byCode.IsSuccess && byCode.Value is not null)
                return byCode.Value;
        }

        // Ya no es fatal: el área se resuelve con la unidad que GOBID adjunta al cargo. Se deja
        // en Information porque delata que el catálogo de cargos de PDS está desincronizado.
        _logger.LogInformation(
            "El positionId '{PositionId}' devuelto por GOBID no existe en la BD de PDS para la organización {OrgId}; se usa la unidad que reporta GOBID.",
            rawPositionId, organizationId);
        return null;
    }

    /// <summary>
    /// Asciende por ParentId devolviendo la cadena completa (unidad propia → nivel 1) y la
    /// unidad de nivel 1: aquella cuyo padre es la raíz. Si la unidad del usuario ES la raíz,
    /// no pertenece a ningún área que formule plan. La cadena permite que el guardrail decida
    /// después qué eslabones califican, en vez de fijar el nivel 1 por código.
    /// </summary>
    private async Task<(OrganizationalStructure? Unit, OrganizationalStructure? PlanArea, IReadOnlyList<OrganizationalStructure> Chain)> ResolveAreasAsync(
        int organizationId,
        Guid unitId,
        CancellationToken ct)
    {
        var unitResult = await _structureRepository.GetAsync(
            s => s.Id == unitId && s.OrganizationId == organizationId);
        if (!unitResult.IsSuccess || unitResult.Value is null)
        {
            _logger.LogWarning("La unidad {UnitId} del cargo no existe en la organización {OrgId}.", unitId, organizationId);
            return (null, null, []);
        }

        var unit = unitResult.Value;
        var chain = new List<OrganizationalStructure> { unit };
        var current = unit;
        var depth = 0;

        while (current.ParentId.HasValue && depth < MaxHierarchyDepth)
        {
            var parentResult = await _structureRepository.GetAsync(
                s => s.Id == current.ParentId.Value && s.OrganizationId == organizationId);
            if (!parentResult.IsSuccess || parentResult.Value is null)
                break;

            if (!parentResult.Value.ParentId.HasValue)
            {
                // current es nivel 1: su padre es la raíz.
                return (unit, current, chain);
            }

            current = parentResult.Value;
            chain.Add(current);
            depth++;
        }

        if (depth >= MaxHierarchyDepth)
            _logger.LogWarning("Jerarquía de más de {Max} niveles al resolver la unidad {UnitId}.", MaxHierarchyDepth, unitId);

        return (unit, null, chain);
    }

    private async Task<string?> ResolveAreaNameAsync(int organizationId, Guid areaId)
    {
        var result = await _structureRepository.GetAsync(
            s => s.Id == areaId && s.OrganizationId == organizationId);
        return result.IsSuccess ? result.Value?.Name : null;
    }

    private async Task<(IReadOnlyCollection<AreaInfoDto> Eligible, IReadOnlyCollection<AreaInfoDto> Consolidator)> ResolveAreaInfosAsync(
        int organizationId,
        IReadOnlyCollection<Guid> eligibleAreaIds,
        IReadOnlyCollection<Guid> consolidatorAreaIds,
        IReadOnlyList<OrganizationalStructure> chain,
        CancellationToken ct)
    {
        var allIds = eligibleAreaIds.Union(consolidatorAreaIds).Distinct().ToList();
        if (allIds.Count == 0) return ([], []);

        var knownMap = chain.ToDictionary(a => a.Id, a => a.Name);

        var missingIds = allIds.Where(id => !knownMap.ContainsKey(id)).ToList();
        if (missingIds.Count > 0)
        {
            var filterResult = await _structureRepository.FilterAsync(
                s => s.OrganizationId == organizationId && missingIds.Contains(s.Id));
            if (filterResult.IsSuccess && filterResult.Value is not null)
            {
                foreach (var s in filterResult.Value)
                {
                    knownMap[s.Id] = s.Name;
                }
            }
        }

        var eligible = eligibleAreaIds
            .Select(id => new AreaInfoDto(id, knownMap.TryGetValue(id, out var name) ? name : id.ToString()))
            .ToList();

        var consolidator = consolidatorAreaIds
            .Select(id => new AreaInfoDto(id, knownMap.TryGetValue(id, out var name) ? name : id.ToString()))
            .ToList();

        return (eligible, consolidator);
    }

    private static string CacheKey(int organizationId, Guid userId, string? personnelId)
        => $"planning-ctx:{organizationId}:{userId}:{personnelId ?? "none"}";
}
