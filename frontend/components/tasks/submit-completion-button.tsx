"use client"

import { useTranslations } from "next-intl"
import { toast } from "sonner"

import { Button } from "@/components/ui/button"
import { useSubmitTaskCompletion } from "@/lib/api/tasks"

interface SubmitCompletionButtonProps {
  taskId: string
}

/**
 * "Mark as done" button shown to the accepted helper while the task is
 * InProgress. Submitting transitions the task to PendingApproval.
 */
export function SubmitCompletionButton({
  taskId,
}: SubmitCompletionButtonProps) {
  const t = useTranslations("tasks.completion")
  const { mutate: submitCompletion, isPending } =
    useSubmitTaskCompletion(taskId)

  function handleSubmit() {
    submitCompletion(undefined, {
      onSuccess: () => toast.success(t("markedDoneToast")),
      onError: () => toast.error(t("markDoneErrorToast")),
    })
  }

  return (
    <Button disabled={isPending} onClick={handleSubmit}>
      {isPending ? t("markingDone") : t("markAsDone")}
    </Button>
  )
}
