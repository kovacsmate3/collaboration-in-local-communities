"use client"

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
