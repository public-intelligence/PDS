using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Catálogo de Cuentas del ERP legado MasterSoft.
/// </summary>
public class MastersoftAccount : ISoftDeletable
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    [MaxLength(30)]
    public required string Code { get; set; }

    [MaxLength(300)]
    public required string Name { get; set; }

    /// <summary>Código de la cuenta control/padre en MasterSoft (ej. "1111" para "1111-01").</summary>
    [MaxLength(30)]
    public string? ControlAccountCode { get; set; }

    public Guid? ControlAccountId { get; set; }

    public int Level { get; set; }

    public bool IsPostable { get; set; }

    // Auditoría y soft delete
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navegación
    public virtual MastersoftAccount? ControlAccount { get; set; }
    public virtual ICollection<MastersoftAccount> SubAccounts { get; } = [];
    public virtual ICollection<MastersoftDigecobMapping> DigecobMappings { get; } = [];
}
