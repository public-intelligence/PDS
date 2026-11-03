namespace PDS.Domain.DTOs.OrganizationalStructureAreatype;

public record OrganizationalStructureAreaTypeDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public Guid? ParentId { get; init; }
    public int Order { get; init; }
}