using PDS.Domain.Enums;

namespace PDS.Domain.Models;

/// <summary>
/// Bitácora append-only del flujo de aprobación de una versión (RF-FA-01): quién creó, revisó,
/// aprobó/rechazó o publicó, con fecha y comentario.
/// </summary>
public class StructureVersionAuditLog
{
    public Guid Id { get; set; }
    public Guid StructureVersionId { get; set; }

    public VersionAction Action { get; set; }

    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime Date { get; set; }
    public string? Comment { get; set; }

    public virtual StructureVersion? StructureVersion { get; set; }
}
