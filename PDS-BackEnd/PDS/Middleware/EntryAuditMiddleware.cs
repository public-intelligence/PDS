using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using PDS.Application.Data;
using PDS.Application.Repositories;
using PDS.Domain.Models;
using PDS.Service.Services.Planning;

namespace PDS.Middleware;

/// <summary>
/// Registra la primera petición autenticada de cada sesión: quién entró, desde qué área y
/// cuándo. Nunca bloquea ni ralentiza la operación: si algo falla se registra una advertencia
/// y la petición continúa.
/// </summary>
public class EntryAuditMiddleware
{
    private static readonly TimeSpan SessionWindow = TimeSpan.FromHours(12);

    private readonly RequestDelegate _next;

    public EntryAuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IMemoryCache cache,
        IServiceProvider serviceProvider,
        ILogger<EntryAuditMiddleware> logger)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await TryRecordEntryAsync(context, cache, serviceProvider, logger);
        }

        await _next(context);
    }

    private static async Task TryRecordEntryAsync(
        HttpContext context,
        IMemoryCache cache,
        IServiceProvider serviceProvider,
        ILogger<EntryAuditMiddleware> logger)
    {
        try
        {
            var user = context.User;

            // Las cuentas de servicio no "entran al sistema": su trazabilidad es la del sistema llamante.
            if (string.Equals(user.FindFirstValue("is_service_account"), "true", StringComparison.OrdinalIgnoreCase))
                return;

            var rawUserId = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(rawUserId, out var userId) || userId == Guid.Empty)
                return;

            var sessionId = user.FindFirstValue("session_id") ?? user.FindFirstValue("jti");
            if (string.IsNullOrWhiteSpace(sessionId))
                return;

            var cacheKey = $"entry-audit:{userId}:{sessionId}";
            if (cache.TryGetValue(cacheKey, out _))
                return;

            _ = int.TryParse(user.FindFirstValue("organization_id"), out var organizationId);
            var userName = user.FindFirstValue("person_full_name") ?? user.FindFirstValue(ClaimTypes.Name);
            var personnelId = user.FindFirstValue("personnel_id") ?? user.FindFirstValue("personnelId");

            using var scope = serviceProvider.CreateScope();
            var contextService = scope.ServiceProvider.GetRequiredService<IPlanningContextService>();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<SystemEntryLog>>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<PDSDbContext>>();

            Guid? areaId = null;
            string? areaName = null;
            string? positionName = null;

            var permissionClaims = user.FindAll("permissions").Select(c => c.Value).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
            var permissionsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pVal in permissionClaims)
            {
                var trimmed = pVal.Trim();
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    try
                    {
                        var parsed = System.Text.Json.JsonSerializer.Deserialize<string[]>(trimmed);
                        if (parsed is not null) foreach (var p in parsed) if (!string.IsNullOrWhiteSpace(p)) permissionsSet.Add(p.Trim());
                    }
                    catch (System.Text.Json.JsonException) { permissionsSet.Add(trimmed); }
                }
                else permissionsSet.Add(trimmed);
            }

            var planningContext = await contextService.GetContextAsync(
                organizationId, userId, userName, personnelId, permissionsSet.Count > 0 ? permissionsSet : null, context.RequestAborted);

            if (planningContext.IsSuccess && planningContext.Value is not null)
            {
                areaId = planningContext.Value.UnitAreaId ?? planningContext.Value.PlanAreaId;
                areaName = planningContext.Value.UnitAreaName ?? planningContext.Value.PlanAreaName;
                positionName = planningContext.Value.PositionName;
            }

            var userAgent = context.Request.Headers.UserAgent.ToString();

            await repository.CreateAsync(new SystemEntryLog
            {
                OrganizationId = organizationId,
                UserId = userId,
                UserName = userName,
                SessionId = sessionId,
                AreaId = areaId,
                AreaName = areaName,
                PositionName = positionName,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = userAgent.Length > 500 ? userAgent[..500] : userAgent,
                EnteredAtUtc = DateTime.UtcNow
            });

            await uow.CommitAsync();

            // Se marca después de guardar: un fallo transitorio no debe silenciar la sesión.
            cache.Set(cacheKey, true, SessionWindow);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo registrar la entrada al sistema; la petición continúa.");
        }
    }
}
