using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Models;
using PDS.Domain.Response;

namespace PDS.Service.Services.Planning;

public class PlanningTemplateService : IPlanningTemplateService
{
    private readonly IRepository<PlanningTemplate> _templateRepository;
    private readonly IRepository<PlanningTemplateColumn> _columnRepository;
    private readonly IPlanningActivityService _activityService;
    private readonly ILogger<PlanningTemplateService> _logger;

    public PlanningTemplateService(
        IRepository<PlanningTemplate> templateRepository,
        IRepository<PlanningTemplateColumn> columnRepository,
        IPlanningActivityService activityService,
        ILogger<PlanningTemplateService> logger)
    {
        _templateRepository = templateRepository;
        _columnRepository = columnRepository;
        _activityService = activityService;
        _logger = logger;
    }

    public async Task<IHttpResult<IEnumerable<PlanningTemplateDto>>> ListAsync(int organizationId)
    {
        var templatesResult = await _templateRepository.FilterAsync(
            t => t.OrganizationId == organizationId && t.DeletedAt == null,
            q => q.Include(t => t.Columns.Where(c => c.DeletedAt == null)).OrderByDescending(t => t.IsActive).ThenBy(t => t.Name)
        );
        if (!templatesResult.IsSuccess)
            return Fail<IEnumerable<PlanningTemplateDto>>("No se pudieron listar las plantillas.");

        var dtos = templatesResult.Value.Select(t => new PlanningTemplateDto
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            Version = t.Version,
            IsActive = t.IsActive,
            ColumnCount = t.Columns.Count,
            CreatedAt = t.CreatedAt
        });

