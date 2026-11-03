namespace PDS.Service.Settings;

/// <summary>
/// Configuración del endpoint de GOBID que devuelve el registro de personal (Personnel) por id.
/// La ruta es configurable porque el contrato exacto de GOBID puede cambiar sin tocar código.
/// El host (BaseUrl) se toma de GobidM2M; aquí solo vive la ruta relativa.
/// </summary>
public class GobidPersonnelSettings
{
    public const string SectionName = "GobidPersonnel";

    /// <summary>
    /// Plantilla de la ruta relativa. Placeholders: {apiVersion} y {personnelId}.
    /// Default: "api/{apiVersion}/Personnel/{personnelId}".
    /// </summary>
    public string PathTemplate { get; init; } = "api/{apiVersion}/Personnel/{personnelId}";
}
