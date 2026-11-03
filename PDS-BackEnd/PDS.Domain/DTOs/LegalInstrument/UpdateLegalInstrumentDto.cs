using PDS.Domain.Enums;

namespace PDS.Domain.DTOs.LegalInstrument;

public record UpdateLegalInstrumentDto
{
    public LegalInstrumentType Type { get; init; }
    public required string Number { get; init; }
    public DateOnly Date { get; init; }
    public required string IssuingAuthority { get; init; }
    public string? DocumentUrl { get; init; }
}
