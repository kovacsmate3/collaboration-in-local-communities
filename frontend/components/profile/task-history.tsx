import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { TaskStatusBadge } from "@/components/tasks/task-status-badge"
import { CategoryBadge } from "@/components/tasks/category-badge"
import { EmptyState } from "@/components/shared/empty-state"
import { formatRelativeTime } from "@/lib/format"
import type { Task } from "@/lib/types"

interface TaskHistoryProps {
  tasks: Task[]
}

/**
 * Compact history list shown on a user's profile.
 *
 * Differs from `TaskList` in that this is read-only context for a
 * profile view, not an actionable feed - so the styling is tighter and
 * cards are not navigable.
 */
export function TaskHistory({ tasks }: TaskHistoryProps) {
  if (tasks.length === 0) {
    return (
      <EmptyState
        title="No task history yet"
        description="Completed and ongoing tasks will appear here."
      />
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-sm tracking-wide text-muted-foreground uppercase">
          Task history
        </CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-2">
        {tasks.map((task) => (
          <div
            key={task.id}
            className="flex items-center justify-between gap-3 border-t border-border pt-2 first:border-t-0 first:pt-0"
          >
            <div className="flex min-w-0 flex-col gap-1">
              <span className="truncate text-sm font-medium">{task.title}</span>
              <div className="flex items-center gap-2">
                <CategoryBadge category={task.category} icon={task.icon} />
                <span className="text-xs text-muted-foreground">
                  {formatRelativeTime(task.createdAt)}
                </span>
              </div>
            </div>
            <TaskStatusBadge status={task.status} />
          </div>
        ))}
      </CardContent>
    </Card>
  )
}
