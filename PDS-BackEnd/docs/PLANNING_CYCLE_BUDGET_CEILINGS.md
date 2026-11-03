# PDS Backend Architecture: Planning Cycles & Budget Ceilings Integration

## 1. Executive Summary & Business Logic

This document specifies the integration between **Planning Cycles (`PlanningCycle`)** and **Budget Ceilings (`BudgetCeiling`)** in the PDS system.

In public sector institutional governance (DIGEPRES compliance):
* A **Planning Cycle (M4)** serves as the single unified container linking Strategy (PEI), Annual Form (Template), Reached Organizational Units (Areas), Timeline (Milestones), and Financial Envelopes (**Cycle Budget Ceilings**).
* Budget ceilings are governed at the **Auxiliary Expense Object level (DIGEPRES `IsDetail == true`)**, NOT at the UNSPSC procurement level.
* Master baseline ceilings (`CycleId == null`) are configured in `Configuración → Techos Presupuestarios`.
* When a `PlanningCycle` is deployed/published (`PublishAsync`), the master baseline ceilings for that fiscal year are automatically instantiated into **Active Cycle Ceilings (`CycleId == cycle.Id`)**.
* Any modification to a deployed/published cycle or its financial envelope requires a mandatory **Justification**, which is permanently logged in `PlanningCycleAmendmentLog`.

---

## 2. Entity Model & Relationships (ERD)

```
╔═════════════════════════════════╗          ╔══════════════════════════════════╗
║         PlanningCycle           ║ 1    *   ║          BudgetCeiling           ║
╠═════════════════════════════════╬──────────║══════════════════════════════════╣
║ Id: Guid                        ║          ║ Id: Guid                         ║
║ OrganizationId: int             ║          ║ OrganizationId: int              ║
║ Name: string                    ║          ║ FiscalYear: int                  ║
║ Year: int                       ║          ║ CycleId: Guid? (FK)              ║
║ Status: Draft | Published | Closed║          ║ ExpenseObjectId: Guid (FK)       ║
║ StrategicPlanId: Guid (FK)      ║          ║ ExpenseObjectCode: string        ║
║ TemplateId: Guid (FK)           ║          ║ FundingSource: string            ║
║ PublishedAt: DateTime?          ║          ║ InitialAmount: decimal(18,2)     ║
╚═════════════════════════════════╝          ║ CurrentAmount: decimal(18,2)     ║
                 │ 1                         ║ ReservedAmount: decimal(18,2)    ║
                 │                           ║ PlannedAmount: decimal(18,2)     ║
                 │ *                         ║ AvailableAmount: decimal(18,2)   ║
╔═════════════════════════════════╗          ║ ValidationBehavior: Block | ...  ║
║    PlanningCycleAmendmentLog    ║          ║ Status: Green | Yellow | Red     ║
╠═════════════════════════════════╣          ╚══════════════════════════════════╝
║ Id: Guid                        ║                           │ 1
║ OrganizationId: int             ║                           │
║ CycleId: Guid (FK)              ║                           │ *
║ AmendmentType: string           ║          ╔══════════════════════════════════╗
║ Justification: string           ║          ║         BudgetAdjustment         ║
║ PreviousValuesJson: string      ║          ╠══════════════════════════════════╣
║ NewValuesJson: string           ║          ║ Id: Guid                         ║
║ CreatedBy: Guid                 ║          ║ BudgetCeilingId: Guid (FK)       ║
║ CreatedAt: DateTime             ║          ║ AdjustmentType: Increase | Red...║
╚═════════════════════════════════╝          ║ Amount: decimal(18,2)            ║
                                             ║ Reason: string                   ║
                                             ╚══════════════════════════════════╝
```

---

## 3. Operational Workflows & Services

### 3.1 Cycle Pre-loading & Publication (`PlanningCycleService.PublishAsync`)
1. Validates cycle parameters (PEI status, template status, areas count $\ge 1$, deadline milestone).
2. Updates `cycle.Status = "Published"`, `PublishedAt = UtcNow`.
3. Creates `AnnualPlan` instances for reached areas.
4. Queries master baseline ceilings (`CycleId == null`, `FiscalYear == cycle.Year`).
5. Instantiates active cycle ceilings (`CycleId = cycle.Id`) for all master expense objects.

### 3.2 Justified Cycle Updates (`PlanningCycleService.UpdateDraftAsync`)
1. If `cycle.Status == "Published"`, validates that `dto.Justification` is non-empty.
2. Updates cycle details (Name, Milestones, Areas).
3. Adds a `PlanningCycleAmendmentLog` entry recording `AmendmentType = "CycleUpdate"`, `Justification`, `PreviousValuesJson`, and `NewValuesJson`.

---

## 4. API Routes & Controllers

| Endpoint | Method | Description |
| :--- | :--- | :--- |
| `/planning-cycles` | `GET` | List planning cycles with plan counts |
| `/planning-cycles/{id}` | `GET` | Get cycle detail (including `budgetCeilings` and `amendments`) |
| `/planning-cycles` | `POST` | Create a draft planning cycle |
| `/planning-cycles/{id}` | `PUT` | Update a cycle (requires `justification` if published) |
| `/planning-cycles/{id}/publish` | `POST` | Publish cycle & instantiate cycle budget ceilings |
| `/api/planning/budget-ceilings` | `GET` | List budget ceilings (filter by `fiscalYear`, `cycleId`, `fundingSource`) |
| `/api/planning/budget-ceilings/{id}/adjustments` | `POST` | Add budget ceiling adjustment with reason |
| `/api/planning/budget-ceilings/validate-requirement` | `POST` | Validate requirement item against available ceiling |

---

## 5. File Location Reference

* **Domain Models**:
  * [PlanningCycle.cs](../PDS.Domain/Models/PlanningCycle.cs)
  * [BudgetCeiling.cs](../PDS.Domain/Models/BudgetCeiling.cs)
  * [PlanningCycleAmendmentLog.cs](../PDS.Domain/Models/PlanningCycleAmendmentLog.cs)
* **DbContext Mapping**:
  * [PDSDbContext.cs](../PDS.Application/Data/PDSDbContext.cs)
* **Services**:
  * [PlanningCycleService.cs](../PDS.Service/Services/Planning/PlanningCycleService.cs)
  * [BudgetCeilingService.cs](../PDS.Service/Services/Planning/BudgetCeilingService.cs)
* **Controllers**:
  * [PlanningCyclesController.cs](../PDS/Controllers/PlanningCyclesController.cs)
  * [BudgetCeilingsController.cs](../PDS/Controllers/BudgetCeilingsController.cs)
* **Unit Tests**:
  * [PlanningCycleServiceTests.cs](../PDS.Tests/Services/PlanningCycleServiceTests.cs)
  * [BudgetCeilingServiceTests.cs](../PDS.Tests/Services/BudgetCeilingServiceTests.cs)