        return HttpResult<IEnumerable<PlanningTemplateDto>>.Success(dtos);
    }

    public async Task<IHttpResult<PlanningTemplateDetailDto>> GetByIdAsync(int organizationId, Guid id)
    {
        var templateResult = await _templateRepository.GetAsync(
            t => t.Id == id && t.OrganizationId == organizationId && t.DeletedAt == null,
            q => q.Include(t => t.Columns.Where(c => c.DeletedAt == null).OrderBy(c => c.Order))
        );

        if (templateResult.Value == null)
        {
            return Fail<PlanningTemplateDetailDto>("Plantilla no encontrada.", HttpStatusCode.NotFound);
        }

        var template = templateResult.Value;
        var dto = MapToDetailDto(template);
        return HttpResult<PlanningTemplateDetailDto>.Success(dto);
    }

    public async Task<IHttpResult<PlanningTemplateDetailDto>> CreateAsync(int organizationId, CreatePlanningTemplateDto dto, Guid userId, string? userName)
    {
        var existsResult = await _templateRepository.AnyAsync(t => t.OrganizationId == organizationId && t.Name == dto.Name);
        if (existsResult.IsSuccess && existsResult.Value)
        {
            return Fail<PlanningTemplateDetailDto>("Ya existe una plantilla con este nombre.");
        }

        var template = new PlanningTemplate
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = dto.Name,
            Description = dto.Description,
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createResult = await _templateRepository.CreateAsync(template);
        if (!createResult.IsSuccess)
            return Fail<PlanningTemplateDetailDto>("No se pudo crear la plantilla.");

        await _activityService.LogAsync(
            organizationId, userId, userName, "Configuracion", "Creó una plantilla",
            $"Plantilla creada: {template.Name}", "PlanningTemplate", template.Id);

        return HttpResult<PlanningTemplateDetailDto>.Success(MapToDetailDto(createResult.Value));
    }

    public async Task<IHttpResult<PlanningTemplateDetailDto>> UpdateAsync(int organizationId, Guid id, UpdatePlanningTemplateDto dto)
    {
        var templateResult = await _templateRepository.GetAsync(
            t => t.Id == id && t.OrganizationId == organizationId && t.DeletedAt == null,
            q => q.Include(t => t.Columns.Where(c => c.DeletedAt == null).OrderBy(c => c.Order))
        );

        if (templateResult.Value == null)
        {
            return Fail<PlanningTemplateDetailDto>("Plantilla no encontrada.", HttpStatusCode.NotFound);
        }

        var template = templateResult.Value;

        if (template.Name != dto.Name)
        {
            var existsResult = await _templateRepository.AnyAsync(t => t.OrganizationId == organizationId && t.Name == dto.Name && t.Id != id);
            if (existsResult.IsSuccess && existsResult.Value)
            {
                return Fail<PlanningTemplateDetailDto>("Ya existe otra plantilla con este nombre.");
            }
        }

        template.Name = dto.Name;
        template.Description = dto.Description;
        template.Version++;
        template.UpdatedAt = DateTime.UtcNow;

        await _templateRepository.UpdateAsync(template);

        return HttpResult<PlanningTemplateDetailDto>.Success(MapToDetailDto(template));
    }

    public async Task<IHttpResult<PlanningTemplateColumnDto>> AddColumnAsync(int organizationId, Guid templateId, CreatePlanningTemplateColumnDto dto, Guid userId, string? userName)
    {
        var templateResult = await _templateRepository.GetAsync(t => t.Id == templateId && t.OrganizationId == organizationId && t.DeletedAt == null);
        if (templateResult.Value == null)
        {
            return Fail<PlanningTemplateColumnDto>("Plantilla no encontrada.", HttpStatusCode.NotFound);
        }

        var technicalNameResult = await _columnRepository.AnyAsync(c => c.TemplateId == templateId && c.TechnicalName == dto.TechnicalName);
        if (technicalNameResult.IsSuccess && technicalNameResult.Value)
        {
            return Fail<PlanningTemplateColumnDto>("El nombre técnico ya existe en esta plantilla.");
        }

        var column = new PlanningTemplateColumn
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            VisibleName = dto.VisibleName,
            TechnicalName = dto.TechnicalName,
            Type = dto.Type,
            Required = dto.Required,
            Visible = dto.Visible,
            Origin = dto.Origin,
            OptionsJson = dto.OptionsJson,
            Order = dto.Order,
            Version = 1,
            CreatedBy = userId,
            CreatedByName = userName,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _columnRepository.CreateAsync(column);
        if (!createResult.IsSuccess)
            return Fail<PlanningTemplateColumnDto>("No se pudo crear la columna.");

        await _activityService.LogAsync(
            organizationId, userId, userName, "Configuracion", "Agregó una columna",
            $"Columna agregada: {column.VisibleName}", "PlanningTemplateColumn", column.Id);

        return HttpResult<PlanningTemplateColumnDto>.Success(MapToColumnDto(createResult.Value));
    }

    public async Task<IHttpResult<PlanningTemplateColumnDto>> UpdateColumnAsync(int organizationId, Guid templateId, Guid columnId, UpdatePlanningTemplateColumnDto dto)
    {
        var templateResult = await _templateRepository.GetAsync(t => t.Id == templateId && t.OrganizationId == organizationId && t.DeletedAt == null);
        if (templateResult.Value == null)
        {
            return Fail<PlanningTemplateColumnDto>("Plantilla no encontrada.", HttpStatusCode.NotFound);
        }

        var columnResult = await _columnRepository.GetAsync(c => c.Id == columnId && c.TemplateId == templateId && c.DeletedAt == null);
        if (columnResult.Value == null)
        {
            return Fail<PlanningTemplateColumnDto>("Columna no encontrada.", HttpStatusCode.NotFound);
        }

        var column = columnResult.Value;

        column.VisibleName = dto.VisibleName;
        column.Type = dto.Type;
        column.Required = dto.Required;
        column.Visible = dto.Visible;
        column.OptionsJson = dto.OptionsJson;
        column.Order = dto.Order;
        column.Version++;

        await _columnRepository.UpdateAsync(column);

        return HttpResult<PlanningTemplateColumnDto>.Success(MapToColumnDto(column));
    }

    public async Task<IHttpResult<PlanningTemplateDetailDto>> ActivateAsync(int organizationId, Guid id, Guid userId, string? userName)
    {
        var templateResult = await _templateRepository.GetAsync(
            t => t.Id == id && t.OrganizationId == organizationId && t.DeletedAt == null,
            q => q.Include(t => t.Columns.Where(c => c.DeletedAt == null).OrderBy(c => c.Order))
        );

        if (templateResult.Value == null)
        {
            return Fail<PlanningTemplateDetailDto>("Plantilla no encontrada.", HttpStatusCode.NotFound);
        }

        var template = templateResult.Value;

        var activeTemplatesResult = await _templateRepository.FilterAsync(t => t.OrganizationId == organizationId && t.IsActive);
        if (activeTemplatesResult.IsSuccess)
        {
            foreach (var activeTemplate in activeTemplatesResult.Value)
            {
                if (activeTemplate.Id != id)
                {
                    activeTemplate.IsActive = false;
                    await _templateRepository.UpdateAsync(activeTemplate);
                }
            }
        }

        template.IsActive = true;
        await _templateRepository.UpdateAsync(template);

        await _activityService.LogAsync(
            organizationId, userId, userName, "Configuracion", "Activó una plantilla",
            $"Plantilla activada: {template.Name}", "PlanningTemplate", template.Id);

        return HttpResult<PlanningTemplateDetailDto>.Success(MapToDetailDto(template));
    }

    public async Task<IHttpResult<PlanningTemplateDetailDto>> SeedStandardColumnsAsync(
        int organizationId, Guid templateId, Guid userId, string? userName)
    {
        var templateResult = await _templateRepository.GetAsync(
            t => t.Id == templateId && t.OrganizationId == organizationId && t.DeletedAt == null,
            q => q.Include(t => t.Columns.Where(c => c.DeletedAt == null))
        );

        if (templateResult.Value == null)
        {
            return Fail<PlanningTemplateDetailDto>("Plantilla no encontrada.", HttpStatusCode.NotFound);
        }

        var template = templateResult.Value;

        var standardSpecs = new (string VisibleName, string TechnicalName, string Type, bool Required, int Order)[]
        {
            ("Foco estratégico", "focoEstrategico", "Select", false, 1),
            ("Objetivo estratégico", "objetivoEstrategico", "Select", false, 2),
            ("Actividad estratégica", "actividadEstrategica", "Select", false, 3),
            ("Producto", "producto", "Text", true, 4),
            ("Unidad de medida", "unidadMedida", "Text", true, 5),
            ("Meta", "meta", "Number", true, 6),
            ("Municipios de impacto", "municipiosImpacto", "Select", false, 7),
            ("Medios de verificación", "mediosVerificacion", "Text", false, 8),
            ("Tareas", "tareas", "Text", false, 9),
            ("Responsable", "responsable", "Select", false, 10),
            ("Área involucrada", "areaInvolucrada", "Select", false, 11),
            ("Cronograma", "cronograma", "Text", false, 12),
            ("Programación de metas", "programacionMetas", "Text", false, 13)
        };

        var existingColumns = template.Columns.ToList();

        foreach (var spec in standardSpecs)
        {
            var col = existingColumns.FirstOrDefault(c =>
                c.TechnicalName.Equals(spec.TechnicalName, StringComparison.OrdinalIgnoreCase) ||
                c.VisibleName.Equals(spec.VisibleName, StringComparison.OrdinalIgnoreCase));

            if (col != null)
            {
                col.VisibleName = spec.VisibleName;
                col.TechnicalName = spec.TechnicalName;
                col.Type = spec.Type;
                col.Required = spec.Required;
                col.Order = spec.Order;
                col.Origin = "Base";
                await _columnRepository.UpdateAsync(col);
            }
            else
            {
                var newCol = new PlanningTemplateColumn
                {
                    Id = Guid.NewGuid(),
                    TemplateId = templateId,
                    VisibleName = spec.VisibleName,
                    TechnicalName = spec.TechnicalName,
                    Type = spec.Type,
                    Required = spec.Required,
                    Visible = true,
                    Origin = "Base",
                    Order = spec.Order,
                    Version = 1,
                    CreatedBy = userId,
                    CreatedByName = userName,
                    CreatedAt = DateTime.UtcNow
                };
                await _columnRepository.CreateAsync(newCol);
            }
        }

        template.Version++;
        template.UpdatedAt = DateTime.UtcNow;
        await _templateRepository.UpdateAsync(template);

        await _activityService.LogAsync(
            organizationId, userId, userName, "Configuracion", "Pobló columnas estándar",
            $"Plantilla poblada con las 13 columnas estándar: {template.Name}", "PlanningTemplate", template.Id);

        var refreshed = await _templateRepository.GetAsync(
            t => t.Id == templateId && t.OrganizationId == organizationId,
            q => q.Include(t => t.Columns.Where(c => c.DeletedAt == null).OrderBy(c => c.Order))
        );

        return HttpResult<PlanningTemplateDetailDto>.Success(MapToDetailDto(refreshed.Value!));
    }

    public async Task<IHttpResult<PlanningTemplateDetailDto>> DeleteColumnAsync(
        int organizationId, Guid templateId, Guid columnId, Guid userId, string? userName)
    {
        var templateResult = await _templateRepository.GetAsync(
            t => t.Id == templateId && t.OrganizationId == organizationId && t.DeletedAt == null,
            q => q.Include(t => t.Columns.Where(c => c.DeletedAt == null))
        );

        if (templateResult.Value == null)
        {
            return Fail<PlanningTemplateDetailDto>("Plantilla no encontrada.", HttpStatusCode.NotFound);
        }

        var template = templateResult.Value;
        var column = template.Columns.FirstOrDefault(c => c.Id == columnId && c.DeletedAt == null);

        if (column == null)
        {
            return Fail<PlanningTemplateDetailDto>("Columna no encontrada.", HttpStatusCode.NotFound);
        }

        column.DeletedAt = DateTime.UtcNow;
        await _columnRepository.UpdateAsync(column);

        template.Version++;
        template.UpdatedAt = DateTime.UtcNow;
        await _templateRepository.UpdateAsync(template);

        await _activityService.LogAsync(
            organizationId, userId, userName, "Configuracion", "Eliminó una columna",
            $"Columna eliminada: {column.VisibleName}", "PlanningTemplateColumn", column.Id);

        var refreshed = await _templateRepository.GetAsync(
            t => t.Id == templateId && t.OrganizationId == organizationId,
            q => q.Include(t => t.Columns.Where(c => c.DeletedAt == null).OrderBy(c => c.Order))
        );

        return HttpResult<PlanningTemplateDetailDto>.Success(MapToDetailDto(refreshed.Value!));
    }

    public async Task<IHttpResult<bool>> DeleteAsync(int organizationId, Guid id, Guid userId, string? userName)
    {
        var templateResult = await _templateRepository.GetAsync(
            t => t.Id == id && t.OrganizationId == organizationId && t.DeletedAt == null,
            q => q.Include(t => t.Columns.Where(c => c.DeletedAt == null))
        );

        if (templateResult.Value == null)
        {
            return Fail<bool>("Plantilla no encontrada.", HttpStatusCode.NotFound);
        }

        var template = templateResult.Value;
        template.DeletedAt = DateTime.UtcNow;
        template.IsActive = false;
        template.UpdatedAt = DateTime.UtcNow;

        foreach (var col in template.Columns)
        {
            col.DeletedAt = DateTime.UtcNow;
            await _columnRepository.UpdateAsync(col);
        }

        await _templateRepository.UpdateAsync(template);

        await _activityService.LogAsync(
            organizationId, userId, userName, "Configuracion", "Eliminó la plantilla",
            $"Plantilla eliminada (soft delete): {template.Name}", "PlanningTemplate", template.Id);

        return HttpResult<bool>.Success(true);
    }

    private static PlanningTemplateDetailDto MapToDetailDto(PlanningTemplate template)
    {
        return new PlanningTemplateDetailDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Version = template.Version,
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt,
            Columns = template.Columns?.Select(MapToColumnDto).ToList() ?? []
        };
    }

    private static PlanningTemplateColumnDto MapToColumnDto(PlanningTemplateColumn column)
    {
        return new PlanningTemplateColumnDto
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
            CreatedBy = column.CreatedBy,
            CreatedByName = column.CreatedByName,
            CreatedAt = column.CreatedAt
        };
    }

    private static HttpResult<T> Fail<T>(string message, HttpStatusCode status = HttpStatusCode.BadRequest)
    {
        var detail = new Dictionary<string, IEnumerable<string>> { { "errors", [message] } };
        return HttpResult<T>.Failure(message, detail, status);
    }
}
