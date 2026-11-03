using System.Text.RegularExpressions;
using NodaTime.TimeZones;

namespace PDS.Service.Services.Calendario;

/// <summary>Versión de la base de datos de zonas horarias IANA embebida en NodaTime (RNF-LO-04: se expone a los consumidores).</summary>
public static partial class TzdbInfo
{
    /// <summary>
    /// Versión de la tzdb, por ejemplo <c>2025b</c>. NodaTime la reporta como «TZDB: 2025b (mapping: …)»; se extrae el
    /// identificador de versión y, si el formato cambiara, se devuelve el texto completo.
    /// </summary>
    public static string Version { get; } = Parse(TzdbDateTimeZoneSource.Default.VersionId);

    public static string Parse(string versionId)
    {
        var m = VersionRegex().Match(versionId);
        return m.Success ? m.Value : versionId.Trim();
    }

    [GeneratedRegex(@"\b\d{4}[a-z]\b")]
    private static partial Regex VersionRegex();
}
