"use client"

import { useTranslations } from "next-intl"
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
  const t = useTranslations("tasks.completion")
  const tCommon = useTranslations("common")
  const { mutate: approveCompletion, isPending } =
    useApproveTaskCompletion(taskId)

  function handleApprove() {
    approveCompletion(undefined, {
      onSuccess: () => toast.success(t("approvedToast")),
      onError: () => toast.error(t("approveErrorToast")),
    })
  }

  return (
    <AlertDialog>
      <AlertDialogTrigger asChild>
        <Button disabled={isPending}>
          {isPending ? t("approving") : t("approveCompletion")}
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{t("approveDialogTitle")}</AlertDialogTitle>
          <AlertDialogDescription>
            {t("approveDialogBody")}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>{tCommon("goBack")}</AlertDialogCancel>
          <AlertDialogAction disabled={isPending} onClick={handleApprove}>
            {isPending ? t("approving") : t("approveCompletion")}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
