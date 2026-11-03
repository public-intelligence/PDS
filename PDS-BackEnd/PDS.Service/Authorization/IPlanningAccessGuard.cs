using PDS.Domain.DTOs.Planning;

namespace PDS.Service.Authorization;

/// <summary>
/// Checks de autorización del módulo de Planificación. Los controllers extraen los claims
/// (ApiControllerBase) y los servicios validan con estos helpers antes de mutar.
/// </summary>
public interface IPlanningAccessGuard
{
    /// <summary>True si el contexto tiene el permiso indicado o planning:admin.</summary>
    bool HasPermission(PlanningContextDto context, string permission);

    /// <summary>
    /// True si el contexto puede actuar sobre recursos del área indicada: tiene el permiso y
    /// el área es la suya o un ancestro que califica para planificar, o bien tiene planning:admin.
    /// </summary>
    bool CanActOnArea(PlanningContextDto context, string permission, Guid? resourceAreaId);

    /// <summary>
    /// True si el contexto puede consolidar requerimientos del área consolidadora indicada:
    /// tiene planning:requirements:consolidate y pertenece a esa consolidadora, o planning:admin.
    /// Falla cerrado cuando el área es nula o el usuario no pertenece a ninguna consolidadora.
    /// </summary>
    bool CanConsolidateForArea(PlanningContextDto context, Guid? consolidatorAreaId);
}
