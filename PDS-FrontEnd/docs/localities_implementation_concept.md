# Localities implementation concept: backend and frontend tickets

Source spec: `localities-time-zones-calendars.md` (SPEC-LOC-CAL-01, Draft v0.3).
Frontend tickets (`T01` to `T33`) are detailed in `localities-concept-map-tasks.md`. Backend tickets (`B01` to `B30`) are derived from the spec; the PDS-BackEnd code was not inspected.

## Instructions for the AI building the concept map

- Two swimlanes: **Backend** (`B..`) and **Frontend** (`T..`).
- Arrows inside a lane come from the **Needs** column.
- Arrows across lanes come from the **Links** table (`backend → frontend`).
- Group both lanes into the same vertical bands, one per wave (W0 to W7), left to right.
- Color by difficulty. Mark release-coupled pairs (see "Release rules") with a thick link.

## Legend

- **Difficulty:** Low = under 1 day, Medium = 1 to 3 days, High = more than 3 days or tricky logic.
- **Priority:** MUST unless marked SHOULD.

## Backend tickets (PDS-BackEnd)

### LOC-M1: foundations

| ID | Ticket | What it does | Needs | RF | Difficulty |
|---|---|---|---|---|---|
| B01 | Country catalog | Seed all ISO 3166-1 countries, add `CodigoIso3`, `GET /territorio/paises` returns all. | none | RF-LO-17 | Low |
| B02 | Addresses abroad | `Ciudad`, `SubdivisionAdministrativa`, territorial validation by country, duplicate rule `paisCiudadTipo`. | B01 | RF-LO-18 | Medium |
| B03 | Permissions and guard | `localities:calendar:manage`, `localities:calendar:read`, `ICalendarioAccessGuard`. Register both in GobID. | none | RF-LO-39 | Medium |
| B04 | Time wiring and locality time zone | Add NodaTime and `TimeProvider`. `ZonaHoraria` column, IANA validation, backfill, admin-only change. | B03 | RF-LO-20 | Medium |
| B05 | Time-zone catalog | `GET /catalogos/zonas-horarias?paisCodigo=`. | B04 | RF-LO-21 | Low |
| B06 | Current time zone | Single and batch endpoints (user and M2M). Locality-local "today". | B04 | RF-LO-22, 23 | Medium |
| B07 | List fields and filters | New fields in list, detail and `by-ids`; code-based filters (`paisCodigo`, `estadoCodigo`). | B02, B04 | RF-LO-19 | Low |
| B08 | M2M hardening and unpaged list | Service account required, `by-ids` scoped by organization, M2M list returns all. | none | RF-LO-51, 52 | Medium |

### LOC-M2: calendar core

| ID | Ticket | What it does | Needs | RF | Difficulty |
|---|---|---|---|---|---|
| B09 | Calendar schema | Migration `AddCalendarioLaboral`: new tables, enums, interval value object. | B04 | §9 | Medium |
| B10 | Feature flag | `Calendario:Habilitado` gates calendar writes. | none | §13.3 | Low |
| B11 | Version and change feed | `CalendarioVersion` increments and `CalendarioCambio` rows on every calendar change. | B09 | RF-LO-43 | Medium |
| B12 | Audit trail | New audit actions, `Canal` column, holiday-calendar audit table. | B09 | RF-LO-44 | Medium |
| B13 | Day resolver engine | Pure engine for one day and a range (§10.2). EC and ST vectors as unit tests. | B09 | RF-LO-34, RNF-LO-08 | High |
| B14 | Holiday calendars and entries | CRUD for calendars and dated entries, 409 on subscribers and duplicate dates. | B09, B11, B12 | RF-LO-27, 28 | Medium |
| B15 | Holiday import (SHOULD) | JSON and CSV import, dry run, replace year. | B14 | RF-LO-29 | Medium |
| B16 | Weekly schedule versions | Create and delete versions, validation, copy default schedule on locality create. | B09, B11, B12 | RF-LO-24 to 26 | Medium |
| B17 | Subscriptions | Replace a locality's holiday-calendar set. | B14 | RF-LO-30 | Low |
| B18 | Exceptions | CRUD for closures, special hours and extra working days; no overlaps. | B09, B11, B12 | RF-LO-31 | Medium |
| B19 | Lifecycle exceptions | Suspend, reactivate and close create or end system exceptions. | B18 | RF-LO-32 | Medium |
| B20 | Calendar read endpoints | Calendar definition, effective calendar, `estado` (user and M2M). | B13, B14, B16, B17, B18 | RF-LO-34 | Medium |
| B21 | Default calendar locality | Flag, unique index, `PUT …/calendario/predeterminada`. | B09 | RF-LO-36 | Low |
| B22 | Legacy migration and freeze | Data migration of §13.1, convert legacy rows, reject `horariosExcepcion` with 400. | B14, B16, B18, B19, B21 | RF-LO-47 | High |

