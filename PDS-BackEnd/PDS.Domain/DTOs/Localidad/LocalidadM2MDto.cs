namespace PDS.Domain.DTOs.Localidad;

/// <summary>
/// Localidad para sistemas M2M (SDS): <c>GET /m2m/organization/{organizationId}/locality/{localityId}</c>.
/// </summary>
public record LocalidadM2MDto
{
    public Guid Id { get; init; }
    public required string Codigo { get; init; }
    public required string NombreOficial { get; init; }
    public required string DireccionCompleta { get; init; }
    public int OrganizationId { get; init; }

    /// <summary>
    /// <c>true</c> solo cuando el estado es ACTIVA. Suspendida temporal, en apertura, en cierre e inactiva se
    /// consideran no activas (la localidad no debe ofrecer citas).
    /// </summary>
    public bool Activa { get; init; }

    /// <summary>Identificador IANA de la zona horaria de la oficina (nunca un offset).</summary>
    public required string IanaTimeZone { get; init; }

    /// <summary>Última modificación de la localidad en UTC (ISO-8601), para cachés y revalidación.</summary>
    public required string Version { get; init; }
}
