using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Models;
using PDS.Domain.Response;
using System.Net;

namespace PDS.Service.Services.Planning;

public sealed class PlanningActivityService : IPlanningActivityService
{
    private readonly IRepository<PlanningActivityLog> _activityRepository;
    private readonly IPaginationService _paginationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPlanningContextService _contextService;
    private readonly IMapper _mapper;
    private readonly ILogger<PlanningActivityService> _logger;

    public PlanningActivityService(
        IRepository<PlanningActivityLog> activityRepository,
        IPaginationService paginationService,
        IHttpContextAccessor httpContextAccessor,
        IPlanningContextService contextService,
        IMapper mapper,
        ILogger<PlanningActivityService> logger)
    {
        _activityRepository = activityRepository;
        _paginationService = paginationService;
        _httpContextAccessor = httpContextAccessor;
        _contextService = contextService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Área desde la que se ejecuta la acción. Se apoya en el contexto ya resuelto en la
    /// petición (caché de 15 min), así que no añade viajes a GOBID. Devuelve nulos cuando no
    /// hay petición HTTP (tareas de fondo) o no se pudo resolver: la bitácora se escribe igual.
    /// </summary>
    private async Task<(Guid? AreaId, string? AreaName)> ResolveActorAreaAsync(int organizationId, Guid userId, string? userName)
    {
        var http = _httpContextAccessor.HttpContext;
        if (http?.User?.Identity?.IsAuthenticated != true)
            return (null, null);

        try
        {
            var personnelId = http.User.FindFirst("personnel_id")?.Value
                ?? http.User.FindFirst("personnelId")?.Value;

            var result = await _contextService.GetContextAsync(organizationId, userId, userName, personnelId, null);
            if (!result.IsSuccess || result.Value is null)
                return (null, null);

            return (result.Value.UnitAreaId ?? result.Value.PlanAreaId,
                    result.Value.UnitAreaName ?? result.Value.PlanAreaName);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "No se pudo resolver el área del actor para la bitácora.");
            return (null, null);
        }
    }

    public async Task LogAsync(
        int organizationId,
        Guid userId,
        string? userName,
        string module,
        string action,
        string? detail = null,
        string? entityType = null,
        Guid? entityId = null)
    {
        try
        {
            var (actorAreaId, actorAreaName) = await ResolveActorAreaAsync(organizationId, userId, userName);

            var entry = new PlanningActivityLog
            {
                OrganizationId = organizationId,
                UserId = userId,
                UserName = userName,
                ActorAreaId = actorAreaId,
                ActorAreaName = actorAreaName,
                Module = module,
                Action = action,
                Detail = detail,
                EntityType = entityType,
                EntityId = entityId
            };

            var result = await _activityRepository.CreateAsync(entry);
            if (!result.IsSuccess)
                _logger.LogWarning("No se pudo registrar la actividad de planificación: {Error}", result.Error);
        }
        catch (Exception ex)
        {
            // La bitácora nunca rompe la operación de negocio que la origina.
            _logger.LogError(ex, "Error inesperado registrando actividad de planificación.");
        }
    }

    public async Task<IHttpResult<PagedResponseDto<ActivityLogDto>>> ListAsync(
        int organizationId,
        PaginationFilterDto pagination,
        HttpContext httpContext,
        string? module = null,
        Guid? entityId = null)
    {
        var countResult = await _activityRepository.CountAsync(a =>
            a.OrganizationId == organizationId
            && (module == null || a.Module == module)
            && (entityId == null || a.EntityId == entityId));
        if (!countResult.IsSuccess)
            return Fail("No se pudo contar la actividad de planificación");

        var pageResult = await _activityRepository.FilterAsync(
            a => a.OrganizationId == organizationId
                && (module == null || a.Module == module)
                && (entityId == null || a.EntityId == entityId),
            query => query
                .OrderByDescending(a => a.Date)
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize));
        if (!pageResult.IsSuccess)
            return Fail("No se pudo listar la actividad de planificación");

        var dtos = _mapper.Map<IEnumerable<ActivityLogDto>>(pageResult.Value);
        var paged = _paginationService.BuildPaginationUrl(
            httpContext, pagination.PageNumber, pagination.PageSize, countResult.Value, dtos);

        return HttpResult<PagedResponseDto<ActivityLogDto>>.Success(paged);
    }

    private static HttpResult<PagedResponseDto<ActivityLogDto>> Fail(string message)
    {
        var detail = new Dictionary<string, IEnumerable<string>> { { "errors", [message] } };
        return HttpResult<PagedResponseDto<ActivityLogDto>>.Failure(
            "Error con la actividad de planificación", detail, HttpStatusCode.BadRequest);
    }
}
