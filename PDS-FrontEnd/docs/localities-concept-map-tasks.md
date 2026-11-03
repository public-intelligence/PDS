# Localities, time zones and calendars: frontend task map

Source spec: `localities-time-zones-calendars.md` (SPEC-LOC-CAL-01, Draft v0.3).
Scope of this file: **PDS-FrontEnd only** (spec §12, UI-01 to UI-16). Backend, WF and SDS work appears only as external blockers.

## Instructions for the AI building the concept map

- One node per task (`T01`…`T33`) and per backend blocker (`B-M1`…`B-M6`).
- Draw an arrow from every ID in the **Needs** column to the task (`need → task`).
- Group nodes by phase (A to H). Color by difficulty (Low, Medium, High).
- Mark backend blockers with a different shape: they are not frontend work.
- Lay out left to right: start nodes on the left, `T33` on the right.

## Legend

- **Difficulty:** Low = under 1 day, Medium = 1 to 3 days, High = more than 3 days or tricky logic.
- **UI / RF:** screen ID and requirement ID in the spec.
- **Priority:** MUST unless marked SHOULD or COULD.

## External blockers (backend, not in this repo)

| ID | Delivers | Spec |
|---|---|---|
| B-M1 | All countries, `ciudad`, `zonaHoraria` field, time-zone catalog, current time zone endpoint | RF-LO-17 to 23 |
| B-M2 | Calendar API: schedules, holiday calendars, subscriptions, exceptions, effective calendar, `estado`, default locality, audit channel | RF-LO-24 to 36, 43, 44, 47 |
| B-M3 | `gobidUserId`, `gestionaCalendario`, `gestionables`, retroactive window, ETag / If-Match | RF-LO-33, 37 to 42 |
| B-M6 | Locality functions catalog and `funcionIds` | RF-LO-45, 46 |

LOC-M4 and LOC-M5 (WF, SDS) need no work in this repo.

## Tasks

### Phase A: Foundations (start here, no backend needed)

| ID | Task | What it does | Needs | UI / RF | Difficulty |
|---|---|---|---|---|---|
| T01 | Feature flag | Add `features.calendarioLaboral` to both environment files. Hides UI-05 to UI-12 and their menu entries while off. | none | §12.2 | Low |
| T02 | Permission helper | `isCalendarAdmin`: token `permissions` includes `localities:calendar:manage`. Same pattern as `admin.guard.ts`. | none | §12.2 | Low |
| T03 | API error mapping | Map `detail` keys to form fields, unknown keys to a modal banner. Handle 403 (toast and refresh), 409 (name the conflict), 412 (reload banner). Today `extractApiErrors` only joins messages. | none | §10.5, §12.2 | Medium |
| T04 | Locality-time utilities | Show times in the locality's zone with abbreviation, `dd/MM/yyyy`, 24 h `HH:mm`, and the locality's "today". Never the browser zone. | none | RF-LO-23, §12.2 | Medium |

### Phase B: LOC-M1, time zone and localities abroad

| ID | Task | What it does | Needs | UI / RF | Difficulty |
|---|---|---|---|---|---|
| T05 | M1 models and services | Add `zonaHoraria`, `ciudad`, `subdivisionAdministrativa`, `paisCodigo`, `paisNombre`, `estadoCodigo` to locality models and query. Add time-zone catalog and current-time-zone calls. | B-M1 | RF-LO-19 to 22 | Low |
| T06 | Form: country branch | "País" lists all countries, searchable, default República Dominicana. DO keeps the cascade. Other countries show "Ciudad" (required) and "Estado / provincia / región". Switching clears the other branch. | T05, T03 | UI-03, RF-LO-18 | Medium |
| T07 | Form: time zone | Required searchable select loaded by country. Preselect when the country has one zone. Clear when the country changes. Read-only with reason for non-admins on edit. | T05, T06, T02 | UI-03, RF-LO-20, 21 | Medium |

### Phase C: LOC-M2, existing locality screens

