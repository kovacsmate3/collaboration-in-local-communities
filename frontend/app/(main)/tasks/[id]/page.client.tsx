"use client"

import * as React from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import { notFound } from "next/navigation"
import { HugeiconsIcon } from "@hugeicons/react"
import {
  Calendar03Icon,
  Location01Icon,
  StarIcon,
} from "@hugeicons/core-free-icons"
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
import { normalizeTaskStatus } from "@/lib/task-status"
import { useAuth } from "@/lib/auth-context"
import {
  useApplyToTask,
  useApproveTaskCompletion,
  useMyTaskApplications,
  usePatchTaskApplication,
  useSubmitTaskCompletion,
  useTask,
  useTaskApplications,
  useUpdateTask,
  useWithdrawTaskApplication,
} from "@/lib/api/tasks"
import type { ApiTask, ApiTaskApplication } from "@/lib/api/tasks"
import { useConversations, useStartConversation } from "@/lib/api/conversations"
import { useSubmitReview } from "@/lib/api/reviews"
import { ApiError } from "@/lib/api/client"

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
  const status = normalizeTaskStatus(task.status)
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
        {task.acceptedHelperProfileId === user?.profileId ? (
          <SubmitCompletionButton taskId={task.id} />
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

  if (status === "pending_approval") {
    const canOpenChat =
      isSeeker || task.acceptedHelperProfileId === user?.profileId
    return (
      <div className="flex flex-col gap-3">
        <p className="text-sm text-muted-foreground">
          {isSeeker
            ? `${task.acceptedHelperDisplayName ?? "The helper"} marked this task as done. Approve to complete it.`
            : "Waiting for the seeker to approve completion."}
        </p>
        <div className="flex flex-wrap gap-2">
          {isSeeker ? <ApproveCompletionButton taskId={task.id} /> : null}
          {canOpenChat ? (
            <Button
              variant="outline"
              disabled={isStarting || isLoadingConversations}
              onClick={handleMessage}
            >
              {isStarting ? "Opening…" : "Open chat"}
            </Button>
          ) : null}
        </div>
      </div>
    )
  }

  if (status === "completed") {
    const isHelper = task.acceptedHelperProfileId === user?.profileId
    const revieweeProfileId = isSeeker
      ? (task.acceptedHelperProfileId ?? null)
      : isHelper
        ? task.seekerProfileId
        : null
    const revieweeName = isSeeker
      ? (task.acceptedHelperDisplayName ?? "the helper")
      : isHelper
        ? task.seekerDisplayName
        : null

    if (!revieweeProfileId || !revieweeName) {
      return null
    }

    return (
      <ReviewDialog
        taskId={task.id}
        revieweeProfileId={revieweeProfileId}
        revieweeName={revieweeName}
      />
    )
  }

  return null
}

function ReviewDialog({
  taskId,
  revieweeProfileId,
  revieweeName,
}: {
  taskId: string
  revieweeProfileId: string
  revieweeName: string
}) {
  const [open, setOpen] = React.useState(false)
  const [rating, setRating] = React.useState(0)
  const [hovered, setHovered] = React.useState(0)
  const [comment, setComment] = React.useState("")
  const [submitted, setSubmitted] = React.useState(false)
  const [alreadyReviewed, setAlreadyReviewed] = React.useState(false)
  const { mutate: submitReview, isPending } = useSubmitReview(
    taskId,
    revieweeProfileId
  )

  if (submitted || alreadyReviewed) {
    return (
      <p className="text-sm text-muted-foreground">
        {alreadyReviewed
          ? "You've already reviewed this task."
          : "Review submitted — thank you!"}
      </p>
    )
  }

  function handleSubmit(event: React.SyntheticEvent<HTMLFormElement>) {
    event.preventDefault()
    if (rating === 0) {
      toast.error("Please select a star rating.")
      return
    }

    submitReview(
      { rating, comment: comment.trim() || undefined },
      {
        onSuccess: () => {
          setOpen(false)
          setSubmitted(true)
          toast.success("Review submitted!")
        },
        onError: (err: unknown) => {
          // 409 from the backend means the same reviewer already posted
          // a review for this task. Surface a clear, non-generic message
          // and collapse the submit path (issue #59 AC).
          if (err instanceof ApiError && err.status === 409) {
            setOpen(false)
            setAlreadyReviewed(true)
            toast.error("You've already reviewed this task.")
            return
          }
          toast.error("Could not submit the review. Please try again.")
        },
      }
    )
  }

  const displayRating = hovered || rating

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button>Leave a review</Button>
      </DialogTrigger>
      <DialogContent className="max-w-md">
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>Leave a review</DialogTitle>
            <DialogDescription>
              How was your experience working with {revieweeName}?
            </DialogDescription>
          </DialogHeader>

          <div className="flex flex-col gap-4 py-4">
            {/* Star rating picker */}
            <div className="flex gap-1" role="radiogroup" aria-label="Rating">
              {[1, 2, 3, 4, 5].map((star) => (
                <button
                  key={star}
                  type="button"
                  role="radio"
                  aria-checked={rating === star}
                  aria-label={`${star} star${star !== 1 ? "s" : ""}`}
                  className="rounded p-0.5 focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
                  onMouseEnter={() => setHovered(star)}
                  onMouseLeave={() => setHovered(0)}
                  onClick={() => setRating(star)}
                >
                  <HugeiconsIcon
                    icon={StarIcon}
                    className={
                      star <= displayRating
                        ? "size-7 text-amber-400"
                        : "size-7 text-muted-foreground/40"
                    }
                  />
                </button>
              ))}
            </div>

            <Textarea
              value={comment}
              onChange={(e) => setComment(e.target.value)}
              maxLength={2000}
              placeholder="Share what went well or what could be improved…"
              rows={4}
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
            <Button type="submit" disabled={isPending || rating === 0}>
              {isPending ? "Submitting…" : "Submit review"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

function SubmitCompletionButton({ taskId }: { taskId: string }) {
  const { mutate: submitCompletion, isPending } =
    useSubmitTaskCompletion(taskId)

  function handleSubmit() {
    submitCompletion(undefined, {
      onSuccess: () => toast.success("Marked as done. Awaiting approval."),
      onError: () => toast.error("Could not mark this task as done."),
    })
  }

  return (
    <Button disabled={isPending} onClick={handleSubmit}>
      {isPending ? "Submitting…" : "Mark as done"}
    </Button>
  )
}

function ApproveCompletionButton({ taskId }: { taskId: string }) {
  const { mutate: approveCompletion, isPending } =
    useApproveTaskCompletion(taskId)

  function handleApprove() {
    approveCompletion(undefined, {
      onSuccess: () => toast.success("Task completed."),
      onError: () => toast.error("Could not approve completion."),
    })
  }

  return (
    <Button disabled={isPending} onClick={handleApprove}>
      {isPending ? "Approving…" : "Approve completion"}
    </Button>
  )
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
  const router = useRouter()
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
        onSuccess: (app) => {
          setMessage("")
          setOpen(false)
          if (app.conversationId) {
            router.push(`/messages/${app.conversationId}`)
          } else {
            toast.success("Application sent.")
          }
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
        {application.conversationId ? (
          <Button variant="outline" asChild>
            <Link href={`/messages/${application.conversationId}`}>
              View conversation
            </Link>
          </Button>
        ) : null}
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
      <div className="flex flex-wrap items-center gap-2">
        <Badge variant="outline" className="h-9 rounded-md px-3">
          Application {application.status.toLowerCase()}
        </Badge>
        {application.conversationId ? (
          <Button variant="outline" size="sm" asChild>
            <Link href={`/messages/${application.conversationId}`}>
              View conversation
            </Link>
          </Button>
        ) : null}
      </div>
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
              <div className="flex flex-wrap gap-2">
                {application.conversationId ? (
                  <Button size="sm" variant="outline" asChild>
                    <Link href={`/messages/${application.conversationId}`}>
                      View chat
                    </Link>
                  </Button>
                ) : null}
                {application.status === "Pending" ? (
                  <>
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
                  </>
                ) : null}
              </div>
            </div>
          ))}
        </div>
      )}
    </section>
  )
}
