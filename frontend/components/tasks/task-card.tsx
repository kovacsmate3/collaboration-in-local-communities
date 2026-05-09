import Link from "next/link"
import { HugeiconsIcon } from "@hugeicons/react"
import { Location01Icon } from "@hugeicons/core-free-icons"

import { Card, CardContent, CardFooter, CardHeader } from "@/components/ui/card"
import { CategoryBadge } from "@/components/tasks/category-badge"
import { CompensationBadge } from "@/components/tasks/compensation-badge"
import { TaskStatusBadge } from "@/components/tasks/task-status-badge"
import { UserAvatar } from "@/components/shared/user-avatar"
import { formatRelativeTime } from "@/lib/format"
import type { ApiTask } from "@/lib/api/tasks"
import { cn } from "@/lib/utils"

interface TaskCardProps {
  task: ApiTask
  hideStatus?: boolean
  className?: string
}

export function TaskCard({ task, hideStatus, className }: TaskCardProps) {
  return (
    <Card className={cn("transition-colors hover:border-ring/40", className)}>
      <Link
        href={`/tasks/${task.id}`}
        className="flex flex-col gap-4 rounded-xl outline-none focus-visible:ring-3 focus-visible:ring-ring/40"
      >
        <CardHeader>
          <div className="flex flex-wrap items-center gap-2">
            <CategoryBadge label={task.categoryName} icon={task.categoryIcon} />
            <CompensationBadge
              compensationType={task.compensationType}
              compensationAmount={task.compensationAmount}
            />
            {!hideStatus ? <TaskStatusBadge status={task.status} /> : null}
            <span className="ml-auto text-xs text-muted-foreground">
              {formatRelativeTime(task.createdAt)}
            </span>
          </div>
          <h3 className="text-base leading-snug font-semibold">{task.title}</h3>
        </CardHeader>

        <CardContent className="flex flex-col gap-3">
          <p className="line-clamp-2 text-sm text-muted-foreground">
            {task.description.replace(/<[^>]*>/g, " ").replace(/\s+/g, " ").trim()}
          </p>
          {task.locationText ? (
            <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
              <HugeiconsIcon icon={Location01Icon} className="size-3.5" />
              <span>{task.locationText}</span>
            </div>
          ) : null}
        </CardContent>

        <CardFooter>
          <div className="flex items-center gap-2">
            <UserAvatar size="sm" name={task.seekerDisplayName} />
            <span className="text-xs font-medium">{task.seekerDisplayName}</span>
          </div>
        </CardFooter>
      </Link>
    </Card>
  )
}
