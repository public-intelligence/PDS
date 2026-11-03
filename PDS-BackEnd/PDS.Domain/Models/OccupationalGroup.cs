namespace PDS.Domain.Models;

public class OccupationalGroup : ISoftDeletable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<Position>? Positions { get; set; }
}