### LOC-M3: delegation and external API

| ID | Ticket | What it does | Needs | RF | Difficulty |
|---|---|---|---|---|---|
| B23 | Responsable GobID link | `Responsable.GobidUserId`, admin only. | B03 | RF-LO-37 | Low |
| B24 | Calendar manager | `GestionaCalendario` flag and the manager rule in the guard. | B23 | RF-LO-38 | Medium |
| B25 | Retroactive window | Managers limited to today − N days; admins need `motivo`. | B24 | RF-LO-33 | Medium |
| B26 | "What can I manage" | `gestionables` endpoints for localities and holiday calendars. | B24 | RF-LO-40 | Low |
| B27 | Concurrency (SHOULD) | `ETag` on GET, `If-Match` on PUT and DELETE, expose the header in CORS. | B14, B18 | RF-LO-42 | Medium |
| B28 | External channel and guide | `X-Client-Application` as audit channel; `docs/calendar_api_integration.md`. | B12 | RF-LO-41 | Low |

### LOC-M4 and LOC-M6

| ID | Ticket | What it does | Needs | RF | Difficulty |
|---|---|---|---|---|---|
| B29 | Business-time endpoints | Deadline and working-minutes calculator (§10.3), BT vectors. Consumed by WF and SDS only. | B20 | RF-LO-35 | High |
| B30 | Locality functions | Catalog, seed, `funcionIds` on locality, consular-section rule, filters. | B07 | RF-LO-45 | Medium |

## Frontend tickets (PDS-FrontEnd)

| ID | Ticket | Difficulty | ID | Ticket | Difficulty |
|---|---|---|---|---|---|
| T01 | Feature flag | Low | T18 | Holiday entry editor | Low |
| T02 | Permission helper | Low | T19 | Holiday import (SHOULD) | Medium |
| T03 | API error mapping | Medium | T20 | Calendar card shell and header | Medium |
| T04 | Locality-time utilities | Medium | T21 | Weekly schedule view | Medium |
| T05 | M1 models and services | Low | T22 | Schedule editor | High |
| T06 | Form: country branch | Medium | T23 | Subscriptions | Medium |
| T07 | Form: time zone | Medium | T24 | Exceptions | High |
| T08 | List | Medium | T25 | Annual view | High |
| T09 | Map | Medium | T26 | Responsables: GobID link | Low |
| T10 | Detail | Medium | T27 | "Gestiona calendario" | Low |
| T11 | Form cleanup | Low | T28 | Manager-aware UI | Medium |
| T12 | Lifecycle modals | Low | T29 | Concurrency (SHOULD) | Medium |
| T13 | Calendar models and services | Medium | T30 | Functions catalog | Low |
| T14 | Intervals editor | Medium | T31 | Form: functions | Medium |
| T15 | Routes and sidebar | Low | T32 | List and detail: functions | Low |
| T16 | Holiday calendars list | Medium | T33 | Hardening and QA | Medium |
| T17 | Holiday calendar detail | Medium | | | |

## Links: which backend ticket unblocks which frontend ticket

