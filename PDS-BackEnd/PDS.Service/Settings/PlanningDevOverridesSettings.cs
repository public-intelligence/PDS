namespace PDS.Service.Settings;

/// <summary>
/// Overrides de desarrollo/pruebas del contexto de Planificación: permite que usuarios concretos
/// obtengan todos los permisos planning:* y/o simular pertenecer a un área específica.
/// Enabled debe quedar en false fuera de Development.
/// </summary>
public class PlanningDevOverridesSettings
{
    public const string SectionName = "PlanningDevOverrides";

    public bool Enabled { get; init; }
    public List<PlanningDevOverrideUser> Users { get; init; } = [];
}

public class PlanningDevOverrideUser
{
    public Guid UserId { get; init; }

    /// <summary>Otorga todos los permisos planning:* además de los del token.</summary>
    public bool GrantAllPermissions { get; init; }

    /// <summary>Si se define, fuerza el área de plan (nivel 1) del contexto — permite simular ser un área concreta.</summary>
    public Guid? PlanAreaId { get; init; }
}
