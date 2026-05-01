import path from "node:path"

// lint-staged passes absolute paths. Convert them to paths relative to each
// sub-project so every tool runs from the same working directory in CI,
// Windows shells, and POSIX shells.
const projectRelative = (projectDir) => (files) => {
  const root = path.resolve(projectDir)
  return files.map((file) =>
    path.relative(root, path.resolve(file)).split(path.sep).join("/")
  )
}

const quote = (paths) =>
  paths.map((pathName) => JSON.stringify(pathName)).join(" ")

// ESLint 9 resolves file arguments as glob patterns. Convert meta-characters
// to bracket expressions so Next.js route-group dirs like `(auth)` and
// dynamic segments like `[id]` are matched as literals (no backslashes —
// they double-escape through JSON.stringify and break on Windows).
const quoteForEslint = (paths) =>
  paths
    .map((p) =>
      p.replace(/\[/g, "[[]").replace(/\(/g, "[(]").replace(/\)/g, "[)]")
    )
    .map((p) => JSON.stringify(p))
    .join(" ")

export default {
  // ---------------------------------------------------------------------
  // Frontend: TypeScript / JavaScript. ESLint then Prettier.
  // ---------------------------------------------------------------------
  "frontend/**/*.{ts,tsx,js,jsx,mjs,cjs}": (files) => {
    const rel = projectRelative("frontend")(files)
    // `npm run --prefix` changes CWD to the package root before exec,
    // unlike `npm exec --prefix` which only affects binary lookup.
    return [
      `npm --prefix frontend run lint:fix -- ${quoteForEslint(rel)}`,
      `npm --prefix frontend run format:staged -- ${quote(rel)}`,
    ]
  },

  // ---------------------------------------------------------------------
  // Frontend: non-code files. Prettier only.
  // ---------------------------------------------------------------------
  "frontend/**/*.{json,md,css,yml,yaml}": (files) => {
    const rel = quote(projectRelative("frontend")(files))
    return [`npm --prefix frontend run format:staged -- ${rel}`]
  },

  // ---------------------------------------------------------------------
  // Backend: C# files in app and tests. dotnet format scoped to staged files.
  // (Build-time analyzers like StyleCop run in CI / on `dotnet build`.)
  // ---------------------------------------------------------------------
  "{backend,backend.Tests}/**/*.cs": (files) => {
    const rel = projectRelative("backend")(files)
    return [`dotnet format backend --include ${quote(rel)}`]
  },
}
