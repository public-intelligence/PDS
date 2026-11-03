using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDS.Application.Data;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Models;
using PDS.Domain.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace PDS.Service.Services.Planning;

/// <inheritdoc cref="IUnifiedCodeService"/>
public class UnifiedCodeService : IUnifiedCodeService
{
    private readonly IUnitOfWork<PDSDbContext> _unitOfWork;
    private readonly ILogger<UnifiedCodeService> _logger;

    public UnifiedCodeService(
        IUnitOfWork<PDSDbContext> unitOfWork,
        ILogger<UnifiedCodeService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IHttpResult<IEnumerable<UnifiedCodeDto>>> ListAsync(
        int organizationId, string? level, Guid? parentId, string? q, bool? enabled, int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            var query = _unitOfWork.Context.UnifiedCodes
                .AsNoTracking()
                .Where(c => c.OrganizationId == organizationId && c.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(level))
            {
                query = query.Where(c => c.Level == level);
            }

            if (parentId.HasValue)
            {
                query = query.Where(c => c.ParentId == parentId.Value);
            }
            else if (string.IsNullOrWhiteSpace(q) && string.IsNullOrWhiteSpace(level))
            {
                // En la raíz solo se muestran los Segmentos (Level == "Segment")
                query = query.Where(c => c.Level == "Segment");
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(term) || c.Code.ToLower().Contains(term));
            }

            if (enabled.HasValue)
            {
                query = query.Where(c => c.InstitutionallyEnabled == enabled.Value);
            }

            var size = Math.Clamp(pageSize, 1, 500);
            var skip = Math.Max(0, (pageNumber - 1) * size);

            var dtos = await query
                .OrderBy(c => c.Code)
                .Skip(skip)
                .Take(size)
                .Select(c => new UnifiedCodeDto
                {
                    Id = c.Id,
                    Level = c.Level,
                    Code = c.Code,
                    Name = c.Name,
                    ParentId = c.ParentId,
                    EnglishName = c.EnglishName,
                    InstitutionallyEnabled = c.InstitutionallyEnabled,
                    ChildCount = c.Children.Count(ch => ch.DeletedAt == null)
                })
                .ToListAsync();

            return HttpResult<IEnumerable<UnifiedCodeDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar códigos unificados UNSPSC.");
            return HttpResult<IEnumerable<UnifiedCodeDto>>.Failure(
                "Error al obtener códigos unificados.",
                new Dictionary<string, IEnumerable<string>> { { "error", new[] { ex.Message } } },
                HttpStatusCode.InternalServerError);
        }
    }

    public async Task<IHttpResult<PagedResponseDto<UnifiedCodeDto>>> ListPagedAsync(
        int organizationId, string? level = null, Guid? parentId = null, string? codePrefix = null, string? q = null, bool? enabled = null, int page = 1, int pageSize = 50)
    {
        try
        {
            var query = _unitOfWork.Context.UnifiedCodes
                .AsNoTracking()
                .Where(c => c.OrganizationId == organizationId && c.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(level))
            {
                query = query.Where(c => c.Level == level);
            }

            if (parentId.HasValue)
            {
                query = query.Where(c => c.ParentId == parentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(codePrefix))
            {
                var prefix = codePrefix.Trim();
                query = query.Where(c => c.Code.StartsWith(prefix));
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(c => EF.Functions.Like(c.Name, $"%{term}%") || EF.Functions.Like(c.Code, $"%{term}%"));
            }

            if (enabled.HasValue)
            {
                query = query.Where(c => c.InstitutionallyEnabled == enabled.Value);
            }

            var clampedPageSize = Math.Clamp(pageSize, 1, 250);
            var pageNumber = Math.Max(1, page);
            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderBy(c => c.Code)
                .Skip((pageNumber - 1) * clampedPageSize)
                .Take(clampedPageSize)
                .Select(c => new UnifiedCodeDto
                {
                    Id = c.Id,
                    Level = c.Level,
                    Code = c.Code,
                    Name = c.Name,
                    ParentId = c.ParentId,
                    EnglishName = c.EnglishName,
                    InstitutionallyEnabled = c.InstitutionallyEnabled,
                    ChildCount = _unitOfWork.Context.UnifiedCodes.Count(ch => ch.ParentId == c.Id && ch.DeletedAt == null)
                })
                .ToListAsync();

            var pagedResponse = new PagedResponseDto<UnifiedCodeDto>(items, pageNumber, clampedPageSize, totalRecords);
            return HttpResult<PagedResponseDto<UnifiedCodeDto>>.Success(pagedResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar catálogo de códigos UNSPSC con paginación M2M.");
            return HttpResult<PagedResponseDto<UnifiedCodeDto>>.Failure(
                $"Error al consultar catálogo UNSPSC: {ex.Message}",
                new Dictionary<string, IEnumerable<string>> { { "error", new[] { ex.ToString() } } },
                HttpStatusCode.InternalServerError);
        }
    }

    public async Task<IHttpResult<UnifiedCodeDetailDto>> GetByIdAsync(int organizationId, Guid id)
    {
        var c = await _unitOfWork.Context.UnifiedCodes
            .AsNoTracking()
            .Include(code => code.Aliases)
            .FirstOrDefaultAsync(code => code.OrganizationId == organizationId && code.Id == id && code.DeletedAt == null);

        if (c == null)
            return HttpResult<UnifiedCodeDetailDto>.Failure(
                "Código unificado no encontrado.",
                new Dictionary<string, IEnumerable<string>> { { "error", new[] { "Código no encontrado" } } },
                HttpStatusCode.NotFound);

        var dto = new UnifiedCodeDetailDto
        {
            Id = c.Id,
            Level = c.Level,
            Code = c.Code,
            Name = c.Name,
            ParentId = c.ParentId,
            EnglishName = c.EnglishName,
            Definition = c.Definition,
            SourceRole = c.SourceRole,
            IncludedInMaster = c.IncludedInMaster,
            LegacyExpenseObjectCode = c.LegacyExpenseObjectCode,
            LegacyExpenseObjectName = c.LegacyExpenseObjectName,
            Source = c.Source,
            InstitutionallyEnabled = c.InstitutionallyEnabled,
            Aliases = c.Aliases.Select(a => new UnifiedCodeAliasDto
            {
                Id = a.Id,
                System = a.System,
                Code = a.Code,
                Name = a.Name
            }).ToList()
        };

        return HttpResult<UnifiedCodeDetailDto>.Success(dto);
    }
}
