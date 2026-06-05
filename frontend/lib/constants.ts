import type { CompensationType, TaskCategory } from "@/lib/types"

/**
 * Stable ordering for task categories. Labels are intentionally not stored
 * here — consumers resolve them through next-intl (`tasks.categories.{id}`)
 * so the same id renders correctly in every locale.
 */
export const TASK_CATEGORY_LIST: readonly TaskCategory[] = [
  "moving",
  "tutoring",
  "household",
  "petcare",
  "tools",
  "tech",
  "errands",
  "other",
]

/**
 * Reward options offered when posting a task. The values map 1:1 to the
 * `CompensationType` discriminant (except `voluntary`, which is not user-
 * selectable). Labels are resolved at render time via
 * `tasks.compensation.{value}`.
 */
export const COMPENSATION_OPTIONS = [
  "points",
  "paid",
  "barter",
] as const satisfies readonly Exclude<CompensationType, "voluntary">[]

/**
 * Recency filter buckets for the feed search. Labels are resolved through
 * `tasks.filters.recencyOptions.{value}` so the dropdown renders in the
 * active locale.
 */
export const RECENCY_OPTIONS = ["any", "24h", "7d", "30d"] as const

export const APP_NAME = "2gather"
