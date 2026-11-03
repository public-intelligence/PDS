namespace PDS.Domain.Enums;

/// <summary>
/// Acción registrada en la bitácora de trazabilidad de una versión (RF-FA-01).
/// </summary>
public enum VersionAction
{
    Created,
    SubmittedForReview,
    Approved,
    Rejected,
    Published,
    Superseded
}
