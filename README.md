# PDS

Platform for managing the organizational structures of government institutions. Institutions design, review, approve, version and publish their organizational charts, positions and locations, keeping their legal basis and history.

## Projects

| Folder           | Description                       | Stack       |
|------------------|-----------------------------------|-------------|
| `PDS-BackEnd/`   | PDS API                           | .NET 10     |
| `PDS-FrontEnd/`  | PDS web application               | Angular 21  |

## Attributions and Authorship

> *“Under Law No. 247-12 and related administrative frameworks, responsibilities such as finance, strategy, legal, and service management are not abstract concepts; they are offices with people and assigned mandates.”*
>
> — ***Rainier Mallol***, *The Operative Republic*

### Architecture

Software Architecture Design by ***Rainier Mallol***, alongside the best practices outlined in his vision paper [*The Operative Republic: Building an Operating System for Dominican Government Services*](https://publicintelligence.ai/govos).

The paper argues that the next phase of public-sector modernization will be defined less by portals and more by **Digital Public Infrastructure**: shared, interoperable capabilities that many institutions can reuse instead of rebuilding. Its architecture, **Federated Bounded-Context Microservices with Centralized Orchestration (FeBMO)**, lets each institution retain ownership of its systems and data, while shared identity, workflow governance, audit trails and service standards bring them together as one coherent service environment. Each system is designed to create value on its own and to become more powerful when connected to the others.

PDS gives that architecture its organizational backbone. FeBMO is grounded in legal and institutional reality: systems must align with real institutional ownership, meaning the units, positions and mandates that laws and decrees assign. PDS is where institutions design, review, approve, version and publish that structure (organizational charts, positions and locations) together with its legal basis and history. GOB-ID, workflows and other services can then rely on one official source for how each institution is organized and who reports to whom, instead of keeping their own copies.

**[Read the vision paper →](https://publicintelligence.ai/govos)**

### Engineering

**Engineers**

- José Cid (cidtroncoso)
- Frailin Ogando
- Angel Peña

With the support of Edgar Polanco.

*Built to help move government services from fragmented coordination to structured execution.*
