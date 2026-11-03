namespace PDS.Domain.DTOs.OrganizationalStructureAreatype;

public record CreateOrganizationalStructureAreaTypeDto
{
    public required string Name { get; set; }
    public Guid? ParentId { get; set; }
    public required int Order { get; set; } 
}
