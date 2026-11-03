namespace PDS.Domain.Enums;

/// <summary>
/// Estados del flujo de aprobación de una versión de la estructura organizativa.
/// Draft → InReview → Approved → Published. Rejected regresa a Draft. Superseded
/// se asigna automáticamente a la versión anterior al publicar una nueva.
/// </summary>
public enum VersionStatus
{
    Draft,
    InReview,
    Approved,
    Published,
    Rejected,
    Superseded
}
