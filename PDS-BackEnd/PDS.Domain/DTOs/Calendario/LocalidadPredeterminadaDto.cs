namespace PDS.Domain.DTOs.Calendario;

/// <summary>
/// RF-LO-36: localidad predeterminada de calendario de una organización (M2M). Es la localidad de respaldo de WF y SDS y la plantilla
/// de horario de las localidades nuevas.
/// </summary>
public sealed class LocalidadPredeterminadaDto
{
    public Guid LocalidadId { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string NombreOficial { get; init; } = string.Empty;
    public string ZonaHoraria { get; init; } = string.Empty;
    public long CalendarioVersion { get; init; }
}

/// <summary>RF-LO-40: una localidad de la que quien llama puede mantener el calendario. <c>Rol</c>: <c>Admin</c> o <c>Gestor</c>.</summary>
public class LocalidadGestionableDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string NombreOficial { get; set; } = string.Empty;
    public string? Pais { get; set; }
    public string? Ciudad { get; set; }
    public string ZonaHoraria { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}
