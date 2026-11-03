namespace PDS.Domain.Models;

/// <summary>
/// Horario especial o excepción de una localidad (RF-LO-01). Hijo de <see cref="Localidad"/>.
/// </summary>
public class LocalidadHorarioExcepcion
{
    public Guid Id { get; set; }
    public Guid LocalidadId { get; set; }
    public required string Descripcion { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public string? Horario { get; set; }

    public Localidad? Localidad { get; set; }
}
