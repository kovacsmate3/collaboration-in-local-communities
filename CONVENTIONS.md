# Coding Conventions

This document is the source of truth for how we write and ship code in this
repository. The project wiki should link here; do not duplicate rules into the wiki.

If a convention is not enforced by tooling, it is enforced by code review.

---

## TL;DR

- **Editor settings** — `.editorconfig` at the repo root. Every IDE and `dotnet
format` reads this.
- **Backend (`/backend`, .NET 10 / C#)** — StyleCop + Roslyn analyzers, with
  `TreatWarningsAsErrors=true`. The build fails on any analyzer hit.
- **Frontend (`/frontend`, Next.js 16 / TS)** — ESLint (flat config) and
  Prettier. `npm run lint` runs with `--max-warnings=0`; any warning fails CI.
- **Pre-commit hooks** — Husky + lint-staged auto-format and lint staged files
  before every commit. A failing lint blocks the commit.
- **CI** — `Backend CI` and `Frontend CI` (in `.github/workflows`) re-run the
  same checks on every PR. CI is the final word; the hook is a fast feedback
  loop, not a guarantee.

---

## One-time setup (every developer)

After cloning, from the **repo root**:

```bash
# Installs husky + lint-staged and registers the git hook
npm install

# Restores the .NET tools defined in .config/dotnet-tools.json (incl. dotnet-ef)
dotnet tool restore

# Restores backend NuGet packages, including StyleCop
dotnet restore backend

# Frontend deps
npm --prefix frontend install
```

Verify the hook is wired:

```bash
ls -la .husky/pre-commit   # should exist and be executable
```

---

## Repository layout

```
/
├── .editorconfig            # Shared editor + C# style rules
├── .husky/                  # Git hooks managed by husky
├── .lintstagedrc.mjs        # What runs on staged files at commit time
├── CONVENTIONS.md           # ← this file
├── package.json             # Root, dev tooling only (husky, lint-staged)
│
├── backend/                 # ASP.NET Core 10 Web API
│   ├── Directory.Build.props  # Shared MSBuild props for all backend projects
│   ├── stylecop.json          # StyleCop config
│   └── backend.csproj
│
├── frontend/                # Next.js 16 + React 19 + Tailwind v4
│   ├── eslint.config.mjs
│   ├── .prettierrc
│   └── package.json
│
└── .github/workflows/       # CI / CD pipelines
```

The repo is a single monorepo with two deployable apps. Do not split it.

---

## Backend (.NET / C#)

### Tooling

| Tool               | Configured in           | Purpose                             |
| ------------------ | ----------------------- | ----------------------------------- |
| `.editorconfig`    | repo root               | Whitespace, naming, language style  |
| StyleCop.Analyzers | `Directory.Build.props` | Layout / documentation rules        |
| Roslyn analyzers   | `Directory.Build.props` | `latest-recommended` analysis level |
| `dotnet format`    | reads `.editorconfig`   | Auto-fixer used by hook + CI        |

`TreatWarningsAsErrors=true` is set in `Directory.Build.props`, so any analyzer
warning fails `dotnet build`. CI runs `dotnet build --configuration Release`,
so a violation that escapes the pre-commit hook still fails the PR.

### Style cheat sheet

- **Namespaces** — file-scoped (`namespace Foo;`), one per file.
- **Usings** — outside the namespace, `System.*` first, no blank lines between
  groups.
- **Naming**
  - Types, methods, properties, events: `PascalCase`
  - Interfaces: `IPascalCase`
  - Local variables and parameters: `camelCase`
  - Private fields: `_camelCase`
  - Constants: `PascalCase`
- **Modifiers** — always explicit. `internal class Foo`, never `class Foo`.
- **Nullability** — `Nullable` is enabled. Don't `!`-suppress unless you have a
  comment justifying why the compiler is wrong.
- **Async** — methods returning `Task` end in `Async`. `async void` is banned
  except for event handlers.
- **`var`** — fine when the type is obvious from the right-hand side; use the
  explicit type otherwise.
- **Braces** — always, even on single-line bodies.
- **XML doc comments** — required on public APIs that ship in libraries; not
  required on internal types or controllers.

### Project structure

We follow the layering hinted at in the existing migration command
(`Backend.Infrastructure.Persistence.AppDbContext`):

- `Domain/` — entities, value objects, domain interfaces. No external deps.
- `Application/` — use cases, DTOs, application interfaces.
- `Infrastructure/` — EF Core, Cosmos, external services, identity.
- `Api/` (or root) — controllers, minimal API endpoints, DI wiring.

Keep dependencies pointing inward (Api → Application → Domain).

### Migrations

EF Core migrations live in `backend/Infrastructure/Persistence/Migrations` and
are created with the command in `README.md`. One migration per logical change;
write a meaningful name (`AddPostgisExtension`, not `Update1`).

---

## Frontend (Next.js / TypeScript)

### Tooling

| Tool               | Configured in                | Purpose                                |
| ------------------ | ---------------------------- | -------------------------------------- |
| ESLint flat config | `frontend/eslint.config.mjs` | Linting + TS rules                     |
| Prettier           | `frontend/.prettierrc`       | Formatting (incl. Tailwind class sort) |
| TypeScript         | `frontend/tsconfig.json`     | `strict: true`                         |

`npm run lint` runs with `--max-warnings=0`. Anything yellow becomes red.

### Style cheat sheet

- **Files** — `PascalCase.tsx` for React components, `kebab-case.ts` for
  modules and utilities, `page.tsx` / `layout.tsx` for App Router files.
- **Components** — function components only. Default-exported page/layout
  components for App Router; named exports everywhere else.
- **Imports** — use the `@/` alias for absolute imports inside the app. Use
  `import type { … }` for type-only imports (the rule is auto-fixable).
- **`any`** — banned. If you genuinely need an escape hatch, use `unknown` and
  narrow.
- **Promises** — `await` them or chain `.catch`. No floating promises in
  request handlers.
- **`console.log`** — banned. `console.warn` / `console.error` allowed.
- **Equality** — `===` always.
- **JSX** — no curly braces around literal string props (`<X title="hi" />`,
  not `<X title={"hi"} />`). Self-close empty elements.
- **State** — prefer Server Components; reach for `"use client"` only when you
  need browser APIs, state, or effects.
- **Styling** — Tailwind utility classes; no inline `style={{ … }}` except for
  dynamic values that can't be expressed as classes. The `cn()` helper from
  `@/lib/utils` handles conditional classes; `prettier-plugin-tailwindcss`
  sorts them.

### Components

- Generated shadcn/ui components live in `components/ui/`. They are vendored
  code and excluded from lint. Don't edit them by hand; re-run
  `npx shadcn@latest add <component>` and re-apply local changes if you must
  customise.
- Custom components live in `components/` (top level for app-wide pieces) or
  beside the page that owns them.

---

## Git workflow

### Branches

- `main` — protected, deployable. Merges only via PR with green CI.
- `development` — integration branch (used by both CI workflows).
- Feature branches: `feature/<short-kebab-description>`
- Bug fixes: `fix/<short-kebab-description>`
- Chores / infra: `chore/<short-kebab-description>`

### Commits

Use [Conventional Commits](https://www.conventionalcommits.org/). The pattern:

```
<type>(<optional scope>): <imperative summary>

<optional body>

<optional footer, e.g. Closes #42>
```

Common types: `feat`, `fix`, `docs`, `chore`, `refactor`, `test`, `ci`,
`build`. Scope is the area (`backend`, `frontend`, `db`, `ci`, …).

Examples:

```
feat(backend): add /events endpoint for community calendar
fix(frontend): correct hydration mismatch on theme toggle
chore(ci): bump setup-dotnet to v5
```

This is a code-review convention for now. We may add commitlint later.

### Pull requests

- Title follows the same Conventional Commits format as commits.
- Description explains _why_ the change is needed; the diff explains _what_.
- Link the issue (`Closes #N`).
- Keep PRs small. If you're touching more than ~400 lines outside generated
  files, split it.
- Both `Backend CI` and `Frontend CI` must be green. CI is mandatory, not
  advisory.

---

## Pre-commit hook — what gets enforced

The hook (`.husky/pre-commit`) runs `lint-staged`, which runs **only on the
files you have staged**:

| Staged file pattern                     | What runs                                                |
| --------------------------------------- | -------------------------------------------------------- |
| `frontend/**/*.{ts,tsx,js,jsx,mjs,cjs}` | `eslint --max-warnings=0 --fix`, then `prettier --write` |
| `frontend/**/*.{json,md,css,yml,yaml}`  | `prettier --write`                                       |
| `backend/**/*.cs`                       | `dotnet format --include <files>`                        |

Auto-fixes are re-staged automatically. If a check still fails after
auto-fixing (e.g. a real ESLint error or a StyleCop violation), the commit
is blocked. Fix it, stage, commit again.

### Bypassing the hook

`git commit --no-verify` exists. Don't use it. If you genuinely need to (a
WIP commit on a personal branch you intend to squash), make sure CI passes
before opening the PR — the same checks run there.

---

## Troubleshooting

**The hook didn't run.**
Did you run `npm install` at the repo root? Husky registers itself via the
`prepare` script. If it's still not running:

```bash
npx husky init
chmod +x .husky/pre-commit
```

**`dotnet format` reports changes I didn't make.**
Someone landed code that didn't pass `dotnet format`. Run
`dotnet format backend` once on a clean branch and commit the result.

**ESLint complains about generated shadcn components.**
It shouldn't — `components/ui/**` is in the global ignore list. If a new
generated path appears, add it to `globalIgnores` in `eslint.config.mjs`.

**StyleCop is too noisy.**
Don't disable rules in individual files with `#pragma`. Add the diagnostic
ID to the `dotnet_diagnostic.<ID>.severity = none` block in `.editorconfig`
and open a short PR explaining why. Reviewers will push back on disabling
rules without justification.

---

## Changing these conventions

Open a PR that edits this file. Discussion happens in the PR. Once merged,
the wiki link can point at the new revision.
