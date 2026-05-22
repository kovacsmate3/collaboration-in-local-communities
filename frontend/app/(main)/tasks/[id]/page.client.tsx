"use client"

import * as React from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import { notFound } from "next/navigation"
import { HugeiconsIcon } from "@hugeicons/react"
import { Calendar03Icon, Location01Icon } from "@hugeicons/core-free-icons"
import { toast } from "sonner"

import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import { Separator } from "@/components/ui/separator"
import { Textarea } from "@/components/ui/textarea"
import { CategoryBadge } from "@/components/tasks/category-badge"
import { CompensationBadge } from "@/components/tasks/compensation-badge"
import { TaskStatusBadge } from "@/components/tasks/task-status-badge"
import { UserAvatar } from "@/components/shared/user-avatar"
import { RichTextContent } from "@/components/shared/rich-text-content"
import { formatRelativeTime } from "@/lib/format"
import { useAuth } from "@/lib/auth-context"
import {
  useApplyToTask,
  useMyTaskApplications,
  usePatchTaskApplication,
  useTask,
  useTaskApplications,
  useUpdateTask,
  useWithdrawTaskApplication,
} from "@/lib/api/tasks"
import type { ApiTask, ApiTaskApplication } from "@/lib/api/tasks"
import { useConversations, useStartConversation } from "@/lib/api/conversations"

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
  const { data: myApplications = [], isLoading: isLoadingMyApplications } =
    useMyTaskApplications()
  const { data: applications = [], isLoading: isLoadingApplications } =
    useTaskApplications(task.id, task.seekerProfileId === user?.profileId)
  const { mutate: startConversation, isPending: isStarting } =
    useStartConversation()
  const isSeeker = task.seekerProfileId === user?.profileId
  const status = task.status.toLowerCase().replace(/([a-z])([A-Z])/g, "$1_$2")
  const canOpenInProgressChat =
    status === "in_progress" &&
    (isSeeker || task.acceptedHelperProfileId === user?.profileId)
  const { data: conversations = [], isLoading: isLoadingConversations } =
    useConversations(canOpenInProgressChat)
  const currentApplication = myApplications.find((a) => a.taskId === task.id)

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
    const existing = conversations.find((c) => c.taskId === task.id)
    if (existing) {
      router.push(`/messages/${existing.id}`)
      return
    }

    if (isSeeker) {
      router.push("/messages")
      return
    }

    startConversation(task.id, {
      onSuccess: (c) => router.push(`/messages/${c.id}`),
      onError: () => toast.error("Could not open chat."),
    })
  }

  if (status === "open") {
    return (
      <div className="flex flex-col gap-4">
        <div className="flex flex-wrap gap-2">
          {!isSeeker ? (
            <ApplicationControls
              taskId={task.id}
              application={currentApplication}
              isLoadingApplication={isLoadingMyApplications}
            />
          ) : (
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
          )}
        </div>
        {isSeeker ? (
          <TaskApplicationsPanel
            applications={applications}
            isLoading={isLoadingApplications}
            taskId={task.id}
          />
        ) : null}
      </div>
    )
  }

  if (status === "in_progress") {
    return (
      <div className="flex flex-wrap gap-2">
        {canOpenInProgressChat ? (
          <Button
            disabled={isStarting || isLoadingConversations}
            onClick={handleMessage}
          >
            {isStarting ? "Opening…" : "Open chat"}
          </Button>
        ) : null}
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

function ApplicationControls({
  taskId,
  application,
  isLoadingApplication = false,
}: {
  taskId: string
  application?: ApiTaskApplication
  isLoadingApplication?: boolean
}) {
  const [open, setOpen] = React.useState(false)
  const [message, setMessage] = React.useState("")
  const { mutate: applyToTask, isPending: isApplying } = useApplyToTask(taskId)
  const { mutate: withdraw, isPending: isWithdrawing } =
    useWithdrawTaskApplication(taskId)

  function handleApply(event: React.SyntheticEvent<HTMLFormElement>) {
    event.preventDefault()

    applyToTask(
      { message: message.trim() || undefined },
      {
        onSuccess: () => {
          toast.success("Application sent.")
          setMessage("")
          setOpen(false)
        },
        onError: () => toast.error("Could not apply to this task."),
      }
    )
  }

  function handleWithdraw() {
    if (!application) return

    withdraw(application.id, {
      onSuccess: () => toast.success("Application withdrawn."),
      onError: () => toast.error("Could not withdraw the application."),
    })
  }

  if (!application && isLoadingApplication) {
    return <Button disabled>Apply to help</Button>
  }

  if (application?.status === "Pending") {
    return (
      <>
        <Button variant="outline" disabled>
          Application pending
        </Button>
        <Button
          variant="ghost"
          disabled={isWithdrawing}
          onClick={handleWithdraw}
        >
          {isWithdrawing ? "Withdrawing…" : "Withdraw"}
        </Button>
      </>
    )
  }

  if (application) {
    return (
      <Badge variant="outline" className="h-9 rounded-md px-3">
        Application {application.status.toLowerCase()}
      </Badge>
    )
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button>Apply to help</Button>
      </DialogTrigger>
      <DialogContent className="max-w-md">
        <form onSubmit={handleApply}>
          <DialogHeader>
            <DialogTitle>Apply to help</DialogTitle>
            <DialogDescription>
              Send a short note to the seeker with your availability or relevant
              experience.
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <Textarea
              value={message}
              onChange={(event) => setMessage(event.target.value)}
              maxLength={1000}
              placeholder="I can help this afternoon and have a small hand cart."
              rows={5}
            />
          </div>
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => setOpen(false)}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={isApplying}>
              {isApplying ? "Sending…" : "Send application"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

function TaskApplicationsPanel({
  applications,
  isLoading,
  taskId,
}: {
  applications: ApiTaskApplication[]
  isLoading: boolean
  taskId: string
}) {
  const { mutate: patchApplication, isPending } =
    usePatchTaskApplication(taskId)

  function handleAction(applicationId: string, action: "accept" | "reject") {
    patchApplication(
      { applicationId, action },
      {
        onSuccess: () =>
          toast.success(
            action === "accept"
              ? "Application accepted."
              : "Application rejected."
          ),
        onError: () => toast.error("Could not update the application."),
      }
    )
  }

  return (
    <section className="rounded-lg border bg-card p-4">
      <div className="mb-3 flex items-center justify-between gap-3">
        <h2 className="text-sm font-medium">Applications</h2>
        <Badge variant="muted">{isLoading ? "…" : applications.length}</Badge>
      </div>
      {isLoading ? (
        <p className="text-sm text-muted-foreground">Loading applications…</p>
      ) : applications.length === 0 ? (
        <p className="text-sm text-muted-foreground">No applications yet.</p>
      ) : (
        <div className="divide-y">
          {applications.map((application) => (
            <div
              key={application.id}
              className="flex flex-col gap-3 py-3 first:pt-0 last:pb-0"
            >
              <div className="flex flex-wrap items-center gap-2">
                <UserAvatar size="sm" name={application.helperDisplayName} />
                <span className="text-sm font-medium">
                  {application.helperDisplayName}
                </span>
                <Badge variant="outline">{application.status}</Badge>
                <span className="ml-auto text-xs text-muted-foreground">
                  {formatRelativeTime(application.createdAt)}
                </span>
              </div>
              {application.message ? (
                <p className="text-sm text-muted-foreground">
                  {application.message}
                </p>
              ) : null}
              {application.status === "Pending" ? (
                <div className="flex flex-wrap gap-2">
                  <Button
                    size="sm"
                    disabled={isPending}
                    onClick={() => handleAction(application.id, "accept")}
                  >
                    Accept
                  </Button>
                  <Button
                    size="sm"
                    variant="ghost"
                    disabled={isPending}
                    onClick={() => handleAction(application.id, "reject")}
                  >
                    Reject
                  </Button>
                </div>
              ) : null}
            </div>
          ))}
        </div>
      )}
    </section>
  )
}
