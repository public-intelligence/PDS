namespace PDS.Domain.Models;

public class OrganizationalStructureAreaType : ISoftDeletable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid? ParentId { get; set; }
    public int Order { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public OrganizationalStructureAreaType? Parent { get; set; }

    public ICollection<OrganizationalStructureAreaType> Children { get; } = [];
    public ICollection<OrganizationalStructure> OrganizationalStructures { get; } = [];
}