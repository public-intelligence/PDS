using PDS.Domain.Enums;

namespace PDS.Domain.Models;

/// <summary>
/// Cabecera de una versión de la estructura organizativa. Lleva el flujo de aprobación,
/// el período de vigencia y el instrumento legal. La fotografía inmutable de la estructura
/// (StructureVersionUnit / StructureVersionPosition) se materializa únicamente al publicar.
/// No es eliminable: la integridad histórica se garantiza por no exponer ninguna ruta de borrado.
/// </summary>
public class StructureVersion
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    /// <summary>Número secuencial por organización, asignado al publicar (NULL mientras no se publica).</summary>
    public int? Number { get; set; }

    public required string Name { get; set; }
    public string? Description { get; set; }

    public VersionStatus Status { get; set; } = VersionStatus.Draft;

    /// <summary>Inicio de vigencia (solo fecha). Se fija al publicar.</summary>
    public DateOnly? EffectiveFrom { get; set; }

    /// <summary>Fin de vigencia (solo fecha). NULL = versión vigente actualmente.</summary>
    public DateOnly? EffectiveTo { get; set; }

    public Guid? LegalInstrumentId { get; set; }

    /// <summary>Habilita el acceso anónimo a través del organigrama público.</summary>
    public bool IsPublic { get; set; }

    // Trazabilidad del flujo de aprobación (RF-FA-01). No hay tabla de usuarios:
    // se guarda el Guid del claim "sub" y el nombre denormalizado para legibilidad.
    public Guid CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid? ReviewedBy { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public Guid? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public Guid? PublishedBy { get; set; }
    public string? PublishedByName { get; set; }
    public DateTime? PublishedAt { get; set; }

    public virtual LegalInstrument? LegalInstrument { get; set; }
    public virtual ICollection<StructureVersionUnit> Units { get; } = [];
    public virtual ICollection<StructureVersionPosition> Positions { get; } = [];
    public virtual ICollection<StructureVersionAuditLog> AuditLogs { get; } = [];
}
