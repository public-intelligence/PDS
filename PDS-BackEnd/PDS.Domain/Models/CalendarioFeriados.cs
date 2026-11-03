using PDS.Domain.Enums;

namespace PDS.Domain.Models;

/// <summary>Calendario de feriados compartido por organización (RF-LO-27). Se elimina lógicamente.</summary>
public class CalendarioFeriados : ISoftDeletable
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }
    /// <summary>Único por organización entre los no eliminados (p. ej. <c>DO-NAC</c>, <c>ES-MD</c>).</summary>
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public Guid? PaisId { get; set; }
    public AmbitoCalendarioFeriados Ambito { get; set; }
    /// <summary>Localidad cuyos gestores de calendario pueden mantener las entradas.</summary>
    public Guid? LocalidadGestoraId { get; set; }
    public bool Activo { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    /// <summary>Concurrencia optimista y ETag (RF-LO-42).</summary>
    public byte[]? RowVersion { get; set; }

    public Pais? Pais { get; set; }
    public Localidad? LocalidadGestora { get; set; }
    public ICollection<CalendarioFeriadosDia> Dias { get; set; } = new List<CalendarioFeriadosDia>();
}
