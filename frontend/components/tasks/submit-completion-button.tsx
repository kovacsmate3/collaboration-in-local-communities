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
import { useSubmitTaskCompletion } from "@/lib/api/tasks"

interface SubmitCompletionButtonProps {
  taskId: string
}

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
    <AlertDialog>
      <AlertDialogTrigger asChild>
        <Button disabled={isPending}>
          {isPending ? "Submitting…" : "Mark as done"}
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Mark task as done?</AlertDialogTitle>
          <AlertDialogDescription>
            This will notify the seeker that you have finished. They will review
            and approve before the task is marked as complete.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Go back</AlertDialogCancel>
          <AlertDialogAction disabled={isPending} onClick={handleSubmit}>
            {isPending ? "Submitting…" : "Mark as done"}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
