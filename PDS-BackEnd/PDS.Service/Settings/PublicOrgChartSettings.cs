namespace PDS.Service.Settings;

/// <summary>
/// Configuración del organigrama público. Como los endpoints públicos son anónimos (sin claim de
/// organización), la institución se resuelve desde aquí, con override opcional vía ?organizationId.
/// </summary>
public class PublicOrgChartSettings
{
    public const string SectionName = "PublicOrgChart";

    public int DefaultOrganizationId { get; init; }
}
