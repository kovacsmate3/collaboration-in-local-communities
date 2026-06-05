"use client"

import { InboxIcon } from "@hugeicons/core-free-icons"
import { useTranslations } from "next-intl"

import { TaskCard } from "@/components/tasks/task-card"
import { EmptyState } from "@/components/shared/empty-state"
import type { ApiTask } from "@/lib/api/tasks"
import { cn } from "@/lib/utils"

interface TaskListProps {
  tasks: ApiTask[]
  emptyTitle?: string
  emptyDescription?: string
  hideStatus?: boolean
  /**
   * `stack` (default) renders a single-column flex list, used by the My
   * Tasks tabs. `grid` renders a responsive 1/2/3-column card grid for the
   * feed. Both collapse to a single column at 360px.
   */
  layout?: "stack" | "grid"
}

export function TaskList({
  tasks,
  emptyTitle,
  emptyDescription,
  hideStatus,
  layout = "stack",
}: TaskListProps) {
  const t = useTranslations("tasks.list")
  if (tasks.length === 0) {
    return (
      <EmptyState
        icon={InboxIcon}
        title={emptyTitle ?? t("emptyTitle")}
        description={emptyDescription}
      />
    )
  }

  return (
    <ul
      className={cn(
        layout === "grid"
          ? "grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3"
          : "flex flex-col gap-3"
      )}
    >
      {tasks.map((task) => (
        <li key={task.id} className="min-w-0">
          <TaskCard task={task} hideStatus={hideStatus} />
        </li>
      ))}
    </ul>
  )
}
