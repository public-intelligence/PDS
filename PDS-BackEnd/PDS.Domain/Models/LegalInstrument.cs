using PDS.Domain.Enums;

namespace PDS.Domain.Models;

/// <summary>
/// Instrumento legal (decreto, resolución, ley, otro) que respalda una publicación (RF-VH-04).
/// El documento se referencia por enlace/URL (PDS no maneja almacenamiento de archivos).
/// </summary>
public class LegalInstrument : ISoftDeletable
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    public LegalInstrumentType Type { get; set; }
    public required string Number { get; set; }
    public DateOnly Date { get; set; }
    public required string IssuingAuthority { get; set; }

    /// <summary>Enlace/URL al documento del instrumento legal.</summary>
    public string? DocumentUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
