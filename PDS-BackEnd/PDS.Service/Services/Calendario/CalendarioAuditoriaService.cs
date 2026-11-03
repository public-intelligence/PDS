using PDS.Application.Data;
using PDS.Application.Repositories;
using PDS.Domain.Enums;
using PDS.Domain.Models;

namespace PDS.Service.Services.Calendario;

/// <summary>
/// RF-LO-44: registra la bitácora de cambios del calendario. Igual que <see cref="ICalendarioCambioService"/>, NO llama a
/// <c>SaveChanges</c>: las filas se persisten con la misma transacción que la mutación. Cada endpoint de escritura debe producir
/// exactamente UNA fila por agregado afectado (localidad o calendario de feriados).
/// </summary>
public interface ICalendarioAuditoriaService
{
    /// <summary>Cambio de calendario a nivel de localidad: <c>ScheduleChanged</c>, <c>HolidaySubscriptionsChanged</c>, <c>CalendarException*</c>.</summary>
    LocalidadAuditoria RegistrarLocalidad(
        int organizationId, Guid localidadId, LocalidadAccion accion, Guid usuarioId, string? usuario, CanalCambio canal,
        string? motivo = null, string? detalle = null, Guid? instrumentoLegalId = null);

    /// <summary>Cambio en un calendario de feriados (cabecera o entradas). Ver <see cref="CalendarioFeriadosAcciones"/>.</summary>
    CalendarioFeriadosAuditoria RegistrarFeriados(
        int organizationId, Guid calendarioFeriadosId, string accion, Guid usuarioId, string? usuario, CanalCambio canal,
        string? motivo = null, string? detalle = null);
}

public class CalendarioAuditoriaService(IUnitOfWork<PDSDbContext> uow, TimeProvider clock) : ICalendarioAuditoriaService
{
    private PDSDbContext Ctx => uow.Context;

    public LocalidadAuditoria RegistrarLocalidad(
        int organizationId, Guid localidadId, LocalidadAccion accion, Guid usuarioId, string? usuario, CanalCambio canal,
        string? motivo = null, string? detalle = null, Guid? instrumentoLegalId = null)
    {
        var fila = new LocalidadAuditoria
        {
            LocalidadId = localidadId,
            OrganizationId = organizationId,
            Accion = accion,
            UsuarioId = usuarioId,
            Usuario = usuario,
            Fecha = clock.GetUtcNow().UtcDateTime,
            Motivo = motivo,
            Detalle = detalle,
            InstrumentoLegalId = instrumentoLegalId,
            Canal = canal,
        };
        Ctx.LocalidadAuditorias.Add(fila);
        return fila;
    }

    public CalendarioFeriadosAuditoria RegistrarFeriados(
        int organizationId, Guid calendarioFeriadosId, string accion, Guid usuarioId, string? usuario, CanalCambio canal,
        string? motivo = null, string? detalle = null)
    {
        var fila = new CalendarioFeriadosAuditoria
        {
            CalendarioFeriadosId = calendarioFeriadosId,
            OrganizationId = organizationId,
            Accion = accion,
            Motivo = motivo,
            Detalle = detalle,
            UsuarioId = usuarioId,
            Usuario = usuario,
            Canal = canal,
            Fecha = clock.GetUtcNow().UtcDateTime,
        };
        Ctx.CalendarioFeriadosAuditorias.Add(fila);
        return fila;
    }
}
