using PDS.Domain.Enums;

namespace PDS.Domain.DTOs.LegalInstrument;

public record LegalInstrumentDto
{
    public Guid Id { get; init; }
    public LegalInstrumentType Type { get; init; }
    public required string Number { get; init; }
    public DateOnly Date { get; init; }
    public required string IssuingAuthority { get; init; }
    public string? DocumentUrl { get; init; }
}
