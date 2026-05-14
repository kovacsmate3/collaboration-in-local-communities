# collaboration-in-local-communities

## Design

Sprint 0 wireframes for the 8 core screens (login, register, profile, seeker
feed, helper feed, post task, chat, my tasks) are maintained in Figma and
exported as PDF snapshots in this repo.

- **Figma (source of truth, editable):** https://www.figma.com/design/WGRPJE8LIU1EBtQVzRgpK5
- **Desktop PDF:** [`docs/wireframes/2gather-wireframes-desktop.pdf`](docs/wireframes/2gather-wireframes-desktop.pdf)
- **Mobile PDF:** [`docs/wireframes/2gather-wireframes-mobile.pdf`](docs/wireframes/2gather-wireframes-mobile.pdf)

The Figma file has two pages — one per viewport — and each frame maps
to a route in `frontend/app/`. See the "Frame ↔ route map" panel in the
desktop Figma page for the exact mapping.

## Local dev stack

`docker-compose.yml` is the local dev stack for the monorepo. It runs Postgres, the Cosmos emulator, the backend, and the frontend together so `docker compose up` gives you the full app environment described in issue `#4`.

Note: the Cosmos DB Linux emulator image is commonly x86_64-only. The compose files pin the emulator service to `platform: linux/amd64`, which may rely on emulation or fail on ARM64 hosts without x86_64 compatibility.

`docker-compose.db.yml` is the backing-services-only variant. It starts just Postgres and the Cosmos emulator, which is useful if you want to run the backend and frontend directly from your IDE while still depending on containerized local databases.

`docker-compose.prod.yml` is the production-oriented compose file. It builds the app containers in their production targets and expects external runtime configuration such as the Cosmos endpoint/key instead of bundling the local emulator into that stack.

## Coding conventions

All formatting, linting, and style rules — for both backend (.NET / C#) and frontend (Next.js / TypeScript) — are documented in [`CONVENTIONS.md`](./CONVENTIONS.md). That file is the single source of truth; please read it before opening your first PR.

Briefly:

- **Editor settings** are shared via [`.editorconfig`](./.editorconfig) at the repo root.
- **Line endings** are normalized to LF via [`.gitattributes`](./.gitattributes).
- **Backend** uses StyleCop + Roslyn analyzers with `TreatWarningsAsErrors=true`. Any analyzer hit fails `dotnet build`. Config: [`backend/Directory.Build.props`](./backend/Directory.Build.props), [`backend/stylecop.json`](./backend/stylecop.json).
- **Frontend** uses ESLint (flat config) + Prettier. `npm run lint` runs with `--max-warnings=0`; any warning fails CI. Config: [`frontend/eslint.config.mjs`](./frontend/eslint.config.mjs), [`frontend/.prettierrc`](./frontend/.prettierrc).
- **Pre-commit hooks** (Husky + lint-staged) auto-format and lint your staged files before each commit. A failing check blocks the commit. Config: [`.lintstagedrc.mjs`](./.lintstagedrc.mjs), [`.husky/pre-commit`](./.husky/pre-commit).
- **CI** re-runs the same checks on every PR — see [`.github/workflows/ci-backend.yml`](./.github/workflows/ci-backend.yml) and [`.github/workflows/ci-frontend.yml`](./.github/workflows/ci-frontend.yml).

### One-time setup after cloning

From the **repo root**:

```powershell
# Installs husky + lint-staged and registers the pre-commit hook
npm install

# Restores .NET tools defined in .config/dotnet-tools.json (incl. dotnet-ef)
dotnet tool restore

# Backend NuGet packages, including StyleCop
dotnet restore backend

# Frontend deps
npm --prefix frontend install
```

Verify the hook is wired:

```powershell
# Should exist; on Linux/macOS it should also be executable
Get-ChildItem .husky/pre-commit
```

Proposing a change to the conventions: open a PR that edits `CONVENTIONS.md`. Discussion happens in the PR.

## Backend database persistence

The backend uses EF Core Code First with PostgreSQL, ASP.NET Core Identity, Npgsql, and NetTopologySuite/PostGIS. The local database compose services use `postgis/postgis:18-3.6-alpine` so the `postgis` extension required by the initial migration is available.

Start the backing PostgreSQL service:

```powershell
docker compose -f docker-compose.db.yml up -d db
```

If you haven't installed `dotnet-ef` globally yet, run:

```powershell
dotnet tool install -g dotnet-ef
```

Create a new migration when the model changes:

```powershell
dotnet ef migrations add InitialCreate --project backend --startup-project backend --context Backend.Infrastructure.Persistence.AppDbContext --output-dir Infrastructure\Persistence\Migrations
```

Apply migrations to the configured database:

```powershell
dotnet ef database update --project backend --startup-project backend --context Backend.Infrastructure.Persistence.AppDbContext
```
