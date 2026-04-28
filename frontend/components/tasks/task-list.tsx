import { InboxIcon } from "@hugeicons/core-free-icons"

import { TaskCard } from "@/components/tasks/task-card"
import { EmptyState } from "@/components/shared/empty-state"
import type { Task } from "@/lib/types"

interface TaskListProps {
  tasks: Task[]
  emptyTitle?: string
  emptyDescription?: string
  /** Hide the status badge on cards (useful on the public feeds). */
  hideStatus?: boolean
}

/**
 * Renders a vertical list of TaskCards or an empty state.
 *
 * Stays presentation-only - data fetching/filtering happens at the page
 * level so this component is trivial to reuse.
 */
export function TaskList({
  tasks,
  emptyTitle = "No tasks yet",
  emptyDescription,
  hideStatus,
}: TaskListProps) {
  if (tasks.length === 0) {
    return (
      <EmptyState
        icon={InboxIcon}
        title={emptyTitle}
        description={emptyDescription}
      />
    )
  }

  return (
    <ul className="flex flex-col gap-3">
      {tasks.map((task) => (
        <li key={task.id}>
          <TaskCard task={task} hideStatus={hideStatus} />
        </li>
      ))}
    </ul>
  )
}
