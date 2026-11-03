using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Columna configurable de una plantilla del plan anual (M2). Puede ser de origen Base
/// (definida por el sistema) o Custom (creada por el usuario). El tipo determina el
/// control de entrada en el frontend (texto, numérico, moneda, selector).
/// </summary>
public class PlanningTemplateColumn : ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }

    [MaxLength(150)]
    public required string VisibleName { get; set; }

    /// <summary>Nombre técnico (slug) para identificar la columna en exportaciones y lógica.</summary>
    [MaxLength(100)]
    public required string TechnicalName { get; set; }

    /// <summary>Tipo de dato: Text, Number, Currency, Select.</summary>
    [MaxLength(20)]
    public required string Type { get; set; }

    public bool Required { get; set; }
    public bool Visible { get; set; } = true;

    /// <summary>Origen de la columna: Base (del sistema) o Custom (creada por el usuario).</summary>
    [MaxLength(20)]
    public required string Origin { get; set; }

    /// <summary>Opciones del selector en JSON (solo para Type=Select). Ej: ["Alto","Medio","Bajo"].</summary>
    public string? OptionsJson { get; set; }

    /// <summary>Orden de aparición en el formulario.</summary>
    public int Order { get; set; }

    /// <summary>Versión de la columna (se incrementa en cada edición).</summary>
    public int Version { get; set; } = 1;

    public Guid? CreatedBy { get; set; }

    [MaxLength(250)]
    public string? CreatedByName { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public virtual PlanningTemplate? Template { get; set; }
}
