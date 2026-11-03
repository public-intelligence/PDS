using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PDS.Application.Data;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Models;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

/// <summary>
/// Configuración de áreas que formulan plan. El guardrail se persiste en
/// Planning.PlanningConfigEntries (antes vivía en memoria estática: se perdía al reiniciar
/// y cada instancia podía tener un valor distinto). Se cachea 5 min y el caché se invalida
/// al guardar, porque de este valor depende qué áreas reciben POA al publicar un ciclo.
/// </summary>
public sealed class PlanningConfigService : IPlanningConfigService
{
    public const int DefaultMaxLevel = 2;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IUnitOfWork<PDSDbContext> _uow;
    private readonly IRepository<OrganizationalStructure> _structureRepository;
    private readonly IRepository<PlanningConfigEntry> _configRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PlanningConfigService> _logger;

    public PlanningConfigService(
        IUnitOfWork<PDSDbContext> uow,
        IRepository<OrganizationalStructure> structureRepository,
        IRepository<PlanningConfigEntry> configRepository,
        IMemoryCache cache,
        ILogger<PlanningConfigService> logger)
    {
        _uow = uow;
        _structureRepository = structureRepository;
        _configRepository = configRepository;
        _cache = cache;
        _logger = logger;
    }

    private static string CacheKey(int organizationId) => $"planning-guardrail:{organizationId}";

    public async Task<int> GetMaxLevelAsync(int organizationId, CancellationToken ct = default)
        => (await GetGuardrailAsync(organizationId, ct)).MaxLevel;

    private async Task<AreaGuardrailValue> GetGuardrailAsync(int organizationId, CancellationToken ct)
    {
        if (_cache.TryGetValue<AreaGuardrailValue>(CacheKey(organizationId), out var cached) && cached is not null)
            return cached;

        var result = await _configRepository.GetAsync(
            e => e.OrganizationId == organizationId && e.Key == PlanningConfigEntry.AreaGuardrailKey);

        var value = Deserialize(result.IsSuccess ? result.Value?.JsonValue : null);
        _cache.Set(CacheKey(organizationId), value, CacheTtl);
        return value;
    }

    private AreaGuardrailValue Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new AreaGuardrailValue { MaxLevel = DefaultMaxLevel };

