"use client"

import * as React from "react"
import { HugeiconsIcon } from "@hugeicons/react"
import { Search01Icon } from "@hugeicons/core-free-icons"

import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  COMPENSATION_LABELS,
  RECENCY_OPTIONS,
  TASK_CATEGORIES,
  TASK_CATEGORY_LIST,
} from "@/lib/constants"
import type { CompensationType, TaskCategory } from "@/lib/types"

export interface TaskFiltersState {
  query: string
  category: TaskCategory | "all"
  compensation: CompensationType | "all"
  recency: (typeof RECENCY_OPTIONS)[number]["value"]
}

interface TaskFiltersProps {
  value: TaskFiltersState
  onChange: (value: TaskFiltersState) => void
}

/**
 * Combined search + filter bar (US-02 proximity, US-04 category,
 * US-09 skill match, US-12 compensation transparency).
 *
 * Controlled component; the parent page owns the filter state.
 *
 * Accessibility notes:
 *  - The search input has only a decorative icon and a placeholder,
 *    so it needs an explicit `aria-label` to give it an accessible
 *    name (placeholders don't qualify as labels).
 *  - The icon is marked `aria-hidden` so screen readers don't try to
 *    announce it as content.
 *  - Each Select trigger shows its current value (e.g. "All
 *    categories"), but without context a screen-reader user can't
 *    tell which dimension it filters. `aria-label` on the trigger
 *    fills that gap.
 */
export function TaskFilters({ value, onChange }: TaskFiltersProps) {
  const update = (patch: Partial<TaskFiltersState>) =>
    onChange({ ...value, ...patch })

  return (
    <div className="flex flex-col gap-3 sm:grid sm:grid-cols-[1fr_auto_auto_auto]">
      <div className="relative">
        <HugeiconsIcon
          icon={Search01Icon}
          aria-hidden="true"
          className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground"
        />
        <Input
          type="search"
          aria-label="Search tasks, skills, locations"
          className="pl-9"
          placeholder="Search tasks, skills, locations..."
          value={value.query}
          onChange={(e) => update({ query: e.target.value })}
        />
      </div>

      <Select
        value={value.category}
        onValueChange={(v) =>
          update({ category: v as TaskFiltersState["category"] })
        }
      >
        <SelectTrigger aria-label="Filter by category" className="sm:w-44">
          <SelectValue placeholder="Category" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All categories</SelectItem>
          {TASK_CATEGORY_LIST.map((c) => (
            <SelectItem key={c} value={c}>
              {TASK_CATEGORIES[c].label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <Select
        value={value.compensation}
        onValueChange={(v) =>
          update({ compensation: v as TaskFiltersState["compensation"] })
        }
      >
        <SelectTrigger aria-label="Filter by compensation" className="sm:w-40">
          <SelectValue placeholder="Compensation" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">Any compensation</SelectItem>
          {(Object.keys(COMPENSATION_LABELS) as CompensationType[]).map(
            (key) => (
              <SelectItem key={key} value={key}>
                {COMPENSATION_LABELS[key]}
              </SelectItem>
            )
          )}
        </SelectContent>
      </Select>

      <Select
        value={value.recency}
        onValueChange={(v) =>
          update({ recency: v as TaskFiltersState["recency"] })
        }
      >
        <SelectTrigger aria-label="Filter by recency" className="sm:w-40">
          <SelectValue placeholder="Recency" />
        </SelectTrigger>
        <SelectContent>
          {RECENCY_OPTIONS.map((opt) => (
            <SelectItem key={opt.value} value={opt.value}>
              {opt.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  )
}

export const DEFAULT_FILTERS: TaskFiltersState = {
  query: "",
  category: "all",
  compensation: "all",
  recency: "any",
}
