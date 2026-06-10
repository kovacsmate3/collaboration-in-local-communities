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
  const t = useTranslations("tasks.cancelApplication")
  const tCommon = useTranslations("common")
  const { mutate: cancelApplication, isPending } =
    useWithdrawTaskApplication(taskId)

  function handleCancel() {
    cancelApplication(applicationId, {
      onSuccess: () => toast.success(t("cancelledToast")),
      onError: () => toast.error(t("cancelErrorToast")),
    })
  }

  return (
    <AlertDialog>
      <AlertDialogTrigger asChild>
        <Button variant={variant} disabled={isPending}>
          {isPending ? t("pending") : t("button")}
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{t("dialogTitle")}</AlertDialogTitle>
          <AlertDialogDescription>{t("dialogBody")}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>{tCommon("goBack")}</AlertDialogCancel>
          <AlertDialogAction disabled={isPending} onClick={handleCancel}>
            {isPending ? t("pending") : t("button")}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
