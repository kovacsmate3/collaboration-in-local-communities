"use client"

import * as React from "react"

import { PageHeader } from "@/components/shared/page-header"
import {
  DEFAULT_FILTERS,
  TaskFilters,
  type TaskFiltersState,
} from "@/components/tasks/task-filters"
import { TaskList } from "@/components/tasks/task-list"
import { useTaskList } from "@/lib/api/tasks"
import type { ApiTask } from "@/lib/api/tasks"

export function HelperPageClient() {
  const [filters, setFilters] =
    React.useState<TaskFiltersState>(DEFAULT_FILTERS)

  const {
    data: allTasks = [],
    isLoading,
    isError,
  } = useTaskList({ status: "Open" })

  const tasks = React.useMemo(
    () => applyFilters(allTasks, filters),
    [allTasks, filters]
  )

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Find someone to help"
        description="Open requests filtered by your skills, location, and availability."
      />
      <TaskFilters value={filters} onChange={setFilters} />
      {isLoading ? (
        <p className="text-sm text-muted-foreground">Loading tasks…</p>
      ) : isError ? (
        <p className="text-sm text-destructive">
          Could not load tasks. Please try again.
        </p>
      ) : (
        <TaskList
          tasks={tasks}
          emptyTitle="No tasks match your filters"
          emptyDescription="Try widening the category or recency range."
          hideStatus
        />
      )}
    </div>
  )
}

function applyFilters(tasks: ApiTask[], filters: TaskFiltersState): ApiTask[] {
  return tasks
    .filter((t) => {
      if (!filters.query) return true
      const q = filters.query.toLowerCase()
      return (
        t.title.toLowerCase().includes(q) ||
        t.description.toLowerCase().includes(q) ||
        (t.locationText ?? "").toLowerCase().includes(q)
      )
    })
    .filter((t) =>
      filters.category === "all"
        ? true
        : t.categoryName.toLowerCase() === filters.category.toLowerCase()
    )
    .filter((t) =>
      filters.compensation === "all"
        ? true
        : t.compensationType.toLowerCase() ===
          filters.compensation.toLowerCase()
    )
}
