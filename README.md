# PDS

[![License: Apache 2.0](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](LICENSE)

Platform for managing the organizational structures of government institutions. Institutions design, review, approve, version and publish their organizational charts, positions and locations, keeping their legal basis and history, and plan their work on top of that structure.

- [Overview](#overview)
- [Architecture](#architecture)
- [Repository structure](#repository-structure)
- [Tech stack](#tech-stack)
- [Getting started](#getting-started)
- [Testing](#testing)
- [Deployment notes](#deployment-notes)
- [Contributing](#contributing)
- [Third-party software](#third-party-software)
- [License](#license)
- [Vision and acknowledgments](#vision-and-acknowledgments)

## Overview

- **Organizational structure.** Each institution manages a tree of units and positions (grade, salary, one head per unit).
- **Versions.** A structure version goes through draft, review, approval and publication. Publishing requires a legal instrument and keeps a snapshot of the units and positions, and the previous version is superseded.
- **Authorized posts.** Each position has its authorized posts and its estimated cost.
- **Locations.** Offices with their territory, working calendars, holidays, time zone and responsible people.
- **Institutional planning.**
  - Strategic plan, planning cycles, annual plans, budget ceilings and funding sources.
  - Master items with expense objects, accounting accounts and unified codes.
  - Requirements and their consolidation.
- **Public organizational chart.** Each institution can publish its current structure for anyone to see.
- **Integration.**
  - Users sign in through GOB-ID, and each position can become a role in GOB-ID.
  - Other systems read structures, locations, calendars and items through M2M endpoints.

## Architecture

```
 Browser ──▶ PDS web (Angular) ──▶ PDS API (.NET 10) ──▶ SQL Server (EF Core migrations)
                │                       │
                └── sign-in by redirect ┴──▶ GOB-ID (user tokens, M2M tokens, roles per position)
                                        ▲
 Other systems, GOB-ID among them ── M2M endpoints
```

## Repository structure

| Folder | Description | Stack |
|---|---|---|
| [`PDS-BackEnd/`](PDS-BackEnd/README.md) | PDS API: structures, positions, versions, locations, planning, public chart and M2M | .NET 10 |
| [`PDS-FrontEnd/`](PDS-FrontEnd/README.md) | PDS web application | Angular 21 |

## Tech stack

| Component | Stack |
|---|---|
| PDS API | ASP.NET Core 10, EF Core with SQL Server (migrations), AutoMapper, QuestPDF, ClosedXML, OpenAPI and Scalar, GOB-ID authentication for users and M2M |
| PDS web | Angular 21, Tailwind CSS 4, `@public-intelligence/ui-core`, Leaflet, d3-hierarchy, jsPDF, pdfmake, Vitest |
| Database | SQL Server |

## Getting started

### Prerequisites

- .NET 10 SDK and the `dotnet-ef` tool
- Node.js 22 and npm
- SQL Server 2019 or later (or Azure SQL Database)
- A running GOB-ID, with PDS registered as an application, to sign in
- A GitHub token with `read:packages`, for `@public-intelligence/ui-core`

### Steps

1. **Database.** Create it with the EF Core migrations, which also load the reference catalogs. From `PDS-BackEnd/`:

   ```bash
   dotnet restore PDS/PDS.csproj
   ConnectionStrings__DefaultConnection="<connection string>" \
     dotnet ef database update --project PDS.Application --startup-project PDS
   ```

2. **API.**
   - Set the secrets with .NET user secrets. The keys are in [`PDS/secrets.example.json`](PDS-BackEnd/PDS/secrets.example.json).
   - Run `dotnet run --project PDS` from `PDS-BackEnd/`. It listens on `http://localhost:5135`.
3. **Web.**
   - Configure `~/.npmrc` for GitHub Packages (see the [frontend README](PDS-FrontEnd/README.md)).
   - Point `src/environments/environment.development.ts` to the API and to GOB-ID.
   - Run `npm install && npm start` from `PDS-FrontEnd/`. It opens on `http://localhost:4200`.

## Testing

- **PDS web:** `cd PDS-FrontEnd && npm test`, with Vitest.
- **PDS API:** no test project in this repository yet. `PDS.slnx` references a `PDS.Tests` project that is not included.

## Deployment notes

- **Secrets** (`ConnectionStrings:DefaultConnection`, `Jwt:Secret`, `GobidM2M:ClientSecret`) go through environment variables or a secret store, never through `appsettings`.
- **The API serves `/openapi` and `/scalar` in production too.** Block them at the reverse proxy if they should not be public.
- **M2M endpoints** are meant for other systems. Expose them only where those systems can reach them.
- **`PlanningDevOverrides`** must stay disabled outside local development.
- **Position roles.** The roles that PDS creates in GOB-ID for each position go to one GOB-ID application installation, configured in `GobidRoles:OrganizationApplicationId`.

## Contributing

- Work in a branch from `development` and open a pull request against it. `master` receives approved versions.
- Schema changes go through EF Core migrations:

  ```bash
  dotnet ef migrations add <Name> --project PDS.Application --startup-project PDS
  ```

## Third-party software

PDS depends on third-party packages that keep their own licenses. They are declared in the `.csproj` files and in `PDS-FrontEnd/package.json`. Most are MIT, Apache 2.0 or BSD. These need attention when you deploy:

- **AutoMapper 16** (API). Since version 15, AutoMapper is distributed under a dual license: the RPL-1.5 or a commercial license from Lucky Penny Software, with a free edition for some organizations. Check whether your organization qualifies, or use version 14, the last one under MIT.
- **QuestPDF** (API, PDF reports). It runs under the QuestPDF Community license, which is free only for some kinds of organizations; others need a commercial license. Check the QuestPDF license terms.
- **Map tiles.** The tile provider configured in `mapTileUrl` has its own usage terms.

## License

Copyright 2026 Rainier Christopher Domingo Mallol Cotes, Jean Antonio Garcia Periche and Melissa Michelle Muñoz Suro.

Licensed under the [Apache License, Version 2.0](LICENSE). See [`NOTICE`](NOTICE).

## Vision and acknowledgments

> *“Under Law No. 247-12 and related administrative frameworks, responsibilities such as finance, strategy, legal, and service management are not abstract concepts; they are offices with people and assigned mandates.”*
>
> — ***Rainier Mallol***, *The Operative Republic*

### Vision

PDS follows the software architecture and best practices of the vision paper [*The Operative Republic: Building an Operating System for Dominican Government Services*](https://publicintelligence.ai/govos), by Rainier Mallol.

The paper argues that the next phase of public-sector modernization will be defined less by portals and more by **Digital Public Infrastructure**: shared, interoperable capabilities that many institutions can reuse instead of rebuilding. Its architecture, **Federated Bounded-Context Microservices with Centralized Orchestration (FeBMO)**, lets each institution retain ownership of its systems and data, while shared identity, workflow governance, audit trails and service standards bring them together as one coherent service environment. Each system is designed to create value on its own and to become more powerful when connected to the others.

PDS gives that architecture its organizational backbone. FeBMO is grounded in legal and institutional reality: systems must align with real institutional ownership, meaning the units, positions and mandates that laws and decrees assign. PDS is where institutions design, review, approve, version and publish that structure (organizational charts, positions and locations) together with its legal basis and history. GOB-ID, workflows and other services can then rely on one official source for how each institution is organized and who reports to whom, instead of keeping their own copies.

**[Read the vision paper →](https://publicintelligence.ai/govos)**

### Acknowledgments

PDS was designed and built with the participation of:

| Name | Role |
|---|---|
| Rainier Mallol | Software Architecture Design |
| José Ant. Cid Troncoso | Lead Engineer |
| Frailin Ogando | Engineer |
| Angel Peña | Engineer |

With the support of Edgar Polanco.

These acknowledgments only recognize participation in the design and development of PDS. They do not grant any copyright, ownership, license or other legal right in the software or its name. All rights belong exclusively to the copyright holders listed under [License](#license).

*Built to help move government services from fragmented coordination to structured execution.*
