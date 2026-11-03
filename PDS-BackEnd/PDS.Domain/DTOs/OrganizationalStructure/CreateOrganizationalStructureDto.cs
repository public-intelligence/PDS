namespace PDS.Domain.DTOs.OrganizationalStructure;

public record CreateOrganizationalStructureDto
{
    public Guid? ParentId { get; init; }

    public string Name { get; init; } = null!;
    public Guid AreaTypeId { get; init; }
}