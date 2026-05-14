import type { CompensationType, TaskCategory, TaskStatus } from "@/lib/types"

/** Display metadata for task categories. Backed by `TaskCategory` for type safety. */
export const TASK_CATEGORIES: Record<
  TaskCategory,
  { label: string; description: string }
> = {
  moving: { label: "Moving", description: "Help carrying, packing, lifting" },
  tutoring: {
    label: "Tutoring",
    description: "Lessons, study help, exam prep",
  },
  household: {
    label: "Household",
    description: "Small repairs, cleaning, assembly",
  },
  petcare: { label: "Pet care", description: "Sitting, walking, feeding" },
  tools: { label: "Tool lending", description: "Borrow or share equipment" },
  tech: { label: "Tech help", description: "Setup, troubleshooting, devices" },
  errands: { label: "Errands", description: "Quick favors, deliveries" },
  other: { label: "Other", description: "Anything else" },
}

export const TASK_CATEGORY_LIST = Object.keys(TASK_CATEGORIES) as TaskCategory[]

export const COMPENSATION_LABELS: Record<CompensationType, string> = {
  paid: "Paid",
  voluntary: "Voluntary",
  barter: "Barter",
}

export const TASK_STATUS_LABELS: Record<TaskStatus, string> = {
  open: "Open",
  in_progress: "In progress",
  completed: "Completed",
  reviewed: "Reviewed",
  cancelled: "Cancelled",
}

/** Recency filter buckets for the Helper feed search. */
export const RECENCY_OPTIONS = [
  { value: "any", label: "Any time" },
  { value: "24h", label: "Last 24 hours" },
  { value: "7d", label: "Last 7 days" },
  { value: "30d", label: "Last 30 days" },
] as const

export const APP_NAME = "2gather"
export const APP_TAGLINE = "Local help, structured trust."