| ID | Task | What it does | Needs | UI / RF | Difficulty |
|---|---|---|---|---|---|
| T08 | List | "Ubicación" column replaces Provincia and Municipio. New "Zona horaria" column with current offset. New "País" filter. | T05, T04 | UI-01 | Medium |
| T09 | Map | Initial view fits all markers (today it starts on RD). Popup adds País, Ciudad, Zona horaria, Hora local (refresh each minute). Same filters as the list. | T05, T04, T08 | UI-02 | Medium |
| T10 | Detail | Header line with zone and local time. Default-calendar badge and admin action "Marcar como predeterminada". Ubicación card for abroad. Bitácora: new action labels and channel. | T05, T04, T02, B-M2 | UI-04, RF-LO-36, 44 | Medium |
| T11 | Form cleanup | Rename "Horario regular" to an informational note. **Remove the "Horarios de excepción" section and stop sending `horariosExcepcion`.** Add the note about the copied schedule on create. | B-M2 | UI-03, RF-LO-26, 47 | Low |
| T12 | Lifecycle modals | Add the calendar-effect line to suspend, reactivate and close. | none | UI-15, RF-LO-32 | Low |

### Phase D: LOC-M2, holiday calendars (settings)

| ID | Task | What it does | Needs | UI / RF | Difficulty |
|---|---|---|---|---|---|
| T13 | Calendar models and services | New `locality-calendar.model.ts`, `holiday-calendar.model.ts` and their two services. | B-M2 | §11, §15.3 | Medium |
| T14 | Intervals editor | Shared control: up to 4 intervals per day, `HH:mm` in 15-minute steps, "24:00" as end, no overlap, end after start. Used by T18, T22, T24. | none | UI-06, 08, 11 | Medium |
| T15 | Routes and sidebar | Add `/settings/calendarios-feriados` (list and `:id`) and the menu entry after "Estados de Localidad", behind the flag. | T01 | UI-16 | Low |
| T16 | Holiday calendars list | `crud-page` list, filters, create and edit modal (admin), delete with 409 subscribers message, "Sin feriados cargados" chip from 1 October. | T13, T15, T02, T03 | UI-09, RF-LO-27 | Medium |
| T17 | Holiday calendar detail | Header plus tabs "Feriados" (year selector, table) and "Localidades suscritas". Read-only for users without rights. | T16 | UI-10, RF-LO-27, 28 | Medium |
| T18 | Holiday entry editor | Modal: fecha, nombre, alcance, intervals for "Jornada reducida". Delete confirmation names the affected localities. | T17, T14 | UI-11, RF-LO-28 | Low |
| T19 | Holiday import (SHOULD) | Mode, paste or file (`.csv`, `.json`), "Validar" dry run with error table, "Importar" enabled only after a clean validation. | T17 | UI-12, RF-LO-29 | Medium |

### Phase E: LOC-M2, locality "Calendario laboral" card

| ID | Task | What it does | Needs | UI / RF | Difficulty |
|---|---|---|---|---|---|
| T20 | Card shell and header strip | New card in the detail after "Ubicación". Zone, local time, offset, DST badge, next clock change, "Abierta ahora / Cerrada ahora". Loading, empty and error states. | T10, T13, T01, T04 | UI-05, RF-LO-22, 34 | Medium |
| T21 | Weekly schedule view | Seven-row table, "Vigente desde", future-version notice with "Ver" and "Eliminar", history. | T20 | UI-05 §1, RF-LO-24, 25 | Medium |
| T22 | Schedule editor | Modal: vigente desde, presets, copy from another locality, day chips, per-day customization, live summary, backdate warning with required motivo. | T21, T14, T02, T03 | UI-06, RF-LO-24 to 26 | High |
| T23 | Subscriptions | Chips linking to the calendar detail, empty-state warning, modal that replaces the whole set (own-country calendars first). | T20, T16 | UI-07, RF-LO-30 | Medium |
| T24 | Exceptions | Table by year (system rows locked), editor modal with three types, overlap 409 with link, delete confirmation. | T20, T14, T03 | UI-08, RF-LO-31, 32 | High |
| T25 | Annual view | Twelve mini-months, legend with patterns (not color only), day tooltip, day panel, "Agregar excepción en esta fecha". | T20, T23, T24 | UI-05 §5, RF-LO-34, RNF-LO-10 | High |

### Phase F: LOC-M3, delegation

