import { InboxIcon } from "@hugeicons/core-free-icons"

import { TaskCard } from "@/components/tasks/task-card"
import { EmptyState } from "@/components/shared/empty-state"
import type { ApiTask } from "@/lib/api/tasks"

interface TaskListProps {
  tasks: ApiTask[]
  emptyTitle?: string
  emptyDescription?: string
  hideStatus?: boolean
}

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
