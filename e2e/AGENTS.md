# AGENTS.md — e2e

End-to-end tests built with [Playwright](https://playwright.dev). Drives a real browser against the running backend + frontend, so the full HTTP/cookie/JWT/refresh path is exercised — not a mocked stand-in.

This package lives at the repo root (alongside `backend.Tests/`) rather than under `frontend/` because the suite covers the whole stack, not just the UI.

See the [root AGENTS.md](../AGENTS.md) for monorepo-wide concerns.

## Scope

- Happy-path coverage of authentication: register and login.
- Tests run against `http://localhost:3000` by default; override with `BASE_URL`.

The suite intentionally stays small — it's the smoke test for the running stack, not a substitute for unit/integration tests in `backend.Tests/`.

## Stack expectations

The browser hits the frontend, which proxies `/api/*` to the backend. For the suite to pass you need all three running:

1. **Postgres** (PostGIS) on `5432`.
2. **Cosmos emulator** on `8081` — the backend health-checks Cosmos at startup and exits if it's unreachable. See the [root README](../README.md) for the platform caveats.
3. **Backend** in `Development` mode on `5073` (or whatever you point `API_URL` at). Development mode auto-applies migrations and seeds the dev users (`user@local.test` / `User123!` and `admin@local.test` / `Admin123!`) — the login test relies on the regular-user seed.
4. **Frontend** (`next start` after `next build`) on `3000` with `API_URL` pointing at the backend.

## Setup

```bash
npm --prefix e2e install
npm --prefix e2e exec playwright install --with-deps chromium
```

`--with-deps` needs sudo on Linux to install system libraries; on Windows/macOS it's a no-op and the browser binary is fetched into the user cache.

## Running locally

The two recommended flows:

### Full stack via Docker Compose (closest to CI)

```bash
# From the repo root
docker compose up -d
# Wait for the backend to become healthy
curl --retry 60 --retry-delay 2 --retry-all-errors http://localhost:8080/health
# Frontend is reachable at http://localhost:3000 — it proxies /api/* to backend:8080
BASE_URL=http://localhost:3000 npm --prefix e2e test
```

### Native runners (faster iteration)

```bash
# 1. Backing services only
docker compose -f docker-compose.db.yml up -d

# 2. Backend (Development env auto-migrates + seeds)
ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS=http://localhost:5073 \
dotnet run --project backend

# 3. Frontend (in a second shell)
npm --prefix frontend run build
API_URL=http://localhost:5073 npm --prefix frontend start

# 4. Tests (in a third shell)
npm --prefix e2e test
```

Helpful variants:

```bash
npm --prefix e2e run test:headed   # See the browser
npm --prefix e2e run test:ui       # Time-travel debugger
npm --prefix e2e run report        # Open the last HTML report
```

## CI

[`.github/workflows/ci-e2e.yml`](../.github/workflows/ci-e2e.yml) brings up the database stack, builds and runs the backend natively, builds and starts the frontend natively, then runs this suite. On failure it uploads the Playwright HTML report and dumps backend/frontend logs as artifacts for triage.

## Conventions

- **Locators** — prefer role/label-based locators (`getByRole`, `getByLabel`) over CSS selectors. They survive Tailwind class churn and double as accessibility checks.
- **Test data** — register tests must use a unique email per run (`uniqueEmail()` in `tests/auth.spec.ts`) so they're idempotent against a non-reset database.
- **Seeded users** — login tests use `user@local.test` / `User123!`. Don't bake other credentials into tests; if you need a different account, provision it through the registration flow.
- **No floating waits** — `waitForURL`, `expect(locator).toBeVisible()`, etc. Avoid `page.waitForTimeout`.
- **TS** — `strict: true`. Same Prettier rules as the rest of the repo (no semis, double quotes, 2-space).

## Troubleshooting

- **`net::ERR_CONNECTION_REFUSED`** — the frontend (or backend) isn't up. Check `docker compose ps` and the backend logs.
- **Login test fails with "incorrect"** — `DevSeed.Enabled` is off, or the backend isn't running in `Development`. Check `appsettings.Development.json` and `ASPNETCORE_ENVIRONMENT`.
- **Browser binaries missing** — re-run `npm --prefix e2e exec playwright install`.
