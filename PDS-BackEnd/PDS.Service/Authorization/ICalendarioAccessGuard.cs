namespace PDS.Service.Authorization;

/// <summary>Checks de autorización del calendario laboral de localidades.</summary>
public interface ICalendarioAccessGuard
{
    /// <summary>True si los permisos incluyen localities:calendar:read o manage.</summary>
    bool CanRead(IReadOnlyCollection<string> permissions);

    /// <summary>True si los permisos incluyen localities:calendar:manage (administrador de calendario, RF-LO-39).</summary>
    bool CanManage(IReadOnlyCollection<string> permissions);

    /// <summary>
    /// RF-LO-38: ¿puede quien llama mantener el calendario de la localidad (jornadas, suscripciones, excepciones)?
    /// Administrador de calendario o gestor de calendario de esa localidad.
    /// </summary>
    Task<bool> PuedeGestionarLocalidadAsync(
        IReadOnlyCollection<string> permissions, Guid localidadId, Guid userId, int organizationId, CancellationToken ct = default);

    /// <summary>
    /// RF-LO-38: ¿puede quien llama mantener las entradas (no la cabecera) de un calendario de feriados?
    /// Administrador de calendario o gestor de calendario de su localidad gestora.
    /// </summary>
    Task<bool> PuedeGestionarCalendarioFeriadosAsync(
        IReadOnlyCollection<string> permissions, Guid calendarioFeriadosId, Guid? localidadGestoraId, Guid userId, int organizationId,
        CancellationToken ct = default);

    /// <summary>
    /// RF-LO-40: ids de las localidades de las que quien llama es gestor de calendario (regla RF-LO-38, sin contar el permiso de
    /// administrador). Vacío si no es gestor de ninguna.
    /// </summary>
    Task<IReadOnlyList<Guid>> LocalidadesQueGestionaAsync(Guid userId, int organizationId, CancellationToken ct = default);
}