| Backend | Unblocks frontend | Shared contract |
|---|---|---|
| B01, B02 | T05, T06 | Countries list; `ciudad`, `subdivisionAdministrativa`; error keys `territorio`, `ciudad` |
| B03, B04, B05 | T05, T07 | `zonaHoraria` field; zones by country; 403 key `permiso` |
| B06 | T08, T09, T10, T20 | Current offset, local time, DST, next transition |
| B07 | T05, T08, T09 | New list fields and `paisId` filter |
| B10 | T01, T15 | Both flags switched on together |
| B12 | T10 | New bitácora actions and `canal` |
| B14 | T13, T16, T17, T18 | Holiday calendar and entry DTOs; 409 keys `suscriptores`, `fecha` |
| B15 | T19 | Import result `{creados, actualizados, omitidos, errores[]}` |
| B16 | T11, T21, T22 | Schedule payload (`dias.mon` … `sun`); key `jornada` |
| B17 | T23 | Subscription set |
| B18, B19 | T24 | Exception DTO, `esSistema`; key `solapamiento` |
| B20 | T13, T20, T25 | Calendar definition, effective days, `estado` |
| B21 | T10 | `esLocalidadCalendarioPredeterminada` and the PUT |
| B22 | T11 | `horariosExcepcion` removed |
| B23 | T26 | `gobidUserId` |
| B24 | T27, T28 | `gestionaCalendario`, `puedeGestionar` |
| B25 | T28 | Retroactive window N; 403 keys `fechaInicio`, `vigenteDesde` |
| B26 | T28 | `gestionables` lists |
| B27 | T29 | `ETag`, `If-Match`, 412 key `concurrencia` |
| B30 | T30, T31, T32 | Functions catalog, `funcionIds`, `funciones`; key `funciones` |

No frontend link: B08, B09, B11, B13, B28, B29. Frontend-only (no backend needed): T02, T03, T04, T12, T14, T33.

## Aligned work order

Each wave lists what both sides work on at the same time. A frontend ticket in wave N consumes backend tickets finished in wave N or earlier.

| Wave | Backend | Frontend | Handoff at the end |
|---|---|---|---|
| W0 Setup | B03, B04, B10 | T01, T02, T03, T04, T12, T14 | Both sides agree on the DTOs and the open gaps below |
| W1 Time zone and abroad (LOC-M1) | B01, B02, B05, B06, B07, B08 | T05, T06, T07 | Localities abroad can be created with a time zone |
| W2 Holiday calendars | B09, B11, B12, B13, B14, B15 | T08, T09, T13, T15, T16, T17, T18, T19 | Holiday calendars can be created and loaded |
| W3 Locality calendar | B16, B17, B18, B19, B20, B21 | T10, T20, T21, T22, T23, T24, T25 | Full "Calendario laboral" card works behind the flag |
| W4 Migration and release (LOC-M2) | B22 | T11 | Same release; flags turned on after data is loaded |
| W5 Delegation (LOC-M3) | B23, B24, B25, B26, B27, B28 | T26, T27, T28, T29 | Managers edit only their localities |
| W6 Roles (LOC-M6, parallel after W1) | B30 | T30, T31, T32 | Embassies and consulates are classified |
| W7 Close-out | B29 (for WF and SDS) | T33 | QA sign-off |

**First tickets on each side:** backend B03, B04, B01; frontend T01, T02, T03, T04, T14.

**Tip while the backend is behind:** the frontend can build W2 and W3 screens against mocked responses from spec §11.3, then swap to the real API.

## Release rules (must ship together)

| Backend | Frontend | Why |
|---|---|---|
| B04 makes `zonaHoraria` required | T07 | Without the field in the form, locality create and edit fail |
| B22 rejects `horariosExcepcion` | T11 | An old form still sends it and gets 400 |
| B10 `Calendario:Habilitado` | T01 `features.calendarioLaboral` | The UI must not show screens whose writes are refused |
| B03 permissions registered in GobID | T02 | The admin check reads the token's `permissions` claim |

## Gaps to agree between backend and frontend before W2

| # | Gap | Tickets |
|---|---|---|
| 1 | No contract lets the frontend read the retroactive window N. | B25, T28 |
| 2 | `puedeGestionar` is used by the UI rules (§12.2) but missing from the contract (§11.3). | B20, B24, T20, T28 |
| 3 | Current offset in the list: batch `zona-horaria` call or computed in the browser. | B06, T08 |
| 4 | OQ-03: GobID user search may replace the GUID text field. | B23, T26 |
| 5 | The spec is a draft; RF-LO numbers may be renumbered. | all |
