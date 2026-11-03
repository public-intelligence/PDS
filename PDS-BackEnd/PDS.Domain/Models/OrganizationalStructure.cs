using System;
using System.Collections.Generic;
using System.Text;

namespace PDS.Domain.Models;

public class OrganizationalStructure : ISoftDeletable
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }
    public Guid? ParentId { get; set; }
    public Guid AreaTypeId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public virtual OrganizationalStructure? Parent { get; set; }
    public virtual required OrganizationalStructureAreaType AreaType { get; set; }

    public virtual ICollection<OrganizationalStructure> Dependencies { get; } = [];
    public ICollection<Position> Positions { get; } = [];
}
