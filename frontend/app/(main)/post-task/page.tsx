import type { Metadata } from "next"
import { getTranslations } from "next-intl/server"

import { PageHeader } from "@/components/shared/page-header"
import { PostTaskForm } from "@/components/tasks/post-task-form"

export const metadata: Metadata = {
  title: "Post a task",
}

/**
 * Task creation page (US-01, US-04, US-07, US-12).
 *
 * Single-step form for the skeleton; consider splitting into a stepper
 * once we add image uploads, location autocomplete, and skill matching.
 */
export default async function PostTaskPage() {
  const t = await getTranslations("tasks.postPage")
  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-6">
      <PageHeader title={t("title")} description={t("description")} />
      <PostTaskForm />
    </div>
  )
}
