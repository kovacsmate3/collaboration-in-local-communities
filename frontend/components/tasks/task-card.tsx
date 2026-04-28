import Link from "next/link"
import { HugeiconsIcon } from "@hugeicons/react"
import { Location01Icon } from "@hugeicons/core-free-icons"

import { Card, CardContent, CardFooter, CardHeader } from "@/components/ui/card"
import { CategoryBadge } from "@/components/tasks/category-badge"
import { CompensationBadge } from "@/components/tasks/compensation-badge"
import { TaskStatusBadge } from "@/components/tasks/task-status-badge"
import { RatingStars } from "@/components/shared/rating-stars"
import { UserAvatar } from "@/components/shared/user-avatar"
import { formatRelativeTime } from "@/lib/format"
import { cn } from "@/lib/utils"
import type { Task } from "@/lib/types"

interface TaskCardProps {
  task: Task
  /** When true, hides the status badge (use it on feeds where status is implied). */
  hideStatus?: boolean
  className?: string
}

/**
 * Single task entry. Linked card pattern: the entire card is a link to the
 * detail page; nested actionable elements should `stopPropagation` if needed.
 */
export function TaskCard({ task, hideStatus, className }: TaskCardProps) {
  return (
    <Card className={cn("transition-colors hover:border-ring/40", className)}>
      <Link
        href={`/tasks/${task.id}`}
        className="flex flex-col gap-4 outline-none focus-visible:ring-3 focus-visible:ring-ring/40 rounded-xl"
      >
        <CardHeader>
          <div className="flex flex-wrap items-center gap-2">
            <CategoryBadge category={task.category} />
            <CompensationBadge compensation={task.compensation} />
            {!hideStatus ? <TaskStatusBadge status={task.status} /> : null}
            <span className="ml-auto text-xs text-muted-foreground">
              {formatRelativeTime(task.createdAt)}
            </span>
          </div>
          <h3 className="text-base font-semibold leading-snug">{task.title}</h3>
        </CardHeader>

        <CardContent className="flex flex-col gap-3">
          <p className="line-clamp-2 text-sm text-muted-foreground">
            {task.description}
          </p>
          <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
            <HugeiconsIcon icon={Location01Icon} className="size-3.5" />
            <span>{task.location}</span>
          </div>
        </CardContent>

        <CardFooter className="justify-between">
          <div className="flex items-center gap-2">
            <UserAvatar
              size="sm"
              name={task.seeker.name}
              src={task.seeker.avatarUrl}
            />
            <div className="flex flex-col">
              <span className="text-xs font-medium">{task.seeker.name}</span>
              <RatingStars
                value={task.seeker.reputation.averageRating}
                reviewCount={task.seeker.reputation.reviewCount}
              />
            </div>
          </div>
        </CardFooter>
      </Link>
    </Card>
  )
}
