# 2gather — Collaboration in Local Communities

**2gather** is a bilingual (HU/EN) neighbour-help platform. Every member is both a
**Seeker** (post a task) and a **Helper** (accept one). Discover people nearby,
agree the terms in chat, complete the task, and build a public reputation —
turning a neighbourhood into a self-sustaining micro-cooperation community.

> Course project for *Advanced Software Technology*. See the assignment background
> and the project results write-up in [`docs/results/`](docs/results/AdvSoftTech_Assignment-Results.md).

## What it does

- **Post a task** — title, description, category, location, and compensation type
  (paid / barter / voluntary), visible to helpers immediately.
- **Find help nearby** — Helper Feed with category, distance, and compensation
  filters; location-aware results via PostGIS proximity.
- **Skill matching** — tasks matching a helper's listed skills surface first.
- **Chat before meeting** — one-on-one realtime messaging (SignalR) opens once
  interest is expressed.
- **Reputation** — star ratings + written reviews; profiles show average rating,
  completed-task count, and recent reviews.
- **Points** — helpers earn platform points on completed tasks.
- **Bilingual** — full Hungarian and English UI.

## Try it

- **Live app:** _‹production URL — to be confirmed›_
- **Local:** `docker compose up` runs the full stack (backend + frontend +
  Postgres + Cosmos emulator). Register → accept Terms → complete your profile →
  post a task → switch to the Helper Feed → accept → chat → complete → review.

See [Local dev stack](#local-dev-stack) below for variants and IDE-based workflows.

## Tech stack

| Layer | Technology |
|---|---|
| Frontend | Next.js (App Router), TypeScript, Tailwind, shadcn/ui |
| Backend | ASP.NET Core Web API (.NET), EF Core, ASP.NET Identity, JWT |
| Data | PostgreSQL + PostGIS (relational + geo), Cosmos DB (chat messages) |
| Realtime | Azure SignalR (chat) |
| Infra | Azure Container Apps (backend), Vercel (frontend), Blob Storage, Key Vault |
| CI/CD | GitHub Actions (CI, E2E, CD), Qodana, Codecov |

## Project management

- **Issues / bug tracker:** [GitHub Issues](https://github.com/kovacsmate3/collaboration-in-local-communities/issues)
- **Board:** GitHub Projects (Kanban), with work organised into Sprint 0–4 milestones.
- **Conventions:** [`CONVENTIONS.md`](./CONVENTIONS.md) — formatting, linting, and contribution workflow.

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

## End-to-end tests

A Playwright suite lives in [`e2e/`](./e2e) and exercises the full running stack (backend + frontend + Postgres + Cosmos). It runs in CI on every PR via [`.github/workflows/ci-e2e.yml`](./.github/workflows/ci-e2e.yml). See [`e2e/AGENTS.md`](./e2e/AGENTS.md) for how to run it locally.

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

## Seeding demo data

A one-shot seeder populates a fresh **development** database with realistic demo
content: a couple of fixed login accounts, ten sample neighbour ("seeker")
accounts, and 50 curated Budapest community tasks spread across every category,
compensation type, and lifecycle status.

With Postgres running (see above), from the **repo root**:

```powershell
npm run seed
```

This runs `dotnet run --project backend -- seed`, which applies any pending
migrations, runs the seeders, and exits without starting the web host. The
seeders also run automatically on normal backend startup in Development, so a
plain `dotnet run` (or `docker compose up`) seeds too. Seeding is **idempotent**
— each demo task carries a stable `DEMO-####` public code, so re-running never
duplicates rows.

### Development-only by design

The demo accounts and tasks are **never created outside Development**. Seeders
are only registered when `ASPNETCORE_ENVIRONMENT=Development`; in any other
environment none of the seed accounts or tasks are inserted. Two config switches
(section `DevSeed`) let you opt out even in Development:

| Setting                    | Effect                                    |
| -------------------------- | ----------------------------------------- |
| `DevSeed:Enabled=false`    | Disables **all** dev seeders.             |
| `DevSeed:SampleTasks:Enabled=false` | Disables just the 50 sample tasks (and their seeker accounts). |

### Seeded login credentials

All seeded accounts use addresses on the reserved `.test` TLD, so they can never
collide with a real email. The two fixed accounts can be overridden via the
`DevSeed:Admin` / `DevSeed:User` config sections.

| Role  | Email                | Password    |
| ----- | -------------------- | ----------- |
| Admin | `admin@local.test`   | `Admin123!` |
| User  | `user@local.test`    | `User123!`  |

The ten sample seeker accounts all share the password `Seed123!`:

| Name      | Email                    |
| --------- | ------------------------ |
| Anna K.   | `seed.anna@local.test`   |
| Bence T.  | `seed.bence@local.test`  |
| Csilla M. | `seed.csilla@local.test` |
| Dániel P. | `seed.daniel@local.test` |
| Eszter V. | `seed.eszter@local.test` |
| Gábor Sz. | `seed.gabor@local.test`  |
| Hanna B.  | `seed.hanna@local.test`  |
| István L. | `seed.istvan@local.test` |
| Júlia N.  | `seed.julia@local.test`  |
| Márk H.   | `seed.mark@local.test`   |
