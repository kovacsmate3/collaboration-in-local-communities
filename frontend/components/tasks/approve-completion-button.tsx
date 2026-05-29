"use client"

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
