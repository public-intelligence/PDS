namespace PDS.Service.Authorization;

/// <summary>
/// Permission strings del calendario laboral de localidades. Viajan en el claim "permissions"
/// del JWT de GOBID y se validan con ICalendarioAccessGuard.
/// </summary>
public static class LocalidadPermissions
{
    /// <summary>Administración del calendario en la organización; también autoriza cambiar la zona horaria de una localidad (RF-LO-20).</summary>
    public const string CalendarManage = "localities:calendar:manage";

    /// <summary>Consultar la configuración y el calendario efectivo.</summary>
    public const string CalendarRead = "localities:calendar:read";

    /// <summary>Todos los permisos de localidades (para registrarlos en GOBID).</summary>
    public static readonly IReadOnlyCollection<string> All = [CalendarManage, CalendarRead];
}
