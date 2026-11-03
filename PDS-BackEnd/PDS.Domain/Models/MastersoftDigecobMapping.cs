using System;
using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Vinculación / Mapeo entre una Cuenta DIGECOB y una Cuenta MasterSoft.
/// </summary>
public class MastersoftDigecobMapping
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    public Guid DigecobAccountId { get; set; }

    [MaxLength(30)]
    public required string DigecobAccountCode { get; set; }

    public Guid MastersoftAccountId { get; set; }

    [MaxLength(30)]
    public required string MastersoftAccountCode { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navegación
    public virtual AccountingAccount? DigecobAccount { get; set; }
    public virtual MastersoftAccount? MastersoftAccount { get; set; }
}
