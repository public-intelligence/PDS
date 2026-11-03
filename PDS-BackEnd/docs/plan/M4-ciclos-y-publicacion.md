# M4 — Ciclos: entidad, wizard y publicación que crea los planes

**Depende de:** M2, M3
**Estado:** Pendiente

## Objetivo

Ciclo de planificación real con FK a la **versión del PEI** que usa y a la **plantilla** del plan anual; publicar el ciclo crea los planes de las áreas alcanzadas en una transacción.

## Entregable verificable

El wizard crea un ciclo en Borrador con PEI y plantilla seleccionados; "Publicar" genera N planes "Sin iniciar" (uno por área alcanzada) visibles en el listado de plan anual filtrado por ciclo.

## Modelos (esquema `Planning`)

- **`PlanningCycle`**: OrganizationId, Name(250), Year int, EndYear int? (bianual), Status (Draft|Published|Closed), **StrategicPlanId FK** (el ciclo fija QUÉ versión del PEI usa — el wizard preselecciona la Vigente), **TemplateId FK** → PlanningTemplate, PublishedAt/By/ByName?, CreatedBy/Name/At, soft delete (solo permitido en Draft). Único filtrado (Org, Year).
- **`PlanningCycleMilestone`**: CycleId, Name(250), Date, Kind enum? (FormulationDeadline|Review|Approval|Other) — FormulationDeadline alimenta el DueDate de los planes.
- **`PlanningCycleArea`**: CycleId, AreaId FK→OrganizationalStructures + AreaName snapshot; único (CycleId, AreaId). Las áreas elegibles son las de **nivel 1** del organigrama vigente.

## Servicio

`IPlanningCycleService`: ListAsync (con conteos de planes por estado), GetAsync, CreateDraftAsync, UpdateDraftAsync, **PublishAsync** — transacción: valida (PEI referenciado en Current, plantilla activa, ≥1 área, hito FormulationDeadline presente) → Published → crea un `AnnualPlan` por área (NotStarted, DueDate = hito límite, nombre "POA {Year} — {AreaName}"); CloseAsync.

## Endpoints

`PlanningCyclesController`: GET `planning-cycles`, GET `/{id}`, POST, PUT `/{id}`, POST `/{id}/publish`, POST `/{id}/close`, GET `/eligible-areas`. Escritura: `planning:cycles:manage`; lectura: cualquier autenticado del tenant.

## Frontend

`cycle.service.ts`; `ciclos-list.component.ts` y `ciclo-nuevo.component.ts` contra API (paso plantilla = PlanningTemplate real; paso PEI = selección de versión; el paso "reglas de ruteo" del wizard pasa a mostrar resumen de la matriz de códigos). Banner solo-lectura en pantallas hijas cuando el ciclo no es el activo (derivado del estado, decisión de navegación por listados con columna Ciclo).

## Migración / seeds
`PlanningM4_Cycles`. Sin seeds.

## Criterios de aceptación
1. Publicar un ciclo con 5 áreas crea exactamente 5 planes NotStarted (transaccional: si falla uno, no queda ninguno).
2. No se puede publicar sin PEI Current, sin plantilla activa o sin fecha límite de formulación.
3. Un ciclo Closed rechaza toda escritura de sus artefactos hijos (409).
4. No se pueden crear dos ciclos con el mismo Year en la misma organización.
