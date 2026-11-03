using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PDS.Application.Data;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Enums;
using PDS.Domain.Models;
using PDS.Domain.Response;
using PDS.Service.Services.Excel;

namespace PDS.Service.Services.Planning;

public class ConsolidatorRoutingService : IConsolidatorRoutingService
{
    private readonly IUnitOfWork<PDSDbContext> _unitOfWork;
    private readonly IRepository<ConsolidatorRoutingRule> _ruleRepository;
    private readonly IRepository<MasterProduct> _productRepository;
    private readonly IPlanningActivityService _activityService;
    private readonly IMemoryCache _cache;

    public const int ConfidenceBonusUnspscPrefix = 10;
    public const int ConfidenceBonusValidated = 5;
    public const int ConfidencePenaltyAmbiguous = 15;
    public const int ConfidencePenaltyUnresolvedArea = 20;

    public ConsolidatorRoutingService(
        IUnitOfWork<PDSDbContext> unitOfWork,
        IRepository<ConsolidatorRoutingRule> ruleRepository,
        IRepository<MasterProduct> productRepository,
        IPlanningActivityService activityService,
        IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _ruleRepository = ruleRepository;
        _productRepository = productRepository;
        _activityService = activityService;
        _cache = cache;
    }

    public async Task<RoutingProposalDto> EvaluateAsync(int organizationId, MasterProduct product)
    {
        var rules = await GetActiveRulesCachedAsync(organizationId);

        var synonyms = product.Aliases?.Select(a => a.Text) ?? [];
        var haystackRaw = $"{product.Name} {product.OriginalDescription} {string.Join(" ", synonyms)} {product.Definition}";
        var haystack = ExcelTableReader.Normalize(haystackRaw);
        var paddedHaystack = $" {haystack} ";

        var matches = new List<(ConsolidatorRoutingRule Rule, bool TriggeredPrefix)>();

        foreach (var rule in rules)
        {
            // 1. Tipo
            if (!string.IsNullOrWhiteSpace(rule.ItemType) &&
                !string.Equals(rule.ItemType.Trim(), product.ItemType?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // 2. Prefijo UNSPSC
            var triggeredPrefix = false;
            if (!string.IsNullOrWhiteSpace(rule.UnspscPrefixes) && !string.IsNullOrWhiteSpace(product.UnspscCode))
            {
                var prefixes = rule.UnspscPrefixes.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                triggeredPrefix = prefixes.Any(p => product.UnspscCode.StartsWith(p, StringComparison.OrdinalIgnoreCase));
                if (!triggeredPrefix)
                    continue;
            }

            // 3. Palabras clave
            if (!string.IsNullOrWhiteSpace(rule.KeywordsNormalized))
            {
                var kwList = rule.KeywordsNormalized.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var keywordMatched = false;
                foreach (var kw in kwList)
                {
                    if (kw.Contains(' '))
                    {
                        if (haystack.Contains(kw, StringComparison.OrdinalIgnoreCase))
                        {
                            keywordMatched = true;
                            break;
                        }
                    }
                    else
                    {
                        if (paddedHaystack.Contains($" {kw} ", StringComparison.OrdinalIgnoreCase))
                        {
                            keywordMatched = true;
                            break;
                        }
                    }
                }

                if (!keywordMatched)
                    continue;
            }

            // 4. Exclusión (veta la regla)
            if (!string.IsNullOrWhiteSpace(rule.ExclusionKeywordsNormalized))
            {
                var exList = rule.ExclusionKeywordsNormalized.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var excluded = false;
                foreach (var ex in exList)
                {
                    if (ex.Contains(' '))
                    {
                        if (haystack.Contains(ex, StringComparison.OrdinalIgnoreCase))
                        {
                            excluded = true;
                            break;
                        }
                    }
                    else
                    {
                        if (paddedHaystack.Contains($" {ex} ", StringComparison.OrdinalIgnoreCase))
                        {
                            excluded = true;
                            break;
                        }
                    }
                }

                if (excluded)
                    continue;
            }

            matches.Add((rule, triggeredPrefix));
        }

        if (matches.Count == 0)
        {
            return new RoutingProposalDto
            {
                ConsolidatorAreaId = null,
                ConsolidatorAreaName = null,
                ConsolidatorAreaSource = ConsolidatorAreaSources.None,
                RuleId = null,
                RuleKey = null,
                RuleVersion = null,
                Confidence = 0,
                AreaAmbiguity = false,
                AlternativeAreas = Array.Empty<AlternativeAreaProposalDto>(),
                RequiresReview = true,
                ReviewReason = "Ninguna regla activa aplica al artículo."
            };
        }

        // Orden de desempate determinista: Priority ASC -> InstitutionallyValidated DESC -> BaseConfidence DESC -> Version DESC -> RuleKey ASC
        var ordered = matches
            .OrderBy(m => m.Rule.Priority)
            .ThenByDescending(m => m.Rule.InstitutionallyValidated)
            .ThenByDescending(m => m.Rule.BaseConfidence)
            .ThenByDescending(m => m.Rule.Version)
            .ThenBy(m => m.Rule.RuleKey, StringComparer.Ordinal)
            .ToList();

        var winner = ordered.First();
        var alternatives = ordered.Skip(1)
            .Where(m => m.Rule.ConsolidatorAreaId != winner.Rule.ConsolidatorAreaId)
            .Take(5)
            .Select(m => new AlternativeAreaProposalDto
            {
                ConsolidatorAreaId = m.Rule.ConsolidatorAreaId,
                ConsolidatorAreaName = m.Rule.ConsolidatorAreaName,
                RuleKey = m.Rule.RuleKey,
                RulePriority = m.Rule.Priority
            })
            .ToList();

        var ambiguity = ordered.Skip(1).Any(m => m.Rule.Priority == winner.Rule.Priority || m.Rule.ConsolidatorAreaId != winner.Rule.ConsolidatorAreaId);

        var confidence = winner.Rule.BaseConfidence;
        if (winner.TriggeredPrefix) confidence += ConfidenceBonusUnspscPrefix;
        if (winner.Rule.InstitutionallyValidated) confidence += ConfidenceBonusValidated;
        if (ambiguity) confidence -= ConfidencePenaltyAmbiguous;
        if (winner.Rule.ConsolidatorAreaId is null) confidence -= ConfidencePenaltyUnresolvedArea;
        confidence = Math.Clamp(confidence, 0, 100);

        var requiresReview = winner.Rule.ForceReview || ambiguity || confidence < 70 || winner.Rule.ConsolidatorAreaId is null;
        var reviewReasons = new List<string>();
        if (winner.Rule.ForceReview) reviewReasons.Add("Regla requiere revisión forzosa");
        if (ambiguity) reviewReasons.Add("Ambigüedad de áreas consolidadoras");
        if (confidence < 70) reviewReasons.Add($"Confianza baja ({confidence}%)");
        if (winner.Rule.ConsolidatorAreaId is null) reviewReasons.Add("Área consolidadora no resuelta");

        return new RoutingProposalDto
        {
            ConsolidatorAreaId = winner.Rule.ConsolidatorAreaId,
            ConsolidatorAreaName = winner.Rule.ConsolidatorAreaName,
            ConsolidatorAreaSource = ConsolidatorAreaSources.Rule,
            RuleId = winner.Rule.Id,
            RuleKey = winner.Rule.RuleKey,
            RuleVersion = winner.Rule.Version,
            Confidence = confidence,
            AreaAmbiguity = ambiguity,
            AlternativeAreas = alternatives,
            RequiresReview = requiresReview,
            ReviewReason = reviewReasons.Count > 0 ? string.Join("; ", reviewReasons) : null
        };
    }

    public async Task<IHttpResult<int>> ReevaluateAsync(int organizationId, Guid? masterProductId = null, bool onlyUnratified = true, Guid? userId = null, string? userName = null)
    {
        var query = _unitOfWork.Context.MasterProducts
            .AsNoTracking()
            .Where(p => p.OrganizationId == organizationId && p.DeletedAt == null);

        if (masterProductId.HasValue)
        {
            query = query.Where(p => p.Id == masterProductId.Value);
        }

        if (onlyUnratified)
        {
            // Ni la ratificación manual ni la asignación institucional del archivo se pisan:
            // las reglas solo proponen donde nadie decidió todavía (o donde propuso otra regla).
            query = query.Where(p =>
                p.ConsolidatorAreaSource != ConsolidatorAreaSources.Manual &&
                p.ConsolidatorAreaSource != ConsolidatorAreaSources.Imported);
        }

        var ids = await query.Select(p => p.Id).ToListAsync();
        var updatedCount = 0;

        // Por lotes, vaciando el tracker: con 18 mil artículos vivos cada SaveChanges se
        // degrada y el import posterior a la carga no termina.
        foreach (var lote in ids.Chunk(500))
        {
            var products = await _unitOfWork.Context.MasterProducts
                .Include(p => p.Aliases)
                .Where(p => lote.Contains(p.Id))
                .ToListAsync();

            foreach (var product in products)
            {
                var proposal = await EvaluateAsync(organizationId, product);

                // Una regla que no acierta no puede dejar sin área a un artículo que ya la tenía.
                if (proposal.ConsolidatorAreaId is null && product.ConsolidatorAreaId is not null)
                    continue;

                product.ConsolidatorAreaId = proposal.ConsolidatorAreaId;
                product.ConsolidatorAreaName = proposal.ConsolidatorAreaName;
                product.ConsolidatorAreaSource = proposal.ConsolidatorAreaSource;
                product.ConsolidatorRuleId = proposal.RuleId;
                product.ConsolidatorRuleKey = proposal.RuleKey;
                product.ConsolidatorRuleVersion = proposal.RuleVersion;
                product.ConsolidatorAreaConfidence = proposal.Confidence;
                product.AreaAmbiguity = proposal.AreaAmbiguity;
                product.AlternativeAreasJson = proposal.AlternativeAreas.Count > 0 ? JsonSerializer.Serialize(proposal.AlternativeAreas) : null;
                product.RequiresReview = proposal.RequiresReview;
                product.ReviewReason = proposal.ReviewReason;

                // El artículo se vuelve utilizable en el POA en cuanto tiene área resuelta.
                product.State = product.ConsolidatorAreaId.HasValue
                    ? MasterProductStates.Active
                    : MasterProductStates.Pending;

                product.UpdatedAt = DateTime.UtcNow;
                updatedCount++;
            }

            await _unitOfWork.Context.SaveChangesAsync();
            _unitOfWork.Context.ChangeTracker.Clear();
        }

        if (userId.HasValue)
        {
            await _activityService.LogAsync(
                organizationId,
                userId.Value,
                userName,
                "Catálogos",
                "Re-evaluó reglas de ruteo",
                $"Re-evaluó reglas de ruteo para {updatedCount} artículos (onlyUnratified={onlyUnratified})",
                "MasterProduct",
                masterProductId ?? Guid.Empty);
        }

        return HttpResult<int>.Success(updatedCount);
    }

    public async Task<IHttpResult<RulePreviewResultDto>> PreviewAsync(int organizationId, Guid ruleId, int take = 20)
    {
        var rule = await _unitOfWork.Context.ConsolidatorRoutingRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Id == ruleId);

        if (rule is null)
        {
            return HttpResult<RulePreviewResultDto>.Failure(
                "Regla no encontrada",
                new Dictionary<string, IEnumerable<string>> { { "ruleId", new[] { "La regla de ruteo especificada no existe." } } },
                HttpStatusCode.NotFound);
        }

        var prefixes = string.IsNullOrWhiteSpace(rule.UnspscPrefixes)
            ? Array.Empty<string>()
            : rule.UnspscPrefixes.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var firstPrefix = prefixes.FirstOrDefault();

        var query = _unitOfWork.Context.MasterProducts
            .AsNoTracking()
            .Where(p => p.OrganizationId == organizationId && p.State != MasterProductStates.Disabled);

        if (!string.IsNullOrEmpty(firstPrefix))
        {
            query = query.Where(p => p.ConsolidatorRuleKey == rule.RuleKey || (p.UnspscCode != null && p.UnspscCode.StartsWith(firstPrefix)));
        }
        else
        {
            query = query.Where(p => p.ConsolidatorRuleKey == rule.RuleKey || p.ConsolidatorAreaId == rule.ConsolidatorAreaId);
        }

        var matchedCount = await query.CountAsync();
        var matchedProducts = await query.OrderBy(p => p.Name).Take(take).ToListAsync();

        var result = new RulePreviewResultDto
        {
            RuleId = rule.Id,
            RuleKey = rule.RuleKey,
            MatchedProductsCount = matchedCount,
            SampleMatchedProducts = matchedProducts.Select(MapProductDto).ToList()
        };

        return HttpResult<RulePreviewResultDto>.Success(result);
    }

    public async Task<IHttpResult<IReadOnlyList<ConsolidatorRoutingRuleDto>>> ListRulesAsync(int organizationId)
    {
        var rules = await _unitOfWork.Context.ConsolidatorRoutingRules
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId && r.DeletedAt == null)
            .OrderBy(r => r.Priority)
            .ThenByDescending(r => r.Version)
            .Select(r => MapRuleDto(r))
            .ToListAsync();

        return HttpResult<IReadOnlyList<ConsolidatorRoutingRuleDto>>.Success(rules);
    }

