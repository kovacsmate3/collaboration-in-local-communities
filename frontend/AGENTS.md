<!-- BEGIN:nextjs-agent-rules -->

# This is NOT the Next.js you know

This version has breaking changes — APIs, conventions, and file structure may all differ from your training data. Read the relevant guide in `node_modules/next/dist/docs/` before writing any code. Heed deprecation notices.

<!-- END:nextjs-agent-rules -->

---

# AGENTS.md — frontend

Next.js 16 (App Router, Turbopack) + React 19 + TypeScript (strict) + Tailwind v4 + shadcn/ui. The frontend is part of the monorepo — see the [root AGENTS.md](../AGENTS.md) for compose, git workflow, and CI.

## Stack

- **Framework** — `next` ^16.2.4 with the App Router. `next dev --turbopack` is the default dev command.
- **UI** — React 19, [shadcn/ui](https://ui.shadcn.com) (`style: radix-vega`, base color `neutral`, CSS variables). Icons from `@hugeicons/react`.
- **Styling** — Tailwind v4 via `@tailwindcss/postcss`. Tokens live in `app/globals.css`. Class merging uses `cn()` from `@/lib/utils` (clsx + tailwind-merge); `class-variance-authority` for variants.
- **Theming** — `next-themes` through `components/theme-provider.tsx`.
- **Lint/format** — ESLint 9 flat config (`eslint-config-next` core-web-vitals + typescript), Prettier 3 with `prettier-plugin-tailwindcss`.

## Setup

From the repo root:

```bash
npm --prefix frontend install
```

Or from this directory:

```bash
npm install
```

## Dev · build · lint · format · typecheck

Run from `frontend/`:

```bash
npm run dev          # next dev --turbopack — http://localhost:3000
npm run build        # next build (CI runs this)
npm run start        # next start (after build)
npm run lint         # eslint (CI gate)
npm run format       # prettier --write "**/*.{ts,tsx}"
npm run typecheck    # tsc --noEmit
```

The `NEXT_PUBLIC_API_URL` env var points at the backend; compose sets it to `http://localhost:8080` and that's the local default.

## Directory layout

```
frontend/
├── app/                          # App Router root
│   ├── layout.tsx                # Root layout — fonts, ThemeProvider, metadata
│   ├── page.tsx, not-found.tsx, globals.css
│   ├── (auth)/                   # Route group for auth screens (login, register)
│   └── (main)/                   # Route group for the authenticated app shell
├── components/
│   ├── ui/                       # shadcn-generated primitives — DO NOT hand-edit
│   ├── auth/  layout/  messages/  profile/  shared/  tasks/   # feature components
│   └── theme-provider.tsx
├── hooks/                        # e.g. use-is-mobile.ts
├── lib/                          # constants, format, mock-data, nav, types, utils
├── public/
├── components.json               # shadcn registry config
├── eslint.config.mjs
├── .prettierrc, .prettierignore
├── next.config.ts
└── tsconfig.json                 # strict, paths: "@/*" → "./*"
```

Each frame in the [Figma design](https://www.figma.com/design/WGRPJE8LIU1EBtQVzRgpK5) maps to a route under `app/`. Check the "Frame ↔ route map" panel in the desktop Figma page when adding a new screen.

## Conventions

- **File naming** — `PascalCase.tsx` for components; `kebab-case.ts` for modules; `page.tsx` / `layout.tsx` / `not-found.tsx` for App Router.
- **Components** — function components only. Default export for App Router pages/layouts; named exports everywhere else.
- **Imports** — use the `@/` alias for absolute imports (`@/lib/utils`, `@/components/...`). Use `import type { … }` for type-only imports.
- **Server vs client** — prefer Server Components. Add `"use client"` only when you need browser APIs, state, or effects.
- **TS** — `strict: true`. No `any`; use `unknown` and narrow.
- **Equality** — `===` only.
- **Promises** — `await` them or chain `.catch`. No floating promises.
- **`console.log`** — avoid; `console.warn` / `console.error` are fine.
- **Styling** — Tailwind utility classes; no inline `style={{ … }}` except for genuinely dynamic values. Use `cn()` for conditional classes; `prettier-plugin-tailwindcss` sorts them on save.

### Prettier (non-default settings)

`.prettierrc` deviates from defaults — match these or `npm run format` will reformat your code:

- `semi: false` — no semicolons.
- `singleQuote: false` — **double** quotes (yes, double).
- `tabWidth: 2`, `printWidth: 80`, `trailingComma: "es5"`, `endOfLine: "lf"`.
- Tailwind plugin reads `app/globals.css` and treats `cn` and `cva` as class-receiving functions.

## shadcn/ui components

`components/ui/` is vendored, generator-owned code. Don't hand-edit. To add one:

```bash
npx shadcn@latest add <component>
```

Configuration lives in [`components.json`](./components.json) (style `radix-vega`, neutral base color, hugeicons, `@/` aliases). If the generator overwrites local tweaks, re-apply them after the regen.

Custom components go in `components/<feature>/` (one folder per feature area: `auth`, `layout`, `messages`, `profile`, `shared`, `tasks`).

## Mock data

`lib/mock-data.ts` provides fixtures for screens that aren't wired to the backend yet. Treat it as a stand-in until the corresponding endpoint exists; don't ship it in production code paths. `lib/types.ts` holds the shared TypeScript types those fixtures and components depend on.
