import { notFound } from "next/navigation"
import Link from "next/link"
import { HugeiconsIcon } from "@hugeicons/react"
import {
  Calendar03Icon,
  Location01Icon,
} from "@hugeicons/core-free-icons"

import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import { CategoryBadge } from "@/components/tasks/category-badge"
import { CompensationBadge } from "@/components/tasks/compensation-badge"
import { TaskStatusBadge } from "@/components/tasks/task-status-badge"
import { RatingStars } from "@/components/shared/rating-stars"
import { UserAvatar } from "@/components/shared/user-avatar"
import { formatRelativeTime } from "@/lib/format"
import { getTaskById } from "@/lib/mock-data"

interface TaskDetailPageProps {
  // Next.js 15+ async params - awaited before use.
  params: Promise<{ id: string }>
}

/**
 * Task detail (Modules 4/5/7).
 *
 * Shows full task info plus status-aware actions:
 *   - Open: "Express interest" (helper) or "Withdraw" (seeker)
 *   - In progress: "Open chat" + "Mark complete"
 *   - Completed: "Leave review"
 *
 * Action wiring is deferred until the API is in place.
 */
export default async function TaskDetailPage({ params }: TaskDetailPageProps) {
  const { id } = await params
  const task = getTaskById(id)
  if (!task) notFound()

  return (
    <article className="mx-auto flex max-w-3xl flex-col gap-6">
      <div className="flex flex-wrap items-center gap-2">
        <CategoryBadge category={task.category} />
        <CompensationBadge compensation={task.compensation} />
        <TaskStatusBadge status={task.status} />
      </div>

      <header className="flex flex-col gap-2">
        <h1 className="text-2xl font-semibold tracking-tight">{task.title}</h1>
        <ul className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-muted-foreground">
          <li className="flex items-center gap-1.5">
            <HugeiconsIcon icon={Location01Icon} className="size-4" />
            {task.location}
          </li>
          <li className="flex items-center gap-1.5">
            <HugeiconsIcon icon={Calendar03Icon} className="size-4" />
            Posted {formatRelativeTime(task.createdAt)}
          </li>
        </ul>
      </header>

      <Separator />

      <section className="flex flex-col gap-2">
        <h2 className="text-sm font-medium">Description</h2>
        <p className="whitespace-pre-line text-sm leading-relaxed text-muted-foreground">
          {task.description}
        </p>
      </section>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm uppercase tracking-wide text-muted-foreground">
            Posted by
          </CardTitle>
        </CardHeader>
        <CardContent className="flex items-center justify-between gap-3">
          <Link
            href={`/profile/${task.seeker.id}`}
            className="flex min-w-0 items-center gap-3"
          >
            <UserAvatar
              size="md"
              name={task.seeker.name}
              src={task.seeker.avatarUrl}
            />
            <div className="flex flex-col">
              <span className="text-sm font-medium">{task.seeker.name}</span>
              <RatingStars
                value={task.seeker.reputation.averageRating}
                reviewCount={task.seeker.reputation.reviewCount}
              />
            </div>
          </Link>
        </CardContent>
      </Card>

      <TaskActions task={task} />
    </article>
  )
}

/**
 * Status-driven primary actions. Kept inline for clarity - extract once
 * we add real handlers and per-action permission rules.
 */
function TaskActions({ task }: { task: ReturnType<typeof getTaskById> }) {
  if (!task) return null

  if (task.status === "open") {
    return (
      <div className="flex flex-wrap gap-2">
        <Button>Express interest</Button>
        <Button variant="outline" asChild>
          <Link href="/messages">Message seeker</Link>
        </Button>
      </div>
    )
  }

  if (task.status === "in_progress") {
    return (
      <div className="flex flex-wrap gap-2">
        <Button asChild>
          <Link href="/messages">Open chat</Link>
        </Button>
        <Button variant="outline">Mark complete</Button>
        <Button variant="ghost">Cancel task</Button>
      </div>
    )
  }

  if (task.status === "completed") {
    return (
      <div className="flex flex-wrap gap-2">
        <Button>Leave a review</Button>
      </div>
    )
  }

  return null
}
