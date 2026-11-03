using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PDS.Application.Data;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Models;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

public class FundingSourceService : IFundingSourceService
{
    private readonly IUnitOfWork<PDSDbContext> _unitOfWork;
    private readonly IPlanningActivityService _activityService;

    public FundingSourceService(
        IUnitOfWork<PDSDbContext> unitOfWork,
        IPlanningActivityService activityService)
    {
        _unitOfWork = unitOfWork;
        _activityService = activityService;
    }

    private static readonly (string Code, string Name, string Description)[] StandardFundingSources =
    [
        ("1", "Fuentes internas", "Recursos que provienen del propio Estado o de la gestión financiera interna de la institución."),
        ("10", "Fondo General", "Recursos del presupuesto nacional que financian el gasto general de la institución."),
        ("20", "Fondos con destino específico", "Ingresos internos que deben usarse para un fin concreto definido por norma o por su origen."),
        ("30", "Fondos propios", "Recursos generados por la propia entidad, como cobros, ventas, tasas o servicios."),
        ("40", "Transferencias", "Recursos recibidos desde otra entidad pública."),
        ("50", "Crédito interno", "Financiamiento obtenido dentro del país mediante deuda interna."),
        ("90", "Fondos de terceros", "Recursos que la institución administra, pero que pertenecen a otra persona o entidad."),
        ("91", "Pasivos financieros", "Recursos asociados a obligaciones financieras registradas como pasivos."),
        ("99", "Fondos contables", "Recursos de naturaleza contable o de registro, no necesariamente ligados a ingreso operativo directo."),
        ("2", "Fuentes externas", "Recursos que provienen del exterior del país."),
        ("60", "Crédito externo", "Préstamos o financiamiento reembolsable proveniente del exterior."),
        ("70", "Donación externa", "Cooperación externa no reembolsable.")
    ];

    private async Task EnsureSeededAsync(int organizationId)
    {
        var exists = await _unitOfWork.Context.FundingSources
            .AnyAsync(f => f.OrganizationId == organizationId && f.DeletedAt == null);

        if (!exists)
        {
            var now = DateTime.UtcNow;
            foreach (var (code, name, desc) in StandardFundingSources)
            {
                _unitOfWork.Context.FundingSources.Add(new FundingSource
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Code = code,
                    Name = name,
                    Description = desc,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            await _unitOfWork.Context.SaveChangesAsync();
        }
    }

    public async Task<IHttpResult<IEnumerable<FundingSourceDto>>> ListAsync(int organizationId, string? q = null, bool? onlyActive = null)
    {
        await EnsureSeededAsync(organizationId);

        var query = _unitOfWork.Context.FundingSources
            .AsNoTracking()
            .Where(f => f.OrganizationId == organizationId && f.DeletedAt == null);

        if (onlyActive == true)
        {
            query = query.Where(f => f.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(f => f.Code.ToLower().Contains(term) || f.Name.ToLower().Contains(term));
        }

        var items = await query
            .OrderBy(f => f.Code)
            .Select(f => MapToDto(f))
            .ToListAsync();

        return HttpResult<IEnumerable<FundingSourceDto>>.Success(items);
    }

    public async Task<IHttpResult<FundingSourceDto>> GetByIdAsync(int organizationId, Guid id)
    {
        var item = await _unitOfWork.Context.FundingSources
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.OrganizationId == organizationId && f.Id == id && f.DeletedAt == null);

        if (item is null)
        {
            return HttpResult<FundingSourceDto>.Failure(
                "Fuente de financiamiento no encontrada",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { $"La fuente de financiamiento '{id}' no existe." } } },
                HttpStatusCode.NotFound);
        }

        return HttpResult<FundingSourceDto>.Success(MapToDto(item));
    }

