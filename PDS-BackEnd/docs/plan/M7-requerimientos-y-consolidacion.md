# M7 — Requerimientos + Consolidación

**Depende de:** M2, M5
**Estado:** Implementado (revisado en M9)

## Objetivo

Requerimientos reales ruteados por la matriz de códigos (área consolidadora heredada), decisiones de priorización, fusión, fase de ajustes y export PACC.

## Entregable verificable

Marcar una necesidad en una tarea del POA crea el requerimiento en la bandeja del área consolidadora (resuelta por la matriz); la consolidadora estima precio, aprueba/reduce/devuelve/reprograma/fusiona con bitácora; Consolidación muestra el tablero real por ciclo y exporta el PACC xlsx.

## Modelos (esquema `Planning`)

- **`Requirement`**: OrganizationId, Code "RQ-{Year}-{NNN}" (secuencia por ciclo), CycleId FK (denormalizado — listado canónico por ciclo), PlanId FK, PoaProductId FK, **PoaTaskId FK Restrict (siempre nace de una tarea)**, OriginAreaId + Name, ConsolidatorAreaId + Name (snapshot resuelto al crear), AppliedRule(500) texto trazable, SupportCategoryId?, UnifiedCodeId FK + Name snapshot, Description(1000), Quantity, UnitOfMeasure(50), EstimatedUnitPrice, PriceConfirmed bit, Quarter (Q1..Q4), RequiredDate? (rubros sin costo), FundingSource?, Priority (High|Medium|Low), Status (New|InReview|Approved|Reduced|Returned|Merged|Rescheduled), ApprovedAmount?, MergedIntoId self-FK, CreatedBy/Name/At. Índices (Org,Cycle,Status), (ConsolidatorArea,Status), (PoaTaskId). Hard delete solo New|Returned por el área origen (con log).
- **`RequirementComment`**: append-only (UserId, UserName, AreaName, Date, Text).
- **`PrioritizationDecision`**: append-only (RequirementId, RequirementCode, UnifiedCodeId?, Type (Reduction|Return|Reschedule|Prioritization), RequestedAmount, ApprovedAmount, Reason, DecidedBy + Name, DecidingAreaId, Date, Level (Consolidator|Authority)).

## Servicios

- `IRequirementService`: List (por ciclo/consolidadora/origen/estado, paginado), Get, **CreateFromTaskAsync** (valida tarea del plan; consolidadora efectiva vía herencia de la matriz — `ResolveEffectiveAttributes`; precio de referencia del código; secuencia), Update (solo origen, New|Returned, re-rutea y resetea PriceConfirmed/ApprovedAmount), Delete, ConfirmPrice, Decide (Approve|Return|Reschedule → PrioritizationDecision + comentario), Reduce (monto + motivo), Merge (consolidado InReview, originales Merged con MergedIntoId, transaccional), ListByTask → **guard**: eliminar/renombrar tarea con requerimientos vivos → 409 (se activa en `UpsertTasksAsync` de M5 y como error bloqueante en el import de M6).
- `IConsolidationService`: GetBoard(cycleId) — planes por estado, requerimientos por consolidadora/estado, montos = `ApprovedAmount ?? Quantity*Price`; OpenAdjustmentsPhase (ReadyForReview|Observed → InAdjustments); SubmitAdjustments(planId); ExportPacc(cycleId) — xlsx vía `ReportTable` + `IReportExportService` existentes, con alias DIGEPRES/UNSPSC de la matriz.

## Endpoints

- `RequirementsController`: GET `requirements?cycleId=&consolidatorAreaId=&originAreaId=&status=`, GET `/{id}`, POST, PUT `/{id}`, DELETE `/{id}`, POST `/{id}/confirm-price`, POST `/{id}/decide`, POST `/{id}/reduce`, POST `/merge`.
  Crear/editar: `planning:requirements:edit` + área origen = área del contexto. Decidir/reducir/fusionar/confirmar precio: `planning:requirements:consolidate` + contexto pertenece a la consolidadora (o `planning:admin`).
- `ConsolidationController`: GET `consolidation/board?cycleId=`, POST `consolidation/open-adjustments?cycleId=`, GET `consolidation/pacc-export?cycleId=` — `planning:poa:review`.

## Frontend

`requirement.service.ts`, `consolidation.service.ts`; conectar bandeja (columna Ciclo, colas según contexto origen/consolidadora), detalle, diálogo de requerimiento del workspace (catálogo desde `unified-code.service` filtrado por categoría), `ajustes-plan.component.ts`, `consolidacion.component.ts` (bloques de techos/validación financiera siguen mock, marcados "demo"). Después: `inicio` y `mi-trabajo` con contadores/actividad reales (lo de seguimiento/aprobación sigue mock).

## Migración / seeds
`PlanningM7_Requirements` + seed dev de categorías de apoyo.

## Criterios de aceptación
1. Crear requerimiento desde una tarea → aparece en la bandeja de la consolidadora correcta según la herencia de la matriz; `AppliedRule` explica el ruteo.
2. Eliminar la tarea con el requerimiento vivo → 409 con los códigos afectados.
3. Fusionar 2 requerimientos → consolidado InReview + originales Merged enlazados.
4. Reducir → ApprovedAmount + decisión registrada; el monto del board usa el aprobado.
5. PACC xlsx descargable con códigos DIGEPRES/UNSPSC.
