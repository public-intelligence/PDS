# PDS Frontend Architecture: Planning Cycles & Budget Ceilings UI Components

## 1. Overview & Information Architecture

This document provides a guide for AI agents and developers on the frontend structure, routes, services, and components governing **Planning Cycles** and **Budget Ceilings** in PDS (`PDS_FrontEnd`).

---

## 2. Component Routes & Page Map

| Route Path | Page Component File | Purpose & Features |
| :--- | :--- | :--- |
| `/configuracion-pds/ciclos` | [ciclos.component.ts](../src/app/modules/planning/pages/configuracion/ciclos.component.ts) | Management of Planning Cycles, wizard setup, area assignment, milestone calendar, publishing, and justified cycle amendments. |
| `/configuracion-pds/techos` | [techos.component.ts](../src/app/modules/planning/pages/configuracion/techos.component.ts) | Institutional Budget Ceilings governance. Features 4 sections: 1) Configuration Workspace, 2) Budget Monitoring, 3) Area Consumption, 4) Cross Execution. Includes Ceiling Detail Drawer and Budget Impact Preview. |

---

## 3. Data Models & Services

### 3.1 Angular Services
* **`PlanningCycleService`**: Wraps `/planning-cycles` HTTP endpoints.
  * Location: [planning-cycle.service.ts](../src/app/services/planning/planning-cycle.service.ts)
* **`BudgetCeilingService`**: Wraps `/api/planning/budget-ceilings` HTTP endpoints.
  * Location: [budget-ceiling.service.ts](../src/app/services/planning/budget-ceiling.service.ts)

### 3.2 Data Models
* Location: [planning-api.models.ts](../src/app/modules/planning/data/planning-api.models.ts)
  * `PlanningCycleDto`, `PlanningCycleDetailDto`, `CreatePlanningCycleDto`, `UpdatePlanningCycleDto`, `PlanningCycleAmendmentLogDto`.
  * `BudgetCeilingDto`, `BudgetCeilingDetailDto`, `CreateBudgetCeilingDto`, `BudgetAdjustmentDto`, `BudgetImpactPreviewDto`.

---

## 4. Key User Flows

1. **Cycle Setup**: Administrator creates a new cycle under `/configuracion-pds/ciclos`. Selects PEI version, POA template, milestones, and target areas.
2. **Cycle Deployment**: On clicking **Publicar Ciclo**, the system generates area POA plans and pre-loads cycle-bound budget ceilings (`CycleId`).
3. **Justified Amendments**: If editing a published cycle, the modal forces a `justification` field. The backend records the amendment in `PlanningCycleAmendmentLogs`.
4. **Budget Monitoring & Validation**: Under `/configuracion-pds/techos`, Planning monitors utilization, adds adjustments with reasons, and simulates impact preview before ceiling reductions.
