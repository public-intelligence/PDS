using PDS.Domain.DTOs.OrganizationalStructureAreatype;

namespace PDS.Domain.DTOs.OrganizationalStructure;

public record OrganizationalStructureWhioutDependenciesDto
{
    public Guid Id { get; set; }
    public Guid? AreaTypeId { get; init; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Parent { get; init; }
    public OrganizationalStructureAreaTypeDto? AreaType { get; init; }
}
