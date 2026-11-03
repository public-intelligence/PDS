using PDS.Domain.DTOs.Planning;

namespace PDS.Service.Authorization;

public sealed class PlanningAccessGuard : IPlanningAccessGuard
{
    public bool HasPermission(PlanningContextDto context, string permission)
        => context.Permissions.Contains(PlanningPermissions.Admin, StringComparer.OrdinalIgnoreCase)
            || context.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

    public bool CanActOnArea(PlanningContextDto context, string permission, Guid? resourceAreaId)
    {
        if (context.Permissions.Contains(PlanningPermissions.Admin, StringComparer.OrdinalIgnoreCase))
            return true;

        if (!context.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
            return false;

        if (resourceAreaId is null)
            return false;

        // El área propia o cualquier ancestro que califique para planificar: el dueño de un
        // plan de nivel 2 debe poder editarlo aunque su "área de plan" sea el nivel 1.
        return context.PlanAreaId == resourceAreaId
            || context.EligibleAreaIds.Contains(resourceAreaId.Value);
    }

    public bool CanConsolidateForArea(PlanningContextDto context, Guid? consolidatorAreaId)
    {
        if (context.Permissions.Contains(PlanningPermissions.Admin, StringComparer.OrdinalIgnoreCase))
            return true;

        if (!context.Permissions.Contains(PlanningPermissions.RequirementsConsolidate, StringComparer.OrdinalIgnoreCase))
            return false;

        // Falla cerrado: sin área consolidadora resuelta en el requerimiento, o sin pertenencia
        // del usuario a ninguna, no se consolida. Tener el permiso no basta.
        if (consolidatorAreaId is null || consolidatorAreaId == Guid.Empty)
            return false;

        return context.ConsolidatorAreaIds.Contains(consolidatorAreaId.Value);
    }
}
