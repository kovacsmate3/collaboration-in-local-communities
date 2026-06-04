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
import { useWithdrawTaskApplication } from "@/lib/api/tasks"

interface CancelApplicationButtonProps {
  taskId: string
  applicationId: string
  variant?: "default" | "outline" | "ghost"
}

export function CancelApplicationButton({
  taskId,
  applicationId,
  variant = "outline",
}: CancelApplicationButtonProps) {
  const { mutate: cancelApplication, isPending } =
    useWithdrawTaskApplication(taskId)

  function handleCancel() {
    cancelApplication(applicationId, {
      onSuccess: () => toast.success("Application cancelled."),
      onError: () => toast.error("Could not cancel the application."),
    })
  }

  return (
    <AlertDialog>
      <AlertDialogTrigger asChild>
        <Button variant={variant} disabled={isPending}>
          {isPending ? "Cancelling…" : "Cancel application"}
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Cancel this application?</AlertDialogTitle>
          <AlertDialogDescription>
            This will remove the accepted helper and return the task to Open.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Go back</AlertDialogCancel>
          <AlertDialogAction disabled={isPending} onClick={handleCancel}>
            {isPending ? "Cancelling…" : "Cancel application"}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
