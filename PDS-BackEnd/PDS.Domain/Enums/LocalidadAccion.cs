namespace PDS.Domain.Enums;

/// <summary>
/// Acción registrada en la bitácora (append-only) de una localidad institucional
/// (RF-LO-06, RF-LO-14). El valor se persiste como texto.
/// </summary>
public enum LocalidadAccion
{
    Created,
    Modified,
    Suspended,
    Reactivated,
    Closed,
    ScheduleChanged,
    HolidaySubscriptionsChanged,
    CalendarExceptionAdded,
    CalendarExceptionUpdated,
    CalendarExceptionRemoved
}
