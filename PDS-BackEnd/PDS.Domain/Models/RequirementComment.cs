using System;
using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Comentario append-only de un requerimiento (M7).
/// </summary>
public class RequirementComment
{
    public Guid Id { get; set; }
    public Guid RequirementId { get; set; }

    public Guid UserId { get; set; }
    [MaxLength(250)]
    public string? UserName { get; set; }
    [MaxLength(250)]
    public string? AreaName { get; set; }

    public DateTime Date { get; set; }

    [MaxLength(2000)]
    public required string Text { get; set; }

    public virtual Requirement? Requirement { get; set; }
}
