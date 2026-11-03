using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using PDS.Service.Settings;

namespace PDS.Filters;

/// <summary>
/// Se aplica a las acciones que escriben el calendario laboral. Si el flag Calendario:Habilitado
/// está apagado responde 404 sin ejecutar la acción.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireCalendarioHabilitadoAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var settings = context.HttpContext.RequestServices.GetRequiredService<IOptionsSnapshot<CalendarioSettings>>();
        if (!settings.Value.Habilitado)
        {
            context.Result = new NotFoundObjectResult(new
            {
                errors = new[] { "El calendario laboral de localidades no está habilitado." }
            });
            return;
        }

        await next();
    }
}
