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
using PDS.Domain.Enums;
using PDS.Domain.Models;
using PDS.Domain.Response;
using System.Text.Json;
using PDS.Service.Services.Excel;

namespace PDS.Service.Services.Planning;

public class MasterProductService : IMasterProductService
{
    private readonly IUnitOfWork<PDSDbContext> _unitOfWork;
    private readonly IRepository<MasterProduct> _productRepository;
    private readonly IConsolidatorRoutingService _routingService;
    private readonly ISequenceService _sequenceService;
    private readonly IPlanningActivityService _activityService;

    public const string SequenceScope = "MASTER_PRODUCT";

    public MasterProductService(
        IUnitOfWork<PDSDbContext> unitOfWork,
        IRepository<MasterProduct> productRepository,
        IConsolidatorRoutingService routingService,
        ISequenceService sequenceService,
        IPlanningActivityService activityService)
    {
        _unitOfWork = unitOfWork;
        _productRepository = productRepository;
        _routingService = routingService;
        _sequenceService = sequenceService;
        _activityService = activityService;
    }

    public async Task<IHttpResult<IReadOnlyList<MasterProductSearchItemDto>>> SearchAsync(int organizationId, string query, string? itemType = null, int take = 20)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return HttpResult<IReadOnlyList<MasterProductSearchItemDto>>.Success(Array.Empty<MasterProductSearchItemDto>());
        }

        var normalized = ExcelTableReader.Normalize(query);
        var q = _unitOfWork.Context.MasterProducts
            .AsNoTracking()
            .Where(p => p.OrganizationId == organizationId && p.State == MasterProductStates.Active && p.ConsolidatorAreaId != null && p.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(itemType))
        {
            q = q.Where(p => p.ItemType == itemType);
        }

        q = q.Where(p => EF.Functions.Like(p.SearchText, $"%{normalized}%"));

        var items = await q
            .OrderBy(p => p.Name)
            .Take(take)
            .Select(p => new MasterProductSearchItemDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                UnspscCode = p.UnspscCode,
                UnspscName = p.UnspscName,
                ItemType = p.ItemType,
                MeasureUnit = p.MeasureUnit,
                ReferencePrice = p.ReferencePrice,
                ExpenseObjectId = p.ExpenseObjectId,
                ExpenseObjectCode = p.ExpenseObjectCode,
                ExpenseObjectName = p.ExpenseObjectName,
                AccountingAccountId = p.AccountingAccountId,
                AccountingAccountCode = p.AccountingAccountCode,
                AccountingAccountName = p.AccountingAccountName,
                ConsolidatorAreaId = p.ConsolidatorAreaId,
                ConsolidatorAreaName = p.ConsolidatorAreaName
            })
            .ToListAsync();

        return HttpResult<IReadOnlyList<MasterProductSearchItemDto>>.Success(items);
    }

    public async Task<IHttpResult<PagedResponseDto<MasterProductDto>>> ListAsync(int organizationId, string? q = null, string? state = null, Guid? consolidatorAreaId = null, bool? missingMapping = null, bool? requiresReview = null, string? unspscPrefix = null, int page = 1, int pageSize = 20)
    {
        var query = _unitOfWork.Context.MasterProducts
            .AsNoTracking()
            .Where(p => p.OrganizationId == organizationId && p.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var norm = ExcelTableReader.Normalize(q);
            query = query.Where(p => EF.Functions.Like(p.SearchText, $"%{norm}%") || p.Code.Contains(q));
        }

        if (!string.IsNullOrWhiteSpace(state))
        {
            query = query.Where(p => p.State == state);
        }

        if (consolidatorAreaId.HasValue)
        {
            query = query.Where(p => p.ConsolidatorAreaId == consolidatorAreaId.Value);
        }

        if (missingMapping == true)
        {
            query = query.Where(p => p.ExpenseObjectId == null || p.AccountingAccountId == null || p.ConsolidatorAreaId == null);
        }

        if (requiresReview == true)
        {
            query = query.Where(p => p.RequiresReview || p.AreaAmbiguity);
        }

        if (!string.IsNullOrWhiteSpace(unspscPrefix))
        {
            query = query.Where(p => p.UnspscCode != null && p.UnspscCode.StartsWith(unspscPrefix));
        }

        var total = await query.CountAsync();
        var rawEntities = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = rawEntities.Select(MapDto).ToList();

        var result = new PagedResponseDto<MasterProductDto>(items, page, pageSize, total);
        return HttpResult<PagedResponseDto<MasterProductDto>>.Success(result);
    }

    public async Task<IHttpResult<MasterProductDetailDto>> GetByIdAsync(int organizationId, Guid id)
    {
        var product = await _unitOfWork.Context.MasterProducts
            .AsNoTracking()
            .Include(p => p.Aliases)
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == id && p.DeletedAt == null);

        if (product is null)
        {
            return HttpResult<MasterProductDetailDto>.Failure(
                "Artículo no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { "El artículo especificado no existe." } } },
                HttpStatusCode.NotFound);
        }

        return HttpResult<MasterProductDetailDto>.Success(MapDetailDto(product));
    }

    public async Task<IHttpResult<MasterProductDetailDto>> CreateAsync(int organizationId, CreateMasterProductDto dto, Guid userId, string? userName)
    {
        var seqNumber = await _sequenceService.NextAsync(organizationId, SequenceScope, "GLOBAL");
        var code = $"ART-{seqNumber:D6}";

        var searchText = ExcelTableReader.Normalize($"{dto.Name} {dto.OriginalDescription} {string.Join(" ", dto.Synonyms ?? [])} {dto.Definition}");

        UnifiedCode? unspsc = null;
        if (dto.UnifiedCodeId.HasValue)
        {
            unspsc = await _unitOfWork.Context.UnifiedCodes.FindAsync(dto.UnifiedCodeId.Value);
        }

        ExpenseObject? exp = null;
        if (dto.ExpenseObjectId.HasValue)
        {
            exp = await _unitOfWork.Context.ExpenseObjects.FindAsync(dto.ExpenseObjectId.Value);
        }

        AccountingAccount? acc = null;
        if (dto.AccountingAccountId.HasValue)
        {
            acc = await _unitOfWork.Context.AccountingAccounts.FindAsync(dto.AccountingAccountId.Value);
        }

        var product = new MasterProduct
        {
            OrganizationId = organizationId,
            Code = code,
            Name = dto.Name.Trim(),
            SearchText = searchText,
            OriginalDescription = dto.OriginalDescription?.Trim(),
            Definition = dto.Definition?.Trim(),
            UnifiedCodeId = unspsc?.Id,
            UnspscCode = unspsc?.Code,
            UnspscName = unspsc?.Name,
            ExpenseObjectId = exp?.Id,
            ExpenseObjectCode = exp?.Code,
            ExpenseObjectName = exp?.Name,
            AccountingAccountId = acc?.Id,
            AccountingAccountCode = acc?.Code,
            AccountingAccountName = acc?.Name,
            ItemType = dto.ItemType?.Trim() ?? "Bien",
            MeasureUnit = dto.MeasureUnit?.Trim() ?? "Unidad",
            ReferencePrice = dto.ReferencePrice,
            ConsolidatorAreaSource = "None",
            State = "Draft",
            RequiresReview = false,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (dto.ConsolidatorAreaId.HasValue && dto.ConsolidatorAreaId.Value != Guid.Empty)
        {
            var profile = await _unitOfWork.Context.ConsolidatorAreaProfiles
                .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && (p.OrganizationalStructureId == dto.ConsolidatorAreaId.Value || p.Id == dto.ConsolidatorAreaId.Value));

            product.ConsolidatorAreaId = dto.ConsolidatorAreaId.Value;
            product.ConsolidatorAreaName = profile?.AreaName ?? "Área Consolidadora";
            product.ConsolidatorAreaSource = ConsolidatorAreaSources.Manual;
            product.State = MasterProductStates.Active;
        }
        else
        {
            var proposal = await _routingService.EvaluateAsync(organizationId, product);
            product.ConsolidatorAreaId = proposal.ConsolidatorAreaId;
            product.ConsolidatorAreaName = proposal.ConsolidatorAreaName;
            product.ConsolidatorAreaSource = proposal.ConsolidatorAreaSource;
            product.ConsolidatorRuleId = proposal.RuleId;
            product.ConsolidatorRuleKey = proposal.RuleKey;
            product.ConsolidatorRuleVersion = proposal.RuleVersion;
            product.ConsolidatorAreaConfidence = proposal.Confidence;
            product.AreaAmbiguity = proposal.AreaAmbiguity;
            product.RequiresReview = proposal.RequiresReview;
            product.ReviewReason = proposal.ReviewReason;
            if (product.ConsolidatorAreaId.HasValue) product.State = MasterProductStates.Active;
        }

        _unitOfWork.Context.MasterProducts.Add(product);
        await _unitOfWork.Context.SaveChangesAsync();

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Catálogos",
            "Creó artículo maestro",
            $"Creó el artículo '{product.Code} — {product.Name}'",
            "MasterProduct",
            product.Id);

        return HttpResult<MasterProductDetailDto>.Success(MapDetailDto(product));
    }

    public async Task<IHttpResult<MasterProductDetailDto>> UpdateAsync(int organizationId, Guid id, UpdateMasterProductDto dto, Guid userId, string? userName)
    {
        var product = await _unitOfWork.Context.MasterProducts
            .Include(p => p.Aliases)
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == id && p.DeletedAt == null);

        if (product is null)
        {
            return HttpResult<MasterProductDetailDto>.Failure(
                "Artículo no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { "El artículo especificado no existe." } } },
                HttpStatusCode.NotFound);
        }

        product.Name = dto.Name.Trim();
        product.OriginalDescription = dto.OriginalDescription?.Trim();
        product.EnglishName = dto.EnglishName?.Trim();
        product.Definition = dto.Definition?.Trim();
        product.ItemType = dto.ItemType?.Trim() ?? product.ItemType;
        product.MeasureUnit = dto.MeasureUnit?.Trim() ?? product.MeasureUnit;
        product.ReferencePrice = dto.ReferencePrice ?? product.ReferencePrice;

        if (dto.UnifiedCodeId.HasValue && dto.UnifiedCodeId.Value != product.UnifiedCodeId)
        {
            var unspsc = await _unitOfWork.Context.UnifiedCodes.FindAsync(dto.UnifiedCodeId.Value);
            product.UnifiedCodeId = unspsc?.Id;
            product.UnspscCode = unspsc?.Code;
            product.UnspscName = unspsc?.Name;
        }

        var searchText = ExcelTableReader.Normalize($"{product.Name} {product.OriginalDescription} {string.Join(" ", dto.Synonyms ?? [])} {product.Definition}");
        product.SearchText = searchText;

        if (dto.Synonyms is not null)
        {
            _unitOfWork.Context.MasterProductAliases.RemoveRange(product.Aliases.Where(a => a.Kind == "Synonym"));
            foreach (var syn in dto.Synonyms.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                product.Aliases.Add(new MasterProductAlias
                {
                    OrganizationId = organizationId,
                    Kind = "Synonym",
                    Text = syn.Trim(),
                    NormalizedText = ExcelTableReader.Normalize(syn)
                });
            }
        }

        if (product.ConsolidatorAreaSource != ConsolidatorAreaSources.Manual)
        {
            var proposal = await _routingService.EvaluateAsync(organizationId, product);
            product.ConsolidatorAreaId = proposal.ConsolidatorAreaId;
            product.ConsolidatorAreaName = proposal.ConsolidatorAreaName;
            product.ConsolidatorAreaSource = proposal.ConsolidatorAreaSource;
            product.ConsolidatorRuleId = proposal.RuleId;
            product.ConsolidatorRuleKey = proposal.RuleKey;
            product.ConsolidatorRuleVersion = proposal.RuleVersion;
            product.ConsolidatorAreaConfidence = proposal.Confidence;
            product.AreaAmbiguity = proposal.AreaAmbiguity;
            product.RequiresReview = proposal.RequiresReview;
            product.ReviewReason = proposal.ReviewReason;
        }

        product.Version += 1;
        product.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Context.SaveChangesAsync();

        return HttpResult<MasterProductDetailDto>.Success(MapDetailDto(product));
    }

    public async Task<IHttpResult<MasterProductDetailDto>> UpdateMappingAsync(int organizationId, Guid id, UpdateMasterProductMappingDto dto, Guid userId, string? userName)
    {
        var product = await _unitOfWork.Context.MasterProducts
            .Include(p => p.Aliases)
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == id && p.DeletedAt == null);

        if (product is null)
        {
            return HttpResult<MasterProductDetailDto>.Failure(
                "Artículo no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { "El artículo especificado no existe." } } },
                HttpStatusCode.NotFound);
        }

        if (!string.IsNullOrWhiteSpace(dto.ItemType)) product.ItemType = dto.ItemType.Trim();
        if (!string.IsNullOrWhiteSpace(dto.State)) product.State = dto.State.Trim();
        if (!string.IsNullOrWhiteSpace(dto.MeasureUnit)) product.MeasureUnit = dto.MeasureUnit.Trim();
        if (dto.ReferencePrice.HasValue) product.ReferencePrice = dto.ReferencePrice.Value;
        if (dto.Notes != null) product.Notes = dto.Notes.Trim();

        if (dto.ExpenseObjectId.HasValue)
        {
            var exp = await _unitOfWork.Context.ExpenseObjects.FindAsync(dto.ExpenseObjectId.Value);
            product.ExpenseObjectId = exp?.Id;
            product.ExpenseObjectCode = exp?.Code;
            product.ExpenseObjectName = exp?.Name;
        }

        if (dto.AccountingAccountId.HasValue)
        {
            var acc = await _unitOfWork.Context.AccountingAccounts.FindAsync(dto.AccountingAccountId.Value);
            product.AccountingAccountId = acc?.Id;
            product.AccountingAccountCode = acc?.Code;
            product.AccountingAccountName = acc?.Name;
        }

        if (dto.ConsolidatorAreaId.HasValue)
        {
            var area = await _unitOfWork.Context.OrganizationalStructures.FindAsync(dto.ConsolidatorAreaId.Value);
            product.ConsolidatorAreaId = area?.Id;
            product.ConsolidatorAreaName = area?.Name;
            product.ConsolidatorAreaSource = ConsolidatorAreaSources.Manual;
            product.ConsolidatorRuleId = null;
            product.ConsolidatorAreaConfidence = 100;
            product.AreaAmbiguity = false;
            product.RequiresReview = false;
            product.ReviewReason = null;
            if (product.State == MasterProductStates.Pending) product.State = MasterProductStates.Active;
        }
        else if (dto.RatifyConsolidatorArea && product.ConsolidatorAreaId.HasValue)
        {
            product.ConsolidatorAreaSource = ConsolidatorAreaSources.Manual;
        }

        product.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Context.SaveChangesAsync();

        return HttpResult<MasterProductDetailDto>.Success(MapDetailDto(product));
    }

    public async Task<IHttpResult<bool>> DeleteAsync(int organizationId, Guid id, Guid userId, string? userName)
    {
        var product = await _unitOfWork.Context.MasterProducts
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == id && p.DeletedAt == null);

        if (product is null)
        {
            return HttpResult<bool>.Failure(
                "Artículo no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { "El artículo especificado no existe." } } },
                HttpStatusCode.NotFound);
        }

        product.DeletedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Context.SaveChangesAsync();

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Catálogos",
            "Eliminó artículo maestro",
            $"Eliminó el artículo '{product.Code} — {product.Name}'",
            "MasterProduct",
            product.Id);

        return HttpResult<bool>.Success(true);
    }

    public async Task<IHttpResult<MasterProductDetailDto>> RatifyConsolidatorAreaAsync(int organizationId, Guid id, RatifyConsolidatorAreaDto dto, Guid userId, string? userName)
    {
        var product = await _unitOfWork.Context.MasterProducts
            .Include(p => p.Aliases)
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == id && p.DeletedAt == null);

        if (product is null)
        {
            return HttpResult<MasterProductDetailDto>.Failure(
                "Artículo no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { "El artículo especificado no existe." } } },
                HttpStatusCode.NotFound);
        }

        var area = await _unitOfWork.Context.OrganizationalStructures.FindAsync(dto.ConsolidatorAreaId);
        if (area is null)
        {
            return HttpResult<MasterProductDetailDto>.Failure(
                "Área no encontrada",
                new Dictionary<string, IEnumerable<string>> { { "consolidatorAreaId", new[] { "El área consolidadora especificada no existe." } } },
                HttpStatusCode.NotFound);
        }

        product.ConsolidatorAreaId = area.Id;
        product.ConsolidatorAreaName = area.Name;
        product.ConsolidatorAreaSource = ConsolidatorAreaSources.Manual;
        product.ConsolidatorRuleId = null;
        product.ConsolidatorRuleKey = null;
        product.ConsolidatorRuleVersion = null;
        product.ConsolidatorAreaConfidence = 100;
        product.AreaAmbiguity = false;
        product.AlternativeAreasJson = null;
        product.RequiresReview = false;
        product.ReviewReason = null;
        if (!string.IsNullOrWhiteSpace(dto.Notes)) product.Notes = dto.Notes.Trim();

        if (product.State == MasterProductStates.Pending)
        {
            product.State = MasterProductStates.Active;
        }

        product.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Context.SaveChangesAsync();

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Catálogos",
            "Ratificó área consolidadora",
            $"Ratificó el área '{area.Name}' para el artículo '{product.Code} — {product.Name}'",
            "MasterProduct",
            product.Id);

        return HttpResult<MasterProductDetailDto>.Success(MapDetailDto(product));
    }

    public async Task<IHttpResult<int>> BulkRatifyAsync(int organizationId, BulkRatifyByUnspscDto dto, Guid userId, string? userName)
    {
        var area = await _unitOfWork.Context.OrganizationalStructures.FindAsync(dto.ConsolidatorAreaId);
        if (area is null)
        {
            return HttpResult<int>.Failure(
                "Área no encontrada",
                new Dictionary<string, IEnumerable<string>> { { "consolidatorAreaId", new[] { "El área consolidadora no existe." } } },
                HttpStatusCode.NotFound);
        }

        ExpenseObject? exp = null;
        if (dto.ExpenseObjectId.HasValue)
        {
            exp = await _unitOfWork.Context.ExpenseObjects.FindAsync(dto.ExpenseObjectId.Value);
        }

        AccountingAccount? acc = null;
        if (dto.AccountingAccountId.HasValue)
        {
            acc = await _unitOfWork.Context.AccountingAccounts.FindAsync(dto.AccountingAccountId.Value);
        }

        var prefix = dto.UnspscPrefix.Trim();
        var products = await _unitOfWork.Context.MasterProducts
            .Where(p => p.OrganizationId == organizationId && p.DeletedAt == null && p.UnspscCode != null && p.UnspscCode.StartsWith(prefix))
            .ToListAsync();

        var count = 0;
        foreach (var p in products)
        {
            p.ConsolidatorAreaId = area.Id;
            p.ConsolidatorAreaName = area.Name;
            p.ConsolidatorAreaSource = ConsolidatorAreaSources.Manual;
            p.ConsolidatorRuleId = null;
            p.ConsolidatorAreaConfidence = 100;
            p.AreaAmbiguity = false;
            p.AlternativeAreasJson = null;
            p.RequiresReview = false;
            p.ReviewReason = null;

            if (exp is not null)
            {
                p.ExpenseObjectId = exp.Id;
                p.ExpenseObjectCode = exp.Code;
                p.ExpenseObjectName = exp.Name;
            }

            if (acc is not null)
            {
                p.AccountingAccountId = acc.Id;
                p.AccountingAccountCode = acc.Code;
                p.AccountingAccountName = acc.Name;
            }

            if (p.State == MasterProductStates.Pending)
            {
                p.State = MasterProductStates.Active;
            }

            p.UpdatedAt = DateTime.UtcNow;
            count++;
        }

        await _unitOfWork.Context.SaveChangesAsync();

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Catálogos",
            "Ratificación masiva por UNSPSC",
            $"Ratificó masivamente {count} artículos de la rama UNSPSC '{prefix}' a '{area.Name}'",
            "MasterProduct",
            Guid.Empty);

        return HttpResult<int>.Success(count);
    }

    public async Task<IHttpResult<MasterProductDetailDto>> SetStateAsync(int organizationId, Guid id, string state, Guid userId, string? userName)
    {
        var product = await _unitOfWork.Context.MasterProducts
            .Include(p => p.Aliases)
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == id && p.DeletedAt == null);

        if (product is null)
        {
            return HttpResult<MasterProductDetailDto>.Failure(
                "Artículo no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { "El artículo especificado no me existe." } } },
                HttpStatusCode.NotFound);
        }

        product.State = state;
        product.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Context.SaveChangesAsync();

        return HttpResult<MasterProductDetailDto>.Success(MapDetailDto(product));
    }

    public async Task<IHttpResult<MasterProductDetailDto>> RequestProductAsync(int organizationId, RequestMasterProductDto dto, Guid userId, string? userName, Guid? userAreaId, string? userAreaName)
    {
        var seqNumber = await _sequenceService.NextAsync(organizationId, SequenceScope, "GLOBAL");
        var code = $"ART-REQ-{seqNumber:D6}";

        var searchText = ExcelTableReader.Normalize($"{dto.ProposedName} {dto.Justification}");

        var product = new MasterProduct
        {
            OrganizationId = organizationId,
            Code = code,
            Name = dto.ProposedName.Trim(),
            SearchText = searchText,
            ItemType = dto.ItemType?.Trim() ?? "Bien",
            MeasureUnit = dto.MeasureUnit?.Trim() ?? "Unidad",
            ConsolidatorAreaSource = "None",
            State = MasterProductStates.Requested,
            RequestedBy = userId,
            RequestedByName = userName,
            RequestedAreaId = userAreaId,
            RequestedAt = DateTime.UtcNow,
            RequestJustification = dto.Justification.Trim(),
            RequiresReview = true,
            ReviewReason = "Artículo solicitado por el usuario del POA",
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _unitOfWork.Context.MasterProducts.Add(product);
        await _unitOfWork.Context.SaveChangesAsync();

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Catálogos",
            "Solicitó nuevo artículo",
            $"Solicitó la creación del artículo '{product.Name}' para el POA",
            "MasterProduct",
            product.Id);

        return HttpResult<MasterProductDetailDto>.Success(MapDetailDto(product));
    }

    public async Task<IHttpResult<MasterProductMappingSummaryDto>> GetMappingSummaryAsync(int organizationId)
    {
        var products = await _unitOfWork.Context.MasterProducts
            .AsNoTracking()
            .Where(p => p.OrganizationId == organizationId && p.DeletedAt == null)
            .ToListAsync();

        var total = products.Count;
        var active = products.Count(p => p.State == MasterProductStates.Active);
        var pending = products.Count(p => p.State == MasterProductStates.Pending);
        var requested = products.Count(p => p.State == MasterProductStates.Requested);
        var disabled = products.Count(p => p.State == MasterProductStates.Disabled);

        var missingExp = products.Count(p => p.ExpenseObjectId == null);
        var missingAcc = products.Count(p => p.AccountingAccountId == null);
        var missingArea = products.Count(p => p.ConsolidatorAreaId == null);

        var requiresReview = products.Count(p => p.RequiresReview);
        var ambiguity = products.Count(p => p.AreaAmbiguity);

        var complete = products.Count(p => p.ExpenseObjectId != null && p.AccountingAccountId != null && p.ConsolidatorAreaId != null && !p.RequiresReview && !p.AreaAmbiguity);

        var summary = new MasterProductMappingSummaryDto
        {
            TotalProducts = total,
            ActiveProducts = active,
            PendingProducts = pending,
            RequestedProducts = requested,
            DisabledProducts = disabled,
            MissingExpenseObject = missingExp,
            MissingAccountingAccount = missingAcc,
            MissingConsolidatorArea = missingArea,
            RequiresReviewCount = requiresReview,
            AreaAmbiguityCount = ambiguity,
            CompleteMappingCount = complete
        };

        return HttpResult<MasterProductMappingSummaryDto>.Success(summary);
    }

    private static MasterProductDto MapDto(MasterProduct p) => new()
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

    private static MasterProductDetailDto MapDetailDto(MasterProduct p) => new()
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
        UpdatedAt = p.UpdatedAt,
        Aliases = p.Aliases.Select(a => new MasterProductAliasDto
        {
            Id = a.Id,
            MasterProductId = a.MasterProductId,
            Kind = a.Kind,
            System = a.System,
            Text = a.Text
        }).ToList()
    };
}
