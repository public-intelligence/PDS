using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Alias de un código unificado en un sistema externo (M2): DIGEPRES, DIGECOB,
/// INVENTARIO, etc. Absorbe las equivalencias del archivo consolidado sin
/// migración estructural. No tiene soft delete — se gestiona dentro del agregado.
/// </summary>
public class UnifiedCodeAlias
{
    public Guid Id { get; set; }
    public Guid UnifiedCodeId { get; set; }

    /// <summary>Sistema externo: 'DIGEPRES', 'DIGECOB', 'INVENTARIO', etc.</summary>
    [MaxLength(30)]
    public required string System { get; set; }

    /// <summary>Código en el sistema externo.</summary>
    [MaxLength(50)]
    public required string Code { get; set; }

    /// <summary>Nombre descriptivo en el sistema externo (opcional, para referencia).</summary>
    [MaxLength(250)]
    public string? Name { get; set; }

    public virtual UnifiedCode? UnifiedCode { get; set; }
}