    public async Task<IHttpResult<ConsolidatorRoutingRuleDto>> GetRuleByIdAsync(int organizationId, Guid id)
    {
        var rule = await _unitOfWork.Context.ConsolidatorRoutingRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Id == id);

        if (rule is null)
        {
            return HttpResult<ConsolidatorRoutingRuleDto>.Failure(
                "Regla no encontrada",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { "La regla especificada no existe." } } },
                HttpStatusCode.NotFound);
        }

        return HttpResult<ConsolidatorRoutingRuleDto>.Success(MapRuleDto(rule));
    }

    public async Task<IHttpResult<ConsolidatorRoutingRuleDto>> CreateRuleAsync(int organizationId, CreateConsolidatorRoutingRuleDto dto)
    {
        var normalizedKw = NormalizeSeparated(dto.Keywords);
        var normalizedEx = NormalizeSeparated(dto.ExclusionKeywords);

        var rule = new ConsolidatorRoutingRule
        {
            OrganizationId = organizationId,
            RuleKey = dto.RuleKey.Trim().ToUpperInvariant(),
            Version = 1,
            Priority = dto.Priority,
            Keywords = dto.Keywords?.Trim(),
            KeywordsNormalized = normalizedKw,
            ExclusionKeywords = dto.ExclusionKeywords?.Trim(),
            ExclusionKeywordsNormalized = normalizedEx,
            UnspscPrefixes = dto.UnspscPrefixes?.Trim(),
            ItemType = dto.ItemType?.Trim(),
            ConsolidatorAreaName = dto.ConsolidatorAreaName.Trim(),
            ConsolidatorAreaId = dto.ConsolidatorAreaId,
            Justification = dto.Justification?.Trim(),
            IsActive = true,
            InstitutionallyValidated = dto.InstitutionallyValidated,
            BaseConfidence = dto.BaseConfidence,
            ForceReview = dto.ForceReview,
            RuleState = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _unitOfWork.Context.ConsolidatorRoutingRules.Add(rule);
        await _unitOfWork.Context.SaveChangesAsync();
        InvalidateCache(organizationId);

        return HttpResult<ConsolidatorRoutingRuleDto>.Success(MapRuleDto(rule));
    }

    public async Task<IHttpResult<ConsolidatorRoutingRuleDto>> UpdateMetadataAsync(int organizationId, Guid id, UpdateConsolidatorRoutingRuleMetadataDto dto)
    {
        var rule = await _unitOfWork.Context.ConsolidatorRoutingRules
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Id == id);

        if (rule is null)
        {
            return HttpResult<ConsolidatorRoutingRuleDto>.Failure(
                "Regla no encontrada",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { "La regla especificada no existe." } } },
                HttpStatusCode.NotFound);
        }

        if (dto.Justification is not null) rule.Justification = dto.Justification.Trim();
        rule.IsActive = dto.IsActive;
        if (!string.IsNullOrWhiteSpace(dto.RuleState)) rule.RuleState = dto.RuleState.Trim();
        rule.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Context.SaveChangesAsync();
        InvalidateCache(organizationId);

        return HttpResult<ConsolidatorRoutingRuleDto>.Success(MapRuleDto(rule));
    }

    public async Task<IHttpResult<ConsolidatorRoutingRuleDto>> CreateNewVersionAsync(int organizationId, Guid id, CreateNewRuleVersionDto dto)
    {
        var existingRule = await _unitOfWork.Context.ConsolidatorRoutingRules
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Id == id);

        if (existingRule is null)
        {
            return HttpResult<ConsolidatorRoutingRuleDto>.Failure(
                "Regla no encontrada",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { "La regla base especificada no existe." } } },
                HttpStatusCode.NotFound);
        }

        existingRule.IsActive = false;
        existingRule.RuleState = "Superseded";
        existingRule.UpdatedAt = DateTime.UtcNow;

        var maxVersion = await _unitOfWork.Context.ConsolidatorRoutingRules
            .Where(r => r.OrganizationId == organizationId && r.RuleKey == existingRule.RuleKey)
            .MaxAsync(r => (int?)r.Version) ?? existingRule.Version;

        var newRule = new ConsolidatorRoutingRule
        {
            OrganizationId = organizationId,
            RuleKey = existingRule.RuleKey,
            Version = maxVersion + 1,
            SupersedesVersion = existingRule.Version,
            Priority = dto.Priority,
            Keywords = dto.Keywords?.Trim(),
            KeywordsNormalized = NormalizeSeparated(dto.Keywords),
            ExclusionKeywords = dto.ExclusionKeywords?.Trim(),
            ExclusionKeywordsNormalized = NormalizeSeparated(dto.ExclusionKeywords),
            UnspscPrefixes = dto.UnspscPrefixes?.Trim(),
            ItemType = dto.ItemType?.Trim(),
            ConsolidatorAreaName = dto.ConsolidatorAreaName.Trim(),
            ConsolidatorAreaId = dto.ConsolidatorAreaId,
            Justification = dto.Justification?.Trim(),
            IsActive = true,
            InstitutionallyValidated = dto.InstitutionallyValidated,
            BaseConfidence = dto.BaseConfidence,
            ForceReview = dto.ForceReview,
            RuleState = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _unitOfWork.Context.ConsolidatorRoutingRules.Add(newRule);
        await _unitOfWork.Context.SaveChangesAsync();
        InvalidateCache(organizationId);

        return HttpResult<ConsolidatorRoutingRuleDto>.Success(MapRuleDto(newRule));
    }

    public async Task<IHttpResult> SetActiveAsync(int organizationId, Guid id, bool isActive)
    {
        var rule = await _unitOfWork.Context.ConsolidatorRoutingRules
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Id == id);

        if (rule is null)
        {
            return HttpResult.Failure(
                "Regla no encontrada",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { "La regla especificada no existe." } } },
                HttpStatusCode.NotFound);
        }

        rule.IsActive = isActive;
        rule.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Context.SaveChangesAsync();
        InvalidateCache(organizationId);

        return HttpResult.Success();
    }

    public async Task<IHttpResult> DeleteRuleAsync(int organizationId, Guid id)
    {
        var rule = await _unitOfWork.Context.ConsolidatorRoutingRules
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Id == id);

        if (rule is null)
        {
            return HttpResult.Failure(
                "Regla no encontrada",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { "La regla especificada no existe." } } },
                HttpStatusCode.NotFound);
        }

        rule.IsActive = false;
        rule.DeletedAt = DateTime.UtcNow;
        rule.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Context.SaveChangesAsync();
        InvalidateCache(organizationId);

        return HttpResult.Success();
    }

    private async Task<List<ConsolidatorRoutingRule>> GetActiveRulesCachedAsync(int organizationId)
    {
        var cacheKey = $"routing_rules_{organizationId}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            return await _unitOfWork.Context.ConsolidatorRoutingRules
                .AsNoTracking()
                .Where(r => r.OrganizationId == organizationId && r.IsActive)
                .OrderBy(r => r.Priority)
                .ThenByDescending(r => r.Version)
                .ThenBy(r => r.RuleKey)
                .ToListAsync();
        }) ?? [];
    }

    private void InvalidateCache(int organizationId)
    {
        _cache.Remove($"routing_rules_{organizationId}");
    }

    private static string? NormalizeSeparated(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var parts = text.Split(new[] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var normalizedParts = parts.Select(p => ExcelTableReader.Normalize(p)).Where(p => p.Length > 0);
        return string.Join("|", normalizedParts);
    }

    private static ConsolidatorRoutingRuleDto MapRuleDto(ConsolidatorRoutingRule r) => new()
    {
        Id = r.Id,
        RuleKey = r.RuleKey,
        Version = r.Version,
        Author = r.Author,
        RuleDate = r.RuleDate,
        SupersedesVersion = r.SupersedesVersion,
        Reversible = r.Reversible,
        Priority = r.Priority,
        Keywords = r.Keywords,
        ExclusionKeywords = r.ExclusionKeywords,
        UnspscPrefixes = r.UnspscPrefixes,
        ItemType = r.ItemType,
        ConsolidatorAreaName = r.ConsolidatorAreaName,
        ConsolidatorAreaId = r.ConsolidatorAreaId,
        Justification = r.Justification,
        IsActive = r.IsActive,
        InstitutionallyValidated = r.InstitutionallyValidated,
        BaseConfidence = r.BaseConfidence,
        ForceReview = r.ForceReview,
        RuleState = r.RuleState,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };

    private static MasterProductDto MapProductDto(MasterProduct p) => new()
    {
        Id = p.Id,
        OrganizationId = p.OrganizationId,
        SourceId = p.SourceId,
        Code = p.Code,
        Name = p.Name,
        OriginalDescription = p.OriginalDescription,
        EnglishName = p.EnglishName,
        Definition = p.Definition,
        UnifiedCodeId = p.UnifiedCodeId,
        UnspscCode = p.UnspscCode,
        UnspscName = p.UnspscName,
        ItemType = p.ItemType,
        ItemSubtype = p.ItemSubtype,
        AssetTreatment = p.AssetTreatment,
        MeasureUnit = p.MeasureUnit,
        ReferencePrice = p.ReferencePrice,
        Currency = p.Currency,
        PriceSource = p.PriceSource,
        ExpenseObjectId = p.ExpenseObjectId,
        ExpenseObjectCode = p.ExpenseObjectCode,
        ExpenseObjectName = p.ExpenseObjectName,
        AccountingAccountId = p.AccountingAccountId,
        AccountingAccountCode = p.AccountingAccountCode,
        AccountingAccountName = p.AccountingAccountName,
        ConsolidatorAreaId = p.ConsolidatorAreaId,
        ConsolidatorAreaName = p.ConsolidatorAreaName,
        ConsolidatorAreaSource = p.ConsolidatorAreaSource,
        ConsolidatorRuleId = p.ConsolidatorRuleId,
        ConsolidatorRuleKey = p.ConsolidatorRuleKey,
        ConsolidatorRuleVersion = p.ConsolidatorRuleVersion,
        ConsolidatorAreaConfidence = p.ConsolidatorAreaConfidence,
        AreaAmbiguity = p.AreaAmbiguity,
        AlternativeAreasJson = p.AlternativeAreasJson,
        RequiresReview = p.RequiresReview,
        ReviewReason = p.ReviewReason,
        Notes = p.Notes,
        State = p.State,
        DuplicateOfMasterProductId = p.DuplicateOfMasterProductId,
        RequestedBy = p.RequestedBy,
        RequestedByName = p.RequestedByName,
        RequestedAreaId = p.RequestedAreaId,
        RequestedAt = p.RequestedAt,
        RequestJustification = p.RequestJustification,
        Version = p.Version,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
