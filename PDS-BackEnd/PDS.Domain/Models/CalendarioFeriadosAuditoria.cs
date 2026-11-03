using PDS.Domain.Enums;

namespace PDS.Domain.Models;

/// <summary>
/// Bitácora append-only de un calendario de feriados (RF-LO-44): quién, cuándo (UTC), qué (diff JSON
/// <c>{ campo: { from, to } }</c>), por qué y por qué canal.
/// </summary>
public class CalendarioFeriadosAuditoria
{
    public Guid Id { get; set; }
    public Guid CalendarioFeriadosId { get; set; }
    public int OrganizationId { get; set; }
    public required string Accion { get; set; }
    public string? Motivo { get; set; }
    public string? Detalle { get; set; }
    public Guid UsuarioId { get; set; }
    public string? Usuario { get; set; }
    public CanalCambio Canal { get; set; }
    public DateTime Fecha { get; set; }

    public CalendarioFeriados? CalendarioFeriados { get; set; }
}
