"use client"

import Link from "next/link"
import { useRouter } from "next/navigation"
import { notFound } from "next/navigation"
import { HugeiconsIcon } from "@hugeicons/react"
import { Calendar03Icon, Location01Icon } from "@hugeicons/core-free-icons"
import { toast } from "sonner"

import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import { CategoryBadge } from "@/components/tasks/category-badge"
import { CompensationBadge } from "@/components/tasks/compensation-badge"
import { TaskStatusBadge } from "@/components/tasks/task-status-badge"
import { UserAvatar } from "@/components/shared/user-avatar"
import { RichTextContent } from "@/components/shared/rich-text-content"
import { formatRelativeTime } from "@/lib/format"
import { useAuth } from "@/lib/auth-context"
import { useTask, useUpdateTask } from "@/lib/api/tasks"
import type { ApiTask } from "@/lib/api/tasks"
import { useStartConversation } from "@/lib/api/conversations"

interface TaskDetailPageClientProps {
  id: string
}

export function TaskDetailPageClient({ id }: TaskDetailPageClientProps) {
  const { data: task, isLoading, isError } = useTask(id)

  if (isLoading) {
    return <p className="text-sm text-muted-foreground">Loading task…</p>
  }

  if (isError || !task) {
    notFound()
  }

  return (
    <article className="mx-auto flex max-w-3xl flex-col gap-6">
      <div className="flex flex-wrap items-center gap-2">
        <CategoryBadge label={task.categoryName} icon={task.categoryIcon} />
        <CompensationBadge
          compensationType={task.compensationType}
          compensationAmount={task.compensationAmount}
        />
        <TaskStatusBadge status={task.status} />
      </div>

      <header className="flex flex-col gap-2">
        <h1 className="text-2xl font-semibold tracking-tight">{task.title}</h1>
        <ul className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-muted-foreground">
          {task.locationText ? (
            <li className="flex items-center gap-1.5">
              <HugeiconsIcon icon={Location01Icon} className="size-4" />
              {task.locationText}
            </li>
          ) : null}
          <li className="flex items-center gap-1.5">
            <HugeiconsIcon icon={Calendar03Icon} className="size-4" />
            Posted {formatRelativeTime(task.createdAt)}
          </li>
        </ul>
      </header>

      <Separator />

      <section className="flex flex-col gap-2">
        <h2 className="text-sm font-medium">Description</h2>
        <RichTextContent html={task.description} />
      </section>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm tracking-wide text-muted-foreground uppercase">
            Posted by
          </CardTitle>
        </CardHeader>
        <CardContent className="flex items-center gap-3">
          <UserAvatar size="md" name={task.seekerDisplayName} />
          <span className="text-sm font-medium">{task.seekerDisplayName}</span>
        </CardContent>
      </Card>

      <TaskActions task={task} />
    </article>
  )
}

function TaskActions({ task }: { task: ApiTask }) {
  const { user } = useAuth()
  const router = useRouter()
  const { mutate: updateTask, isPending: isCancelling } = useUpdateTask(task.id)
  const { mutate: startConversation, isPending: isStarting } =
    useStartConversation()
  const isSeeker = task.seekerProfileId === user?.profileId
  const status = task.status.toLowerCase().replace(/([a-z])([A-Z])/g, "$1_$2")

  function handleCancel() {
    updateTask(
      { status: "Cancelled", cancellationReason: "Cancelled by seeker" },
      {
        onSuccess: () => {
          toast.success("Task cancelled.")
          router.refresh()
        },
        onError: () => toast.error("Could not cancel the task."),
      }
    )
  }

  function handleMessage() {
    startConversation(task.id, {
      onSuccess: (c) => router.push(`/messages/${c.id}`),
      onError: () => toast.error("Could not open chat."),
    })
  }

  if (status === "open") {
    return (
      <div className="flex flex-wrap gap-2">
        {!isSeeker ? (
          <Button
            variant="outline"
            disabled={isStarting}
            onClick={handleMessage}
          >
            {isStarting ? "Opening…" : "Message seeker"}
          </Button>
        ) : null}
        {isSeeker ? (
          <>
            <Button variant="outline" asChild>
              <Link href={`/tasks/${task.id}/edit`}>Edit task</Link>
            </Button>
            <Button
              variant="ghost"
              disabled={isCancelling}
              onClick={handleCancel}
            >
              {isCancelling ? "Cancelling…" : "Cancel task"}
            </Button>
          </>
        ) : null}
      </div>
    )
  }

  if (status === "in_progress") {
    return (
      <div className="flex flex-wrap gap-2">
        <Button disabled={isStarting} onClick={handleMessage}>
          {isStarting ? "Opening…" : "Open chat"}
        </Button>
        {isSeeker ? (
          <Button
            variant="ghost"
            disabled={isCancelling}
            onClick={handleCancel}
          >
            {isCancelling ? "Cancelling…" : "Cancel task"}
          </Button>
        ) : null}
      </div>
    )
  }

  if (status === "completed") {
    return (
      <div className="flex flex-wrap gap-2">
        <Button>Leave a review</Button>
      </div>
    )
  }

  return null
}
