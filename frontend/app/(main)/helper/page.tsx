"use client"

import * as React from "react"

import { PageHeader } from "@/components/shared/page-header"
import {
  DEFAULT_FILTERS,
  TaskFilters,
  type TaskFiltersState,
} from "@/components/tasks/task-filters"
import { TaskList } from "@/components/tasks/task-list"
import { mockTasks } from "@/lib/mock-data"
import type { Task } from "@/lib/types"

/**
 * Helper Feed (US-01, US-02, US-04, US-09).
 *
 * Dedicated surface for helpers: open tasks only, surfaced through a
 * filtered search. Skill-matching ordering is mocked as a no-op for
 * now; replace `applyFilters` with a server-side query later.
 */
export default function HelperPage() {
  const [filters, setFilters] = React.useState<TaskFiltersState>(DEFAULT_FILTERS)

  const tasks = React.useMemo(
    () => applyFilters(mockTasks, filters),
    [filters]
  )

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Find someone to help"
        description="Open requests filtered by your skills, location, and availability."
      />
      <TaskFilters value={filters} onChange={setFilters} />
      <TaskList
        tasks={tasks}
        emptyTitle="No tasks match your filters"
        emptyDescription="Try widening the category or recency range."
        hideStatus
      />
    </div>
  )
}

function applyFilters(tasks: Task[], filters: TaskFiltersState): Task[] {
  return tasks
    .filter((t) => t.status === "open")
    .filter((t) => {
      if (!filters.query) return true
      const q = filters.query.toLowerCase()
      return (
        t.title.toLowerCase().includes(q) ||
        t.description.toLowerCase().includes(q) ||
        t.location.toLowerCase().includes(q)
      )
    })
    .filter((t) =>
      filters.category === "all" ? true : t.category === filters.category
    )
    .filter((t) =>
      filters.compensation === "all"
        ? true
        : t.compensation.type === filters.compensation
    )
}
