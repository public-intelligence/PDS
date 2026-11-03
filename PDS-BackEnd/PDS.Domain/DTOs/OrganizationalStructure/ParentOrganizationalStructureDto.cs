namespace PDS.Domain.DTOs.OrganizationalStructure;

public record ParentOrganizationalStructureDto
{
    public Guid Id { get; init; }
    public Guid? AreaTypeId { get; init; }

    public required string Code { get; init; }
    public required string Name { get; init; }
}