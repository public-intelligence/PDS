# Fuentes de Financiamiento — Architecture & Business Logic

## Business Context

The **Funding Sources (Fuentes de Financiamiento)** catalog represents the origin/provenance of budget funds used in institutional procurement and planning. Examples include:
- **Fuente 10** – Recursos del Tesoro (Treasury Funds)
- **Fuente 20** – Crédito Externo (External Credit/Loans)
- **Fuente 30** – Ingresos Propios (Own Revenue/Fees)
- **Fuente 40** – Donaciones (Donations/Grants)

Each budget ceiling (`BudgetCeiling`) references a funding source, and the combination `{OrganizationId, FiscalYear, CycleId, ExpenseObjectId, FundingSource}` is unique.

---

## Data Model

```
Planning.FundingSources
├── Id              (uniqueidentifier PK)
├── OrganizationId  (int, NOT NULL)
├── Code            (nvarchar(20), NOT NULL)
├── Name            (nvarchar(200), NOT NULL)
├── Description     (nvarchar(500), NULL)
├── IsActive        (bit, default 1)
├── CreatedAt       (datetime2)
├── UpdatedAt       (datetime2)
└── DeletedAt       (datetime2, NULL — soft delete)

Unique Index: UX_FundingSources_OrgCode (OrganizationId, Code) WHERE DeletedAt IS NULL
```

---

## REST API

| Method | Route                                 | Description                     |
|--------|---------------------------------------|---------------------------------|
| GET    | `/api/planning/funding-sources`       | List all (with ?q= and ?onlyActive=) |
| GET    | `/api/planning/funding-sources/{id}`  | Get by ID                       |
| POST   | `/api/planning/funding-sources`       | Create new                      |
| PUT    | `/api/planning/funding-sources/{id}`  | Update existing                 |
| DELETE | `/api/planning/funding-sources/{id}`  | Soft-delete                     |

---

## Related Files

### Backend
- **Entity**: `PDS.Domain/Models/FundingSource.cs`
- **DTOs**: `PDS.Domain/DTOs/Planning/FundingSourceDtos.cs`
- **Service Interface**: `PDS.Service/Services/Planning/IFundingSourceService.cs`
- **Service Implementation**: `PDS.Service/Services/Planning/FundingSourceService.cs`
- **Controller**: `PDS/Controllers/FundingSourcesController.cs`
- **DbContext mapping**: `PDS.Application/Data/PDSDbContext.cs` (Planning.FundingSources)
- **DI Registration**: `PDS.Service/Extensions/ServiceExtensions.cs`
- **Tests**: `PDS.Tests/Services/FundingSourceServiceTests.cs`

### Frontend
- **TypeScript Models**: `planning-api.models.ts` (FundingSourceDto, CreateFundingSourceDto, UpdateFundingSourceDto)
- **Angular Service**: `services/planning/funding-source.service.ts`
- **CRUD UI**: `catalogos-regulatorios.component.ts` → **Fuentes de Financiamiento** tab
- **Dropdown Consumer**: `techos.component.ts` → Fuente de Financiamiento dropdown (dynamic, loaded from catalog)

---

## Integration with Budget Ceilings

When creating a new Budget Ceiling (`BudgetCeiling`), the user selects:
1. **Auxiliar del Objeto del Gasto (DIGEPRES)** — from `RegulatoryCatalogService.listExpenseObjects(onlyDetail=true)`
2. **Fuente de Financiamiento** — from `FundingSourceService.list(onlyActive=true)`

The selected funding source code+name is stored as `FundingSource` (string) on the `BudgetCeiling` entity.
