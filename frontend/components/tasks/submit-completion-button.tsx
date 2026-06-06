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
import { useSubmitTaskCompletion } from "@/lib/api/tasks"

interface SubmitCompletionButtonProps {
  taskId: string
}

export function SubmitCompletionButton({
  taskId,
}: SubmitCompletionButtonProps) {
  const t = useTranslations("tasks.completion")
  const tCommon = useTranslations("common")
  const { mutate: submitCompletion, isPending } =
    useSubmitTaskCompletion(taskId)

  function handleSubmit() {
    submitCompletion(undefined, {
      onSuccess: () => toast.success(t("markedDoneToast")),
      onError: () => toast.error(t("markDoneErrorToast")),
    })
  }

  return (
    <AlertDialog>
      <AlertDialogTrigger asChild>
        <Button disabled={isPending}>
          {isPending ? t("markingDone") : t("markAsDone")}
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{t("submitDialogTitle")}</AlertDialogTitle>
          <AlertDialogDescription>
            {t("submitDialogBody")}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>{tCommon("goBack")}</AlertDialogCancel>
          <AlertDialogAction disabled={isPending} onClick={handleSubmit}>
            {isPending ? t("markingDone") : t("markAsDone")}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
