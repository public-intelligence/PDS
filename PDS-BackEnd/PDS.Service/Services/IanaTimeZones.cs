using NodaTime;
using NodaTime.TimeZones;

namespace PDS.Service.Services;

/// <summary>Zona horaria IANA asociada a un país del catálogo tzdb.</summary>
/// <param name="Id">Identificador IANA (p. ej. <c>America/New_York</c>).</param>
/// <param name="PaisCodigo">Código ISO 3166-1 alfa-2 del país.</param>
/// <param name="Comentario">Comentario de tzdb (p. ej. "Eastern (most areas)"); vacío si no hay.</param>
public sealed record IanaZoneEntry(string Id, string PaisCodigo, string Comentario);

/// <summary>Validación y catálogo de identificadores de zona horaria IANA (RF-LO-20 / RF-LO-21).</summary>
public static class IanaTimeZones
{
    public const string Default = "America/Santo_Domingo";

    public static bool IsValid(string? zoneId)
        => !string.IsNullOrWhiteSpace(zoneId) && DateTimeZoneProviders.Tzdb.GetZoneOrNull(zoneId) is not null;

    /// <summary>Indica si el valor tiene forma de código de país ISO 3166-1 alfa-2 (dos letras).</summary>
    public static bool IsCountryCodeFormat(string? value)
        => value is { Length: 2 } && char.IsAsciiLetter(value[0]) && char.IsAsciiLetter(value[1]);

    /// <summary>
    /// Zonas horarias de tzdb por país, ordenadas por identificador y país. Con <paramref name="countryCode"/> nulo se
    /// devuelven todas; un código sin zonas (o desconocido) devuelve una lista vacía.
    /// </summary>
    public static IReadOnlyList<IanaZoneEntry> ForCountry(string? countryCode = null)
    {
        var code = countryCode?.Trim().ToUpperInvariant();
        return TzdbDateTimeZoneSource.Default.ZoneLocations!
            .Where(z => code is null || z.CountryCode == code)
            .Select(z => new IanaZoneEntry(z.ZoneId, z.CountryCode, z.Comment ?? string.Empty))
            .OrderBy(z => z.Id, StringComparer.Ordinal)
            .ThenBy(z => z.PaisCodigo, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Desplazamiento UTC vigente de una zona IANA en <paramref name="instant"/>, con formato <c>+hh:mm</c> / <c>-hh:mm</c>,
    /// y si en ese instante rige horario de verano (RF-LO-21). Devuelve <c>null</c> si la zona no existe.
    /// </summary>
    public static (string Offset, bool IsDaylightSaving)? CurrentOffset(string zoneId, Instant instant)
    {
        var zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(zoneId);
        if (zone is null) return null;

        var interval = zone.GetZoneInterval(instant);
        var totalMinutes = (int)interval.WallOffset.ToTimeSpan().TotalMinutes;
        var sign = totalMinutes < 0 ? '-' : '+';
        var abs = Math.Abs(totalMinutes);
        return ($"{sign}{abs / 60:00}:{abs % 60:00}", interval.Savings != Offset.Zero);
    }
}
