namespace PDS.Domain.Models;

/// <summary>
/// Relación N:M entre una localidad y un instrumento legal existente (RF-LO-01). El propósito indica
/// para qué respalda el instrumento (creación, autorización, modificación, suspensión, cierre).
/// </summary>
public class LocalidadInstrumentoLegal
{
    public Guid Id { get; set; }
    public Guid LocalidadId { get; set; }
    public Guid InstrumentoLegalId { get; set; }
    public string? Proposito { get; set; }

    public Localidad? Localidad { get; set; }
    public LegalInstrument? InstrumentoLegal { get; set; }
}
