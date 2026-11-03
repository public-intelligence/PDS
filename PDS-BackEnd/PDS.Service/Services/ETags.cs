using System.Security.Cryptography;
using System.Text;

namespace PDS.Service.Services;

/// <summary>ETag fuertes y comparación <c>If-None-Match</c> (RFC 9110) para los endpoints M2M cacheables.</summary>
public static class ETags
{
    /// <summary>ETag fuerte (entre comillas) derivado del contenido: cambia cuando cambia cualquiera de las partes.</summary>
    public static string FromParts(params string?[] parts)
    {
        var payload = string.Join('\u001f', parts.Select(p => p ?? string.Empty));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return $"\"{Convert.ToHexString(hash, 0, 16).ToLowerInvariant()}\"";
    }

    /// <summary>
    /// Verdadero si <paramref name="ifNoneMatch"/> (valor del encabezado, posiblemente con varias entradas, <c>*</c> o
    /// prefijo débil <c>W/</c>) incluye <paramref name="etag"/>. Para <c>If-None-Match</c> se usa comparación débil.
    /// </summary>
    public static bool Matches(string? ifNoneMatch, string etag)
    {
        if (string.IsNullOrWhiteSpace(ifNoneMatch)) return false;
        var current = Strip(etag);
        foreach (var raw in ifNoneMatch.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (raw == "*") return true;
            if (Strip(raw) == current) return true;
        }
        return false;

        static string Strip(string value) => value.StartsWith("W/", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
    }

    /// <summary>ETag fuerte de una fila con <c>rowversion</c> (RF-LO-42); nulo si la fila aún no tiene versión.</summary>
    public static string? FromRowVersion(byte[]? rowVersion) =>
        rowVersion is { Length: > 0 } ? $"\"{Convert.ToHexString(rowVersion).ToLowerInvariant()}\"" : null;

    /// <summary>
    /// RF-LO-42: evalúa <c>If-Match</c> (comparación fuerte). Sin encabezado (o <c>*</c>) se acepta; con encabezado, debe incluir
    /// el ETag vigente. Falso => la fila cambió desde que el cliente la leyó (412).
    /// </summary>
    public static bool IfMatchSatisfied(string? ifMatch, string? currentEtag)
    {
        if (string.IsNullOrWhiteSpace(ifMatch)) return true;
        foreach (var raw in ifMatch.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (raw == "*") return true;
            if (currentEtag is not null && raw == currentEtag) return true;
        }
        return false;
    }
}
