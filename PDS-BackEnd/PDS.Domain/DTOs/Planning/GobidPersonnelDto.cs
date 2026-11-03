namespace PDS.Domain.DTOs.Planning;

/// <summary>
/// Resumen normalizado del registro de personal que devuelve GOBID (Personnel API).
/// El contrato exacto de GOBID puede variar; el cliente lo parsea de forma defensiva y
/// solo conserva lo que PDS necesita: el identificador del cargo (Position) para resolver el área.
/// </summary>
public record GobidPersonnelDto
{
    public required string PersonnelId { get; init; }

    /// <summary>
    /// Identificador del cargo tal como lo devuelve GOBID (texto crudo). Debe hacer match
    /// con Position.Id (Guid) de la BD de PDS; como plan B se intenta contra Position.Code (int).
    /// </summary>
    public string? PositionId { get; init; }

    public string? PositionName { get; init; }
    public string? FullName { get; init; }

    /// <summary>
    /// Unidad organizacional del cargo, tal como la devuelve GOBID en
    /// `position.organizationalStructure`. Es la vía PREFERENTE para resolver el área: su id
    /// coincide con OrganizationalStructure.Id de PDS, mientras que el positionId de GOBID no
    /// siempre existe en la tabla Positions local (verificado en dev el 2026-08-05).
    /// </summary>
    public string? OrganizationalStructureId { get; init; }

    public string? OrganizationalStructureName { get; init; }
}
