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

export default {
  // ---------------------------------------------------------------------
  // Frontend: TypeScript / JavaScript. ESLint then Prettier.
  // ---------------------------------------------------------------------
  "frontend/**/*.{ts,tsx,js,jsx,mjs,cjs}": (files) => {
    const rel = quote(projectRelative("frontend")(files))
    return [
      `npm --prefix frontend exec -- eslint --max-warnings=0 --fix ${rel}`,
      `npm --prefix frontend exec -- prettier --write ${rel}`,
    ]
  },

  // ---------------------------------------------------------------------
  // Frontend: non-code files. Prettier only.
  // ---------------------------------------------------------------------
  "frontend/**/*.{json,md,css,yml,yaml}": (files) => {
    const rel = quote(projectRelative("frontend")(files))
    return [`npm --prefix frontend exec -- prettier --write ${rel}`]
  },

  // ---------------------------------------------------------------------
  // Backend: C# files. dotnet format scoped to staged files.
  // (Build-time analyzers like StyleCop run in CI / on `dotnet build`.)
  // ---------------------------------------------------------------------
  "backend/**/*.cs": (files) => {
    const rel = projectRelative("backend")(files)
    return [`dotnet format backend --include ${quote(rel)}`]
  },
}
