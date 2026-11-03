using PDS.Domain.DTOs.OrganizationalStructureAreatype;
using PDS.Domain.DTOs.Position;

namespace PDS.Domain.DTOs.OrganizationalStructure;

public record OrganizationalStructureDto
{
    public Guid Id { get; set; }
    public Guid? AreaTypeId { get; init; }

    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int? PersonnelCount { get; set; }
    public string? Parent { get; init; }
    public OrganizationalStructureAreaTypeDto? AreaType { get; init; }
    public ICollection<PositionDto> Positions { get; set; } = [];
    public ICollection<OrganizationalStructureDto> Dependencies { get; set; } = [];
}