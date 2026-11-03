using PDS.Domain.DTOs.Reporting;

namespace PDS.Service.Services.Reporting;

/// <summary>Líneas de etiqueta del contexto y de generación, compartidas por todos los renderers.</summary>
internal static class ReportLabelFormatter
{
    public static string ContextLine(ReportContextLabelDto c)
    {
        var version = c.VersionNumber.HasValue ? $"Versión {c.VersionNumber}" : "Vista previa";
        var period = c.EffectiveFrom.HasValue
            ? $" · Vigencia desde {c.EffectiveFrom:yyyy-MM-dd}" +
              (c.EffectiveTo.HasValue ? $" hasta {c.EffectiveTo:yyyy-MM-dd}" : " (vigente)")
            : string.Empty;
        return $"{version} — {c.VersionName} ({c.Status}){period} · Corte: {c.ResolvedAsOf:yyyy-MM-dd}";
    }

    public static string GeneratedLine(ReportContextLabelDto c)
        => $"Generado: {c.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC" +
           (string.IsNullOrWhiteSpace(c.GeneratedByName) ? string.Empty : $" · Por: {c.GeneratedByName}");
}
