namespace PDS.Service.Services.Planning;

/// <summary>
/// Secuencias de códigos del módulo de Planificación (M5): los códigos visibles
/// ("PR-01", "TR-01", "RQ-2027-001") se generan en el SERVIDOR con PlanningSequence,
/// nunca en el cliente (el mock los generaba con contadores locales → colisiones).
/// </summary>
public interface ISequenceService
{
    /// <summary>
    /// Reserva y devuelve el siguiente número de la secuencia (scope + scopeKey) de forma
    /// atómica (MERGE con HOLDLOCK). Participa de la transacción ambiente si existe.
    /// </summary>
    Task<int> NextAsync(int organizationId, string scope, string scopeKey);
}
