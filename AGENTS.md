# AGENTS.md

`collaboration-in-local-communities` is a monorepo with two deployable apps that ship together via Docker Compose.

- [`backend/`](./backend) — ASP.NET Core 10 Web API. See [`backend/AGENTS.md`](./backend/AGENTS.md).
- [`frontend/`](./frontend) — Next.js 16 app. See [`frontend/AGENTS.md`](./frontend/AGENTS.md).
- [`backend.Tests/`](./backend.Tests) — xUnit v3 tests for the backend.

When working under `backend/` or `frontend/`, the nested `AGENTS.md` wins — read that one first. This file covers the repo-wide concerns.

## Setup

```bash
# Backend
dotnet tool restore                  # installs dotnet-ef from .config/dotnet-tools.json
dotnet restore                       # restores backend + backend.Tests via the .slnx

# Frontend
npm --prefix frontend install
```

## Local dev stack

Three compose files at the repo root — pick the one that matches what you're doing:

| File                       | Use when                                                                  |
| -------------------------- | ------------------------------------------------------------------------- |
| `docker-compose.yml`       | Full stack (Postgres + Cosmos + backend + frontend). `docker compose up`. |
| `docker-compose.db.yml`    | Only databases. Run apps from your IDE while DBs stay containerized.      |
| `docker-compose.prod.yml`  | Production-target builds. Expects external Cosmos endpoint/key.           |

Caveats:

- The Cosmos DB Linux emulator image is x86_64-only. The compose files pin `platform: linux/amd64`; ARM64 hosts will rely on emulation or fail.
- The Postgres image is `postgis/postgis:18-3.6-alpine` because the initial migration creates the `postgis` extension. Don't swap it for plain `postgres`.

## Git workflow

- `main` is protected and deployable; merges only via PR with green CI.
- `development` is the integration branch — CI runs on PRs targeting either `main` or `development`.
- Branch naming: `feature/<kebab>`, `fix/<kebab>`, `chore/<kebab>`, `hotfix/<kebab>`.
- Use [Conventional Commits](https://www.conventionalcommits.org/) for commit messages and PR titles. Scope by area: `feat(backend): …`, `fix(frontend): …`, `chore(ci): …`.
- Keep PRs small. Link the issue (`Closes #N`). Description explains the *why*; the diff shows the *what*.

## CI

| Workflow                                                              | Runs                                                                                                |
| --------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| [`.github/workflows/ci-backend.yml`](./.github/workflows/ci-backend.yml)   | `dotnet restore` → `dotnet build -c Release` → `dotnet test` → `dotnet format --verify-no-changes` |
| [`.github/workflows/ci-frontend.yml`](./.github/workflows/ci-frontend.yml) | Node 24: `npm ci` → `npm run lint` → `npm run build`                                                |

CI is the final word — run the equivalent commands locally before pushing.

## What lives at the root

- `collaboration-in-local-communities.slnx` — solution file referencing both `backend.csproj` and `backend.Tests.csproj`. Use it for restoring/building both projects in one shot.
- `.env.example` — template for required environment variables. Copy to `.env` for local dev.
- `docs/wireframes/` — Sprint 0 wireframe PDFs. The [Figma file](https://www.figma.com/design/WGRPJE8LIU1EBtQVzRgpK5) is the source of truth; each frame maps to a route under `frontend/app/`.
