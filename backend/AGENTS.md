# AGENTS.md — backend

ASP.NET Core 10 Web API. Targets `net10.0`, root namespace `Backend`, `Nullable` and `ImplicitUsings` enabled.

The startup project is [`backend.csproj`](./backend.csproj). Tests live in the sibling [`backend.Tests/`](../backend.Tests) project (xUnit v3).

See the [root AGENTS.md](../AGENTS.md) for monorepo-wide concerns (compose, git workflow, CI).

## Stack

- **Web** — ASP.NET Core 10, OpenAPI via `Microsoft.AspNetCore.OpenApi`.
- **Auth** — ASP.NET Core Identity (`AddIdentityCore`), backed by EF Core.
- **Relational** — EF Core 10 + Npgsql + NetTopologySuite (PostGIS). Local dev runs against `postgis/postgis:18-3.6-alpine` because the initial migration depends on the `postgis` extension.
- **Document** — Cosmos DB (`Microsoft.Azure.Cosmos`). Local dev hits the Linux emulator; Azure uses managed identity (no key).
- **Telemetry** — Application Insights. AI is only registered if `APPLICATIONINSIGHTS_CONNECTION_STRING` is set.

## Setup

From the repo root:

```bash
dotnet tool restore                  # installs dotnet-ef
dotnet restore                       # restores both backend and backend.Tests via the .slnx
```

Bring up the databases (one of):

```bash
docker compose up -d db cosmos                  # databases only (using docker-compose.yml)
docker compose -f docker-compose.db.yml up -d   # databases only (using docker-compose.db.yml)
```

## Build · run · test · lint

Run all commands from the repo root.

```bash
# Build
dotnet build --configuration Release

# Run the API (defaults to https://localhost:7xxx + http://localhost:5xxx)
dotnet run --project backend

# Tests
dotnet test --configuration Release

# Format check (CI gate) and auto-format
dotnet format --verify-no-changes
dotnet format
```

A successful run prints `Connected to database at …` and `Connected to CosmosDB account: …`. If either connection check fails the host throws and exits — don't paper over the exception.

`/health` returns `{ "status": "ok" }`. OpenAPI is mapped only in Development at `/openapi/v1.json`.

## Project layout

We layer Clean-Architecture-lite. Dependencies point inward: API → Application → Domain.

```
backend/
├── Program.cs                       # Composition root, DI wiring, host pipeline
├── Domain/                          # Entities + value objects + enums. No external deps.
│   ├── Entities/
│   └── Enums/
├── Infrastructure/
│   ├── Identity/                    # ApplicationUser, ApplicationRole, RefreshToken
│   └── Persistence/
│       ├── AppDbContext.cs
│       ├── Configurations/          # IEntityTypeConfiguration<T> per aggregate
│       ├── Analytics/               # Read-model classes (KPIs, balances, reputation)
│       └── Migrations/              # EF Core migrations live here
├── Properties/
├── appsettings.json
└── appsettings.Development.json     # Local Postgres + Cosmos emulator config
```

Keep new entity configurations in [`Infrastructure/Persistence/Configurations/`](./Infrastructure/Persistence/Configurations) and register them via `OnModelCreating` reflection if that pattern is already in use — check `AppDbContext.cs` first.

## Configuration

`Program.cs` reads from environment variables first, then `appsettings.{Env}.json`. Two distinct paths:

| Setting              | Local (`appsettings.Development.json` / compose env) | Azure (env vars)                                                     |
| -------------------- | ---------------------------------------------------- | -------------------------------------------------------------------- |
| Postgres connection  | `ConnectionStrings:DefaultConnection`                | `AZURE_POSTGRESQL_HOST`, `_PORT`, `_DATABASE`, `_USERNAME` (managed identity token) |
| Cosmos endpoint      | `CosmosDb:AccountEndpoint` + `CosmosDb:AccountKey`   | `AZURE_COSMOS_ENDPOINT` (managed identity, no key)                   |
| Application Insights | not set                                              | `APPLICATIONINSIGHTS_CONNECTION_STRING`                              |

In `Development`, the Cosmos client switches to gateway mode and accepts the emulator's self-signed certificate for local emulator scenarios. Common local hosts include `localhost`, `127.0.0.1`, and `cosmos`; do not rely on this bypass outside Development.

## Migrations

EF Core migrations live in `Infrastructure/Persistence/Migrations`. Run from the repo root:

```bash
# Create a migration
dotnet ef migrations add <Name> \
  --project backend --startup-project backend \
  --context Backend.Infrastructure.Persistence.AppDbContext \
  --output-dir Infrastructure\Persistence\Migrations

# Apply migrations
dotnet ef database update \
  --project backend --startup-project backend \
  --context Backend.Infrastructure.Persistence.AppDbContext
```

One migration per logical change; use a meaningful name (`AddPostgisExtension`, not `Update1`). If you need a new Postgres extension, enable it in the migration's `Up()` (`migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS …")`), not at runtime.

## C# style

`dotnet format --verify-no-changes` is the CI gate, so formatting is non-negotiable. Beyond that:

- **Namespaces** — file-scoped (`namespace Backend.Foo;`), one per file.
- **Usings** — outside the namespace; `System.*` first, then third-party, then `Backend.*`.
- **Naming** — types/methods/properties `PascalCase`; interfaces `IPascalCase`; locals/parameters `camelCase`; private fields `_camelCase`; constants `PascalCase`.
- **Modifiers** — always explicit (`internal class Foo`, never bare `class Foo`).
- **Nullability** — `Nullable` is enabled. Don't `!`-suppress without a comment justifying why the compiler is wrong.
- **Async** — methods returning `Task` end in `Async`. `async void` is banned outside event handlers.
- **`var`** — fine when the type is obvious from the right-hand side; explicit otherwise.
- **Braces** — always, even on single-line bodies.

## Tests

[`backend.Tests/`](../backend.Tests) uses xUnit v3 with Microsoft.NET.Test.Sdk and coverlet. It references `backend.csproj` directly. Add new test classes alongside existing ones; keep them deterministic (no live network or Cosmos calls). Run with `dotnet test` from the repo root.
