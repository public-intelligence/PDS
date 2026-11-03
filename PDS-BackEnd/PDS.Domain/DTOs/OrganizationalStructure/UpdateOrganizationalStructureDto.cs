namespace PDS.Domain.DTOs.OrganizationalStructure;

public record UpdateOrganizationalStructureDto
{
    public Guid? ParentId { get; init; }
    public Guid AreaTypeId { get; init; }

    public required string Name { get; init; }
}