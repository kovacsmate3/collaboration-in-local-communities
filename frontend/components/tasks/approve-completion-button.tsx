"use client"

import { toast } from "sonner"

import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog"
import { Button } from "@/components/ui/button"
import { useApproveTaskCompletion } from "@/lib/api/tasks"

interface ApproveCompletionButtonProps {
  taskId: string
}

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
    <AlertDialog>
      <AlertDialogTrigger asChild>
        <Button disabled={isPending}>
          {isPending ? "Approving…" : "Approve completion"}
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Approve task completion?</AlertDialogTitle>
          <AlertDialogDescription>
            This will mark the task as complete and release the helper&apos;s
            reward. This action cannot be undone.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Go back</AlertDialogCancel>
          <AlertDialogAction disabled={isPending} onClick={handleApprove}>
            {isPending ? "Approving…" : "Approve completion"}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
