# PDS API

ASP.NET Core (.NET 10) API for organizational structures and planning:
- units, positions and versioned structures with their legal instruments;
- locations, with territory, working calendars, holidays and responsible people;
- institutional planning: strategic plan, annual plans, budget ceilings, funding sources, master items and requirements;
- the public organizational chart;
- machine-to-machine (M2M) endpoints for other systems.

See the [repository README](../README.md) for the overall architecture.

## Structure

```
PDS/                 API: controllers, filters, middleware, security (GOB-ID authentication), Program
PDS.Application/     EF Core context, repositories, interceptors, migrations and seed scripts
PDS.Domain/          Models, DTOs, enums, value objects and responses
PDS.Service/         Services, authorization, HTTP clients (GOB-ID) and mappers
docs/                Integration guides and planning notes (mostly in Spanish)
```

Routes declared without a leading slash get the `api/v1` prefix (`ApiPrefixConvention`); M2M routes are under `/api/v1/m2m/...` and `/m2m/...`.

## Database

The schema is managed with EF Core migrations in `PDS.Application`. The migrations also load reference catalogs: the territory of the Dominican Republic, ISO 3166 countries, the initial working calendar and the location functions. To create or update a database:

```bash
dotnet tool install --global dotnet-ef        # once
dotnet restore PDS/PDS.csproj
ConnectionStrings__DefaultConnection="<connection string>" \
  dotnet ef database update --project PDS.Application --startup-project PDS
```

The design-time factory (`PDSDbContextDesignTimeFactory`) reads only that environment variable, so no other secrets are needed. The API does not apply migrations on startup.

## Run

```bash
dotnet run --project PDS      # http://localhost:5135 (https://localhost:7247)
```

IdentityHub, in the GOB-ID repository, uses the same port by default. If you run both, start one of them with `--urls`.

In the Development and Production environments, the API serves its OpenAPI document (`/openapi/v1.json`) and an interactive reference with Scalar (`/scalar`). In production, block those paths at the reverse proxy if they should not be public.

## Configuration

`PDS/appsettings.json` and `appsettings.Development.json` hold the non-secret settings:
- the token issuer and audience;
- the GOB-ID URLs and the M2M client id;
- the GOB-ID application installation used for position roles;
- the allowed client origins;
- the public organizational chart.

Secrets are listed, without values, in [`PDS/secrets.example.json`](PDS/secrets.example.json):
- `ConnectionStrings:DefaultConnection`;
- `Jwt:Secret`, the GOB-ID application secret of PDS, used to validate user tokens;
- `GobidM2M:ClientSecret`.

Locally, set them with .NET user secrets:

```bash
dotnet user-secrets set --project PDS "Jwt:Secret" "<value>"
```

In a server, use environment variables (`Jwt__Secret`) or a secret store.

`PlanningDevOverrides` grants planning permissions to listed users. Keep it **disabled** outside local development.

## M2M integration

Third-party systems use the M2M endpoints with a GOB-ID service token. The integration guide, in Spanish, is [`docs/m2m_api_documentation.md`](docs/m2m_api_documentation.md).
