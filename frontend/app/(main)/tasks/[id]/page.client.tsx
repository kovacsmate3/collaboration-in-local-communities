"use client"

import Link from "next/link"
import { useRouter } from "next/navigation"
import { notFound } from "next/navigation"
import { HugeiconsIcon } from "@hugeicons/react"
import { Calendar03Icon, Location01Icon } from "@hugeicons/core-free-icons"
import { useLocale, useTranslations } from "next-intl"
import { toast } from "sonner"

import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import { ApplicationControls } from "@/components/tasks/application-controls"
import { ApproveCompletionButton } from "@/components/tasks/approve-completion-button"
import { CancelApplicationButton } from "@/components/tasks/cancel-application-button"
import { CancelTaskButton } from "@/components/tasks/cancel-task-button"
import { CategoryBadge } from "@/components/tasks/category-badge"
import { CompensationBadge } from "@/components/tasks/compensation-badge"
import { ReviewDialog } from "@/components/tasks/review-dialog"
import { SubmitCompletionButton } from "@/components/tasks/submit-completion-button"
import { TaskApplicationsPanel } from "@/components/tasks/task-applications-panel"
import { TaskStatusBadge } from "@/components/tasks/task-status-badge"
import { UserAvatar } from "@/components/shared/user-avatar"
import { LoadingState } from "@/components/shared/loading-state"
import { RichTextContent } from "@/components/shared/rich-text-content"
import { formatRelativeTime } from "@/lib/format"
import { normalizeTaskStatus } from "@/lib/task-status"
import { useAuth } from "@/lib/auth-context"
import {
  useMyTaskApplications,
  useTask,
  useTaskApplications,
  useUpdateTask,
} from "@/lib/api/tasks"
import type { ApiTask } from "@/lib/api/tasks"
import { useConversations, useStartConversation } from "@/lib/api/conversations"

interface TaskDetailPageClientProps {
  id: string
}

export function TaskDetailPageClient({ id }: TaskDetailPageClientProps) {
  const t = useTranslations("tasks.detail")
  const locale = useLocale()
  const { data: task, isLoading, isError } = useTask(id)

  if (isLoading) {
    return <LoadingState rows={5} />
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
            {t("posted", {
              relative: formatRelativeTime(task.createdAt, locale),
            })}
          </li>
        </ul>
      </header>

      <Separator />

      <section className="flex flex-col gap-2">
        <h2 className="text-sm font-medium">{t("description")}</h2>
        <RichTextContent html={task.description} />
      </section>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm tracking-wide text-muted-foreground uppercase">
            {t("postedBy")}
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
  const t = useTranslations("tasks.detail")
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
  const acceptedApplication =
    applications.find((application) => application.status === "Accepted") ??
    (currentApplication?.status === "Accepted" ? currentApplication : undefined)

  function handleCancel() {
    updateTask(
      // cancellationReason is persisted data sent to the backend; intentionally
      // left in English so DB content stays consistent across UI languages.
      { status: "Cancelled", cancellationReason: "Cancelled by seeker" },
      {
        onSuccess: () => {
          toast.success(t("cancelledToast"))
          router.refresh()
        },
        onError: () => toast.error(t("cancelErrorToast")),
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
      onError: () => toast.error(t("openChatErrorToast")),
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
                <Link href={`/tasks/${task.id}/edit`}>{t("editTask")}</Link>
              </Button>
              <CancelTaskButton
                onCancel={handleCancel}
                isPending={isCancelling}
              />
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
            {isStarting ? t("openingChat") : t("openChat")}
          </Button>
        ) : null}
        {task.acceptedHelperProfileId === user?.profileId ? (
          <SubmitCompletionButton taskId={task.id} />
        ) : null}
        {acceptedApplication ? (
          <CancelApplicationButton
            taskId={task.id}
            applicationId={acceptedApplication.id}
          />
        ) : null}
        {isSeeker ? (
          <CancelTaskButton onCancel={handleCancel} isPending={isCancelling} />
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
            ? t("helperMarkedDone", {
                helper: task.acceptedHelperDisplayName ?? t("defaultHelper"),
              })
            : t("waitingApproval")}
        </p>
        <div className="flex flex-wrap gap-2">
          {isSeeker ? <ApproveCompletionButton taskId={task.id} /> : null}
          {canOpenChat ? (
            <Button
              variant="outline"
              disabled={isStarting || isLoadingConversations}
              onClick={handleMessage}
            >
              {isStarting ? t("openingChat") : t("openChat")}
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
      ? (task.acceptedHelperDisplayName ?? t("defaultHelper"))
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
