using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Plan Estratégico Institucional — PEI (M3). Se versiona por enmiendas: como máximo una
/// versión Draft (en elaboración) y una Current (vigente) por organización; el resto queda
/// Historical e inmutable. El descarte de una enmienda es la ÚNICA ruta de borrado del grafo
/// (hard delete en cascada, solo en estado Draft, desde el servicio).
/// </summary>
public class StrategicPlan
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    [MaxLength(250)]
    public required string Name { get; set; }

    /// <summary>Período del plan (ej. 2025-2028). Las columnas de metas anuales se derivan de aquí.</summary>
    public int PeriodStartYear { get; set; }
    public int PeriodEndYear { get; set; }

    /// <summary>Número de versión (1, 2, 3…). Se muestra como "v{N}.0".</summary>
    public int VersionNumber { get; set; } = 1;

    /// <summary>Estado del versionado: Draft, Current, Historical.</summary>
    [MaxLength(20)]
    public required string Status { get; set; }

    /// <summary>Justificación de la enmienda (obligatoria al abrirla: modificar el PEI es un acto excepcional).</summary>
    [MaxLength(2000)]
    public string? Reason { get; set; }

    public Guid CreatedBy { get; set; }

    [MaxLength(250)]
    public string? CreatedByName { get; set; }

    public Guid? ApprovedBy { get; set; }

    [MaxLength(250)]
    public string? ApprovedByName { get; set; }

    public DateTime? ApprovedAt { get; set; }

    /// <summary>Versión de la que se clonó esta enmienda (trazabilidad entre versiones).</summary>
    public Guid? PreviousVersionId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<StrategicFocus> Focuses { get; } = [];
    public virtual ICollection<StrategicPlanChange> Changes { get; } = [];
}
