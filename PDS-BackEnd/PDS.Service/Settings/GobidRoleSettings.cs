namespace PDS.Service.Settings;

/// <summary>
/// Configuración para la creación de roles por posición en GOBID.
/// El OrganizationApplicationId se toma de la configuración/variables de entorno
/// (p. ej. <c>GobidRoles__OrganizationApplicationId</c>) para que sea dinámico por ambiente.
/// El host (BaseUrl) y la versión del API se toman de GobidM2M.
/// </summary>
public class GobidRoleSettings
{
    public const string SectionName = "GobidRoles";

    public int OrganizationApplicationId { get; init; }
}