    public async Task<IHttpResult<FundingSourceDto>> CreateAsync(int organizationId, CreateFundingSourceDto dto, Guid userId, string? userName)
    {
        var codeClean = dto.Code.Trim();
        var codeExists = await _unitOfWork.Context.FundingSources
            .AnyAsync(f => f.OrganizationId == organizationId && f.Code == codeClean && f.DeletedAt == null);

        if (codeExists)
        {
            return HttpResult<FundingSourceDto>.Failure(
                "Código duplicado",
                new Dictionary<string, IEnumerable<string>> { { "code", new[] { $"Ya existe una fuente de financiamiento con el código '{codeClean}'." } } },
                HttpStatusCode.Conflict);
        }

        var entity = new FundingSource
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Code = codeClean,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _unitOfWork.Context.FundingSources.Add(entity);
        await _unitOfWork.Context.SaveChangesAsync();

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Catálogos",
            "Creó fuente de financiamiento",
            $"Creó la fuente '{entity.Code} - {entity.Name}'",
            "FundingSource",
            entity.Id);

        return HttpResult<FundingSourceDto>.Success(MapToDto(entity), HttpStatusCode.Created);
    }

    public async Task<IHttpResult<FundingSourceDto>> UpdateAsync(int organizationId, Guid id, UpdateFundingSourceDto dto, Guid userId, string? userName)
    {
        var entity = await _unitOfWork.Context.FundingSources
            .FirstOrDefaultAsync(f => f.OrganizationId == organizationId && f.Id == id && f.DeletedAt == null);

        if (entity is null)
        {
            return HttpResult<FundingSourceDto>.Failure(
                "Fuente de financiamiento no encontrada",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { $"La fuente con ID '{id}' no existe." } } },
                HttpStatusCode.NotFound);
        }

        var codeClean = dto.Code.Trim();
        if (entity.Code != codeClean)
        {
            var codeExists = await _unitOfWork.Context.FundingSources
                .AnyAsync(f => f.OrganizationId == organizationId && f.Code == codeClean && f.Id != id && f.DeletedAt == null);
            if (codeExists)
            {
                return HttpResult<FundingSourceDto>.Failure(
                    "Código duplicado",
                    new Dictionary<string, IEnumerable<string>> { { "code", new[] { $"Ya existe otra fuente con el código '{codeClean}'." } } },
                    HttpStatusCode.Conflict);
            }
        }

        entity.Code = codeClean;
        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description?.Trim();
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Context.SaveChangesAsync();

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Catálogos",
            "Actualizó fuente de financiamiento",
            $"Actualizó la fuente '{entity.Code} - {entity.Name}'",
            "FundingSource",
            entity.Id);

        return HttpResult<FundingSourceDto>.Success(MapToDto(entity));
    }

    public async Task<IHttpResult> DeleteAsync(int organizationId, Guid id, Guid userId, string? userName)
    {
        var entity = await _unitOfWork.Context.FundingSources
            .FirstOrDefaultAsync(f => f.OrganizationId == organizationId && f.Id == id && f.DeletedAt == null);

        if (entity is null)
        {
            return HttpResult.Failure(
                "Fuente de financiamiento no encontrada",
                new Dictionary<string, IEnumerable<string>> { { "id", new[] { $"La fuente con ID '{id}' no existe." } } },
                HttpStatusCode.NotFound);
        }

        entity.DeletedAt = DateTime.UtcNow;
        await _unitOfWork.Context.SaveChangesAsync();

        await _activityService.LogAsync(
            organizationId,
            userId,
            userName,
            "Catálogos",
            "Eliminó fuente de financiamiento",
            $"Eliminó la fuente '{entity.Code} - {entity.Name}'",
            "FundingSource",
            entity.Id);

        return HttpResult.Success();
    }

    private static FundingSourceDto MapToDto(FundingSource entity) => new()
    {
        Id = entity.Id,
        OrganizationId = entity.OrganizationId,
        Code = entity.Code,
        Name = entity.Name,
        Description = entity.Description,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
