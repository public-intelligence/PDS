using PDS.Domain.Enums;

namespace PDS.Domain.Models;

/// <summary>
/// Bitácora append-only de cambios de una localidad (RF-LO-06, RF-LO-14): quién, cuándo, por qué,
/// el instrumento legal de respaldo (cuando aplica) y el diff de campos modificados (JSON).
/// Nunca se elimina; conserva el historial aunque la localidad se dé de baja lógicamente.
/// </summary>
public class LocalidadAuditoria
{
    public Guid Id { get; set; }
    public Guid LocalidadId { get; set; }
    public int OrganizationId { get; set; }

    public LocalidadAccion Accion { get; set; }
    public string? Motivo { get; set; }

    public Guid UsuarioId { get; set; }
    public string? Usuario { get; set; }
    public DateTime Fecha { get; set; }

    public Guid? InstrumentoLegalId { get; set; }

    /// <summary>Diff JSON de los campos modificados: { campo: { from, to } }.</summary>
    public string? Detalle { get; set; }

    /// <summary>Canal del cambio (RF-LO-44). Nulo en filas anteriores al calendario laboral.</summary>
    public CanalCambio? Canal { get; set; }

    public Localidad? Localidad { get; set; }
    public LegalInstrument? InstrumentoLegal { get; set; }
}
