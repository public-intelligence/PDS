namespace PDS.Domain.Models;

/// <summary>
/// Bitácora append-only del módulo de Planificación (M1): toda acción de negocio
/// (ciclos, PEI, POA, requerimientos, configuración) registra quién, qué y cuándo.
/// Equivalente persistido del registro de actividad del mock. Nunca se edita ni elimina.
/// </summary>
public class PlanningActivityLog
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    public DateTime Date { get; set; }

    public Guid UserId { get; set; }
    public string? UserName { get; set; }

    /// <summary>Área desde la que actuó el usuario (snapshot). Null si no se pudo resolver.</summary>
    public Guid? ActorAreaId { get; set; }
    public string? ActorAreaName { get; set; }

    /// <summary>Módulo funcional: Ciclos, PEI, PlanAnual, Requerimientos, Consolidacion, Configuracion.</summary>
    public required string Module { get; set; }

    /// <summary>Acción de negocio en pasado: "Publicó el ciclo", "Sometió el plan", etc.</summary>
    public required string Action { get; set; }

    public string? Detail { get; set; }

    /// <summary>Tipo y clave de la entidad afectada (referencia lógica, sin FK — la bitácora sobrevive a la entidad).</summary>
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
}
