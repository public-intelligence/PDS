using PDS.Domain.Enums;
using PDS.Domain.ValueObjects;

namespace PDS.Domain.Models;

/// <summary>
/// Excepción del calendario de una localidad (RF-LO-31, RF-LO-32): cierre, jornada especial o día laborable adicional.
/// Las de sistema (<see cref="EsSistema"/>) las generan suspensión y cierre de la localidad y no se editan por la API.
/// <see cref="FechaFin"/> nula solo es válida en excepciones de sistema abiertas.
/// </summary>
public class LocalidadExcepcionCalendario : ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid LocalidadId { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public TipoExcepcionCalendario Tipo { get; set; }
    public CategoriaExcepcionCalendario Categoria { get; set; }
    public OrigenExcepcionCalendario Origen { get; set; }
    /// <summary>JSON; nulo para <see cref="TipoExcepcionCalendario.Cierre"/>.</summary>
    public List<IntervaloLaboral>? Intervalos { get; set; }
    public required string Motivo { get; set; }
    public string? ReferenciaNormativa { get; set; }
    public Guid? InstrumentoLegalId { get; set; }
    public bool EsSistema { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public byte[]? RowVersion { get; set; }
    public Guid CreadoPorId { get; set; }
    public string? CreadoPor { get; set; }
    public Guid? ModificadoPorId { get; set; }
    public string? ModificadoPor { get; set; }

    public Localidad? Localidad { get; set; }
    public LegalInstrument? InstrumentoLegal { get; set; }
}
