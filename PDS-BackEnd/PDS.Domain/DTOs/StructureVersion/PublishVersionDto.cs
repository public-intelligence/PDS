using PDS.Domain.DTOs.LegalInstrument;

namespace PDS.Domain.DTOs.StructureVersion;

/// <summary>
/// Cuerpo de publicación. Debe traer un instrumento legal: o bien un Id existente
/// (<see cref="LegalInstrumentId"/>) o los datos para crearlo en línea (<see cref="LegalInstrument"/>).
/// </summary>
public record PublishVersionDto
{
    public DateOnly EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; init; }
    public bool IsPublic { get; init; }

    public Guid? LegalInstrumentId { get; init; }
    public CreateLegalInstrumentDto? LegalInstrument { get; init; }

    /// <summary>
    /// Override opcional de plazas autorizadas por cargo (PositionId → cantidad). Si no se indica,
    /// se arrastra el valor de la última versión publicada para ese cargo (o 1 si es nuevo).
    /// </summary>
    public Dictionary<Guid, int>? AuthorizedPosts { get; init; }
}
