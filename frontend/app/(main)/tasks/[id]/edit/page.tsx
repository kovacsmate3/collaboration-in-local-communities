"use client"

import { notFound, useParams } from "next/navigation"
import { useTranslations } from "next-intl"

import { LoadingState } from "@/components/shared/loading-state"
import { PageHeader } from "@/components/shared/page-header"
import { PostTaskForm } from "@/components/tasks/post-task-form"
import { useAuth } from "@/lib/auth-context"
import { useTask } from "@/lib/api/tasks"
import type { CompensationType } from "@/lib/types"

export default function EditTaskPage() {
  const t = useTranslations("tasks.editPage")
  const { id } = useParams<{ id: string }>()
  const { user, isLoading: isAuthLoading } = useAuth()
  const { data: task, isLoading: isTaskLoading, isError } = useTask(id)

  if (isTaskLoading || isAuthLoading) {
    return <LoadingState rows={5} />
  }

  if (isError || !task) {
    notFound()
  }

  if (task.seekerProfileId !== user?.profileId) {
    notFound()
  }

  if (task.status.toLowerCase() !== "open") {
    notFound()
  }

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-6">
      <PageHeader title={t("title")} description={t("description")} />
      <PostTaskForm
        taskId={id}
        initialValues={{
          title: task.title,
          description: task.description,
          categoryId: task.categoryId,
          location: {
            locationText: task.locationText ?? "",
            latitude: task.latitude ?? undefined,
            longitude: task.longitude ?? undefined,
          },
          compensationType: task.compensationType as CompensationType,
          compensationAmount: task.compensationAmount?.toString() ?? "",
        }}
      />
    </div>
  )
}