| ID | Task | What it does | Needs | UI / RF | Difficulty |
|---|---|---|---|---|---|
| T26 | Responsables: GobID link | Column "Usuario GobID" and an admin-only GUID field with validation and 409 message. | B-M3, T02, T03 | UI-14, RF-LO-37 | Low |
| T27 | "Gestiona calendario" | Checkbox per responsable row in the form (admin edits, others read-only). Badge in the detail. | B-M3, T02 | UI-03, UI-04, RF-LO-38 | Low |
| T28 | Manager-aware UI | Show edit actions when `puedeGestionar` or admin; hide, never disable. "Solo las que gestiono" / "Solo los que gestiono" filters. No actions on rows older than the retroactive window, with tooltip. | B-M3, T08, T16, T22, T23, T24, T18 | §12.2, RF-LO-33, 38, 40 | Medium |
| T29 | Concurrency (SHOULD) | Send `If-Match` on PUT and DELETE. On 412 show the reload banner. | B-M3, T03, T13 | RF-LO-42 | Medium |

### Phase G: LOC-M6, foreign-service roles (parallel, any time after Phase B)

| ID | Task | What it does | Needs | UI / RF | Difficulty |
|---|---|---|---|---|---|
| T30 | Functions catalog | New page `/settings/funciones-localidad`, same pattern as "Tipos de localidad". Model, service, route, menu entry. | B-M6 | UI-13, UI-16, RF-LO-45 | Low |
| T31 | Form: functions | "Funciones" multi-select. Check "Sección consular" requires "Embajada". Non-blocking warning when a function is set and "Unidad vinculada" is empty. | T30, T06 | UI-03, RF-LO-45, 46 | Medium |
| T32 | List and detail: functions | "Funciones" column and filter in the list. Chips in the detail header. "Unidad vinculada" links to the org chart, with the same warning. | T30, T08, T10 | UI-01, UI-04 | Low |

### Phase H: Close-out (last)

| ID | Task | What it does | Needs | UI / RF | Difficulty |
|---|---|---|---|---|---|
| T33 | Hardening and QA | Keyboard and legend check on every screen, loading / empty / error states, Spanish copy review against §12, unit tests for T03, T04, T14, build and lint. | all | §12.2, §14.4, RNF-LO-09, 10 | Medium |

## Flow summary

- **Start with:** T01, T02, T03, T04, T12, T14. None needs the backend.
- **Critical path:** T05 → T06 → T07 → T10 → T13 → T20 → T24 → T25 → T28 → T33.
- **Parallel track:** Phase G (T30 to T32) once Phase B is done.
- **Last:** T28, T29, T33.
- **Phase order:** A → B → C → D → E → F → H. Phase D goes before Phase E because subscriptions and the annual view need holiday calendars to exist.

## Production rules that affect order

- **T11 must ship in the same release as backend RF-LO-47.** After the migration the API rejects `horariosExcepcion` with 400, so an old frontend breaks locality create and edit (RNF-LO-12).
- **T07 must be live before the backend makes `zonaHoraria` required**, or locality create and edit fail.
- Everything in Phases D and E stays behind `features.calendarioLaboral` until the backend flag `Calendario:Habilitado` is on.
- The server is authoritative on permissions. The frontend only hides actions.

## Gaps to resolve before coding

| # | Gap | Affects |
|---|---|---|
| 1 | The spec gives no way for the frontend to read the retroactive window *N* (`Calendario:VentanaRetroactivaDias`). | T22, T24, T28 |
| 2 | `puedeGestionar` is named in §12.2 but is missing from the calendar contract in §11.3. | T20, T28 |
| 3 | UI-01 and UI-09 are tagged LOC-M2, but "Solo las que gestiono" needs RF-LO-40 (LOC-M3) and the "Funciones" column needs RF-LO-45 (LOC-M6). Split here as T08, T28 and T32. | T08, T28, T32 |
| 4 | The list needs each locality's current UTC offset. Decide: batch `zona-horaria` endpoint or the browser's `Intl` API. | T04, T08 |
| 5 | OQ-03 may replace the GobID GUID text field with a user search. | T26 |
| 6 | The project has no multi-select control confirmed and no date or time-zone library. | T04, T31 |
| 7 | The spec is a draft; RF-LO numbers may be renumbered. | all |
