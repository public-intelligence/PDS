using PDS.Domain.DTOs.StructureVersion;

namespace PDS.Domain.DTOs.OrgChart;

/// <summary>
/// Respuesta del organigrama: la cabecera de la versión + la lista plana de nodos.
/// </summary>
public record OrgChartDto
{
    public required StructureVersionSummaryDto Version { get; init; }
    public ICollection<OrgChartNodeDto> Nodes { get; init; } = [];
}
