using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PDS.Service.Settings;

namespace PDS.Service.Services.Calendario;

/// <summary>
/// RF-LO-43: retención del feed de cambios. Una vez al día (<see cref="CalendarioSettings.PurgaCambiosIntervaloHoras"/>) elimina las filas
/// más antiguas que <see cref="CalendarioSettings.RetencionCambiosDias"/>, que es lo que permite responder 410 a un cursor vencido.
/// No hace nada mientras el calendario esté deshabilitado (las tablas pueden no existir aún) ni si <see cref="CalendarioSettings.PurgaCambiosHabilitada"/> es false.
/// Un fallo se registra y se reintenta en el siguiente ciclo; nunca tumba el proceso.
/// </summary>
public sealed class CalendarioCambiosPurgaService(
    IServiceScopeFactory scopes,
    IOptions<CalendarioSettings> settings,
    TimeProvider clock,
    ILogger<CalendarioCambiosPurgaService> logger) : BackgroundService
{
    /// <summary>Una pasada de purga. Devuelve las filas eliminadas, o <c>null</c> si está deshabilitada o falló.</summary>
    public async Task<int?> EjecutarUnaVezAsync(CancellationToken ct = default)
    {
        var s = settings.Value;
        if (!s.Habilitado || !s.PurgaCambiosHabilitada) return null;

        try
        {
            using var scope = scopes.CreateScope();
            var cambios = scope.ServiceProvider.GetRequiredService<ICalendarioCambioService>();
            var eliminadas = await cambios.PurgarExpiradosAsync(ct);
            if (eliminadas > 0)
                logger.LogInformation("Feed de cambios del calendario: {Eliminadas} filas anteriores a la retención de {Dias} días eliminadas.", eliminadas, s.RetencionCambiosDias);
            return eliminadas;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "No se pudo purgar el feed de cambios del calendario; se reintentará en el siguiente ciclo.");
            return null;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalo = TimeSpan.FromHours(Math.Max(1, settings.Value.PurgaCambiosIntervaloHoras));
        using var timer = new PeriodicTimer(intervalo, clock);
        try
        {
            do
            {
                await EjecutarUnaVezAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // apagado normal
        }
    }
}
