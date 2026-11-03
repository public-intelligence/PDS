using Microsoft.EntityFrameworkCore;
using PDS.Application.Data;
using PDS.Application.Repositories;
using PDS.Domain.Models;

namespace PDS.Service.Authorization;

/// <summary>
/// RF-LO-39 / RF-LO-38. El administrador de calendario (<c>localities:calendar:manage</c>) puede todo. Un gestor de calendario
/// de la localidad <i>L</i> es quien llama si y solo si:
/// <list type="bullet">
/// <item>existe un <see cref="LocalidadResponsable"/> activo (no eliminado) de <i>L</i> con <c>GestionaCalendario = true</c>;</item>
/// <item>el <see cref="Responsable"/> vinculado tiene <c>GobidUserId</c> igual al <c>sub</c> del token (<c>userId</c>), no está
///       eliminado y es de la organización del token;</item>
/// <item><i>L</i> es de la organización del token, no está eliminada y su estado no es terminal
///       (<see cref="EstadoLocalidad.EsTerminal"/>: INACTIVA; EN_CIERRE solo lo es si el catálogo así lo marca).</item>
/// </list>
/// Depende de la base de datos (registro scoped).
/// </summary>
public sealed class CalendarioAccessGuard(IUnitOfWork<PDSDbContext> uow) : ICalendarioAccessGuard
{
    private PDSDbContext Ctx => uow.Context;

    public bool CanRead(IReadOnlyCollection<string> permissions)
        => Has(permissions, LocalidadPermissions.CalendarRead) || CanManage(permissions);

    public bool CanManage(IReadOnlyCollection<string> permissions)
        => Has(permissions, LocalidadPermissions.CalendarManage);

    public async Task<bool> PuedeGestionarLocalidadAsync(
        IReadOnlyCollection<string> permissions, Guid localidadId, Guid userId, int organizationId, CancellationToken ct = default)
        => CanManage(permissions) || await EsGestorAsync(localidadId, userId, organizationId, ct);

    public async Task<bool> PuedeGestionarCalendarioFeriadosAsync(
        IReadOnlyCollection<string> permissions, Guid calendarioFeriadosId, Guid? localidadGestoraId, Guid userId, int organizationId,
        CancellationToken ct = default)
    {
        if (CanManage(permissions)) return true;
        // §10.4: el gestor de la localidad gestora mantiene las ENTRADAS del calendario (no la cabecera ni su baja).
        return localidadGestoraId is { } gestora && await EsGestorAsync(gestora, userId, organizationId, ct);
    }

    public async Task<IReadOnlyList<Guid>> LocalidadesQueGestionaAsync(Guid userId, int organizationId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) return [];
        return await GestoresQuery(userId, organizationId)
            .Select(lr => lr.LocalidadId)
            .Distinct()
            .ToListAsync(ct);
    }

    private async Task<bool> EsGestorAsync(Guid localidadId, Guid userId, int organizationId, CancellationToken ct)
    {
        if (userId == Guid.Empty) return false;
        return await GestoresQuery(userId, organizationId).AnyAsync(lr => lr.LocalidadId == localidadId, ct);
    }

    private IQueryable<LocalidadResponsable> GestoresQuery(Guid userId, int organizationId)
        => Ctx.LocalidadResponsables.AsNoTracking().Where(lr =>
            lr.DeletedAt == null
            && lr.GestionaCalendario
            && lr.Responsable!.GobidUserId == userId
            && lr.Responsable.DeletedAt == null
            && lr.Responsable.OrganizationId == organizationId
            && lr.Localidad!.OrganizationId == organizationId
            && lr.Localidad.DeletedAt == null
            && !lr.Localidad.Estado!.EsTerminal);

    private static bool Has(IReadOnlyCollection<string> permissions, string permission)
        => permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
}
