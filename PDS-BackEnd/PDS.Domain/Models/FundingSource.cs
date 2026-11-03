using System;
using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Catálogo de Fuentes de Financiamiento Institucionales (Módulo Presupuesto / PDS - M10).
/// Representa la procedencia de los fondos (ej: "Fuente 10 - Recursos del Tesoro", "Fuente 20 - Crédito Externo", etc.).
/// </summary>
public class FundingSource : ISoftDeletable
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    [MaxLength(20)]
    public required string Code { get; set; }

    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