        try
        {
            return JsonSerializer.Deserialize<AreaGuardrailValue>(json)
                ?? new AreaGuardrailValue { MaxLevel = DefaultMaxLevel };
        }
        catch (JsonException ex)
        {
            // Un valor corrupto no debe dejar la institución sin planificación: se cae al
            // predeterminado y se registra para corregirlo.
            _logger.LogError(ex, "El guardrail de áreas guardado no se pudo leer; se usa el nivel predeterminado {Level}.", DefaultMaxLevel);
            return new AreaGuardrailValue { MaxLevel = DefaultMaxLevel };
        }
    }

    public async Task<IHttpResult<PlanningAreaConfigDto>> GetAreaConfigAsync(int organizationId, CancellationToken ct = default)
    {
        var guardrail = await GetGuardrailAsync(organizationId, ct);
        var dto = await BuildAreaConfigDtoAsync(organizationId, guardrail, ct);
        return HttpResult<PlanningAreaConfigDto>.Success(dto);
    }

    public async Task<IHttpResult<PlanningAreaConfigDto>> UpdateAreaConfigAsync(
        int organizationId, int maxLevel, Guid userId, string? userName, List<Guid>? customAreaIds = null, CancellationToken ct = default)
    {
        if (maxLevel == -1 && (customAreaIds is null || customAreaIds.Count == 0))
        {
            return HttpResult<PlanningAreaConfigDto>.Failure(
                "Configuración inválida",
                new Dictionary<string, IEnumerable<string>>
                {
                    { "customAreaIds", new[] { "La selección personalizada requiere al menos un área." } }
                },
                System.Net.HttpStatusCode.BadRequest);
        }

        var guardrail = new AreaGuardrailValue
        {
            MaxLevel = maxLevel,
            CustomAreaIds = maxLevel == -1 ? [.. customAreaIds!.Distinct()] : []
        };

        var existing = await _configRepository.GetAsync(
            e => e.OrganizationId == organizationId && e.Key == PlanningConfigEntry.AreaGuardrailKey);

        var json = JsonSerializer.Serialize(guardrail);

        if (existing.IsSuccess && existing.Value is not null)
        {
            existing.Value.JsonValue = json;
            existing.Value.UpdatedBy = userId;
            existing.Value.UpdatedByName = userName;
            existing.Value.UpdatedAt = DateTime.UtcNow;
            await _configRepository.UpdateAsync(existing.Value);
        }
        else
        {
            await _configRepository.CreateAsync(new PlanningConfigEntry
            {
                OrganizationId = organizationId,
                Key = PlanningConfigEntry.AreaGuardrailKey,
                JsonValue = json,
                UpdatedBy = userId,
                UpdatedByName = userName,
                UpdatedAt = DateTime.UtcNow
            });
        }

        var commit = await _uow.CommitAsync();
        if (!commit.IsSuccess)
        {
            return HttpResult<PlanningAreaConfigDto>.Failure(
                "No se pudo guardar la configuración",
                new Dictionary<string, IEnumerable<string>> { { "maxLevel", new[] { commit.Error ?? "Error al guardar." } } },
                System.Net.HttpStatusCode.BadRequest);
        }

        _cache.Set(CacheKey(organizationId), guardrail, CacheTtl);

        _logger.LogInformation(
            "Usuario {UserName} ({UserId}) actualizó el guardrail de nivel jerárquico a {MaxLevel} para la org {OrgId}.",
            userName, userId, maxLevel, organizationId);

        var dto = await BuildAreaConfigDtoAsync(organizationId, guardrail, ct);
        return HttpResult<PlanningAreaConfigDto>.Success(dto);
    }

    public async Task<List<string>> GetQualifyingAreaNamesAsync(int organizationId, CancellationToken ct = default)
    {
        var guardrail = await GetGuardrailAsync(organizationId, ct);
        var config = await BuildAreaConfigDtoAsync(organizationId, guardrail, ct);
        return config.QualifyingAreas
            .Where(a => a.QualifiesForPlanning)
            .Select(a => a.Name)
            .OrderBy(n => n)
            .ToList();
    }

    public async Task<HashSet<Guid>> GetQualifyingAreaIdsAsync(int organizationId, CancellationToken ct = default)
    {
        var guardrail = await GetGuardrailAsync(organizationId, ct);
        var config = await BuildAreaConfigDtoAsync(organizationId, guardrail, ct);
        return [.. config.QualifyingAreas.Where(a => a.QualifiesForPlanning).Select(a => a.Id)];
    }

    private async Task<PlanningAreaConfigDto> BuildAreaConfigDtoAsync(
        int organizationId, AreaGuardrailValue guardrail, CancellationToken ct)
    {
        var result = await _structureRepository.FilterAsync(s => s.OrganizationId == organizationId && s.DeletedAt == null);
        var areas = result.IsSuccess ? result.Value.ToList() : [];

        var areaMap = areas.ToDictionary(a => a.Id);
        var parentNames = areas.ToDictionary(a => a.Id, a => a.Name);

        var maxLevel = guardrail.MaxLevel;
        var customSet = maxLevel == -1 ? new HashSet<Guid>(guardrail.CustomAreaIds) : null;

        var items = areas.Select(a =>
        {
            var depth = CalculateDepth(a, areaMap);
            var qualifies = maxLevel == -1
                ? (customSet != null && customSet.Contains(a.Id))
                : depth <= maxLevel;
            var parentName = a.ParentId.HasValue && parentNames.TryGetValue(a.ParentId.Value, out var pName) ? pName : null;

            return new PlanningAreaItemDto
            {
                Id = a.Id,
                Code = a.Code,
                Name = a.Name,
                ParentId = a.ParentId,
                ParentName = parentName,
                DepthLevel = depth,
                QualifiesForPlanning = qualifies
            };
        })
        .OrderBy(a => a.DepthLevel)
        .ThenBy(a => a.Name)
        .ToList();

        var desc = maxLevel switch
        {
            -1 => "Selección Personalizada: Unidades seleccionadas manualmente de forma individual.",
            0 => "Nivel 0: Solo Despacho / Máxima Autoridad",
            1 => "Nivel 1: Hasta Viceministerios / Direcciones Generales",
            2 => "Nivel 2: Hasta Direcciones (Predeterminado para PEI y POA)",
            3 => "Nivel 3: Hasta Departamentos",
            _ => $"Nivel {maxLevel}: Todas las unidades habilitadas"
        };

        return new PlanningAreaConfigDto
        {
            MaxLevel = maxLevel,
            Description = desc,
            QualifyingAreas = items
        };
    }

    public static int CalculateDepth(OrganizationalStructure area, IDictionary<Guid, OrganizationalStructure> areaMap)
    {
        int depth = 0;
        var current = area;
        while (current.ParentId.HasValue && areaMap.TryGetValue(current.ParentId.Value, out var parent) && depth < 20)
        {
            depth++;
            current = parent;
        }
        return depth;
    }
}
