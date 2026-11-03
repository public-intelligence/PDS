using PDS.Domain.Models;

namespace PDS.Service.Services.Planning;

/// <summary>
/// Validación de completitud del POA (M5) — réplica EXACTA de rowIssues() del contrato
/// funcional aprobado (planning-store.service.ts:381-417). El servidor es la única fuente
/// de verdad al someter; el cliente la usa solo como pre-validación visual.
/// </summary>
public interface IPoaValidationService
{
    /// <summary>Incidencias de un producto. columnasObligatorias = columnas Required+Visible de la plantilla del ciclo.</summary>
    IReadOnlyCollection<string> GetProductIssues(
        PoaProduct product,
        IReadOnlyCollection<PlanningTemplateColumn> requiredColumns);
}
