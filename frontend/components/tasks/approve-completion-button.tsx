"use client"

import { useTranslations } from "next-intl"
import { toast } from "sonner"

import { Button } from "@/components/ui/button"
import { useApproveTaskCompletion } from "@/lib/api/tasks"

interface ApproveCompletionButtonProps {
  taskId: string
}

/**
 * "Approve completion" button shown to the seeker while the task is
 * PendingApproval. Approving transitions the task to Completed.
 */
export function ApproveCompletionButton({
  taskId,
}: ApproveCompletionButtonProps) {
  const t = useTranslations("tasks.completion")
  const { mutate: approveCompletion, isPending } =
    useApproveTaskCompletion(taskId)

  function handleApprove() {
    approveCompletion(undefined, {
      onSuccess: () => toast.success(t("approvedToast")),
      onError: () => toast.error(t("approveErrorToast")),
    })
  }

  return (
    <Button disabled={isPending} onClick={handleApprove}>
      {isPending ? t("approving") : t("approveCompletion")}
    </Button>
  )
}
