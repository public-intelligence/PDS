namespace PDS.Domain.Models;

/// <summary>
/// Registro append-only de entradas al sistema: una fila por sesión autenticada, con el área
/// desde la que entró el usuario. Responde "quién entró y desde qué área", que la bitácora de
/// actividad (centrada en acciones de negocio) no cubre.
/// </summary>
public class SystemEntryLog
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    public Guid UserId { get; set; }
    public string? UserName { get; set; }

    /// <summary>Claim session_id del token (o jti como respaldo). Deduplica la entrada.</summary>
    public string? SessionId { get; set; }

    /// <summary>Área resuelta al entrar; snapshot, no FK: la bitácora sobrevive al organigrama.</summary>
    public Guid? AreaId { get; set; }
    public string? AreaName { get; set; }
    public string? PositionName { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public DateTime EnteredAtUtc { get; set; }
}
