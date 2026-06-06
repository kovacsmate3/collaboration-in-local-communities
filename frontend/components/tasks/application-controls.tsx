"use client"

import * as React from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import { useTranslations } from "next-intl"
import { toast } from "sonner"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import { Textarea } from "@/components/ui/textarea"
import {
  type ApiTaskApplication,
  useApplyToTask,
  useWithdrawTaskApplication,
} from "@/lib/api/tasks"

interface ApplicationControlsProps {
  taskId: string
  application?: ApiTaskApplication
  isLoadingApplication?: boolean
}

/**
 * Helper-side controls for a task: apply, view pending application, or
 * withdraw. Renders different affordances based on the helper's current
 * application status.
 */
export function ApplicationControls({
  taskId,
  application,
  isLoadingApplication = false,
}: ApplicationControlsProps) {
  const t = useTranslations("tasks.application")
  const tStatuses = useTranslations("tasks.applications.statuses")
  const router = useRouter()
  const [open, setOpen] = React.useState(false)
  const [message, setMessage] = React.useState("")
  const { mutate: applyToTask, isPending: isApplying } = useApplyToTask(taskId)
  const { mutate: withdraw, isPending: isWithdrawing } =
    useWithdrawTaskApplication(taskId)

  function handleApply(event: React.SyntheticEvent<HTMLFormElement>) {
    event.preventDefault()

    applyToTask(
      { message: message.trim() || undefined },
      {
        onSuccess: (app) => {
          setMessage("")
          setOpen(false)
          if (app.conversationId) {
            router.push(`/messages/${app.conversationId}`)
          } else {
            toast.success(t("sentToast"))
          }
        },
        onError: () => toast.error(t("applyErrorToast")),
      }
    )
  }

  function handleWithdraw() {
    if (!application) return

    withdraw(application.id, {
      onSuccess: () => toast.success(t("withdrewToast")),
      onError: () => toast.error(t("withdrawErrorToast")),
    })
  }

  if (!application && isLoadingApplication) {
    return <Button disabled>{t("applyToHelp")}</Button>
  }

  if (application?.status === "Pending") {
    return (
      <>
        <Button variant="outline" disabled>
          {t("applicationPending")}
        </Button>
        {application.conversationId ? (
          <Button variant="outline" asChild>
            <Link href={`/messages/${application.conversationId}`}>
              {t("viewConversation")}
            </Link>
          </Button>
        ) : null}
        <Button
          variant="ghost"
          disabled={isWithdrawing}
          onClick={handleWithdraw}
        >
          {isWithdrawing ? t("withdrawing") : t("withdraw")}
        </Button>
      </>
    )
  }

  if (application) {
    return (
      <div className="flex flex-wrap items-center gap-2">
        <Badge variant="outline" className="h-9 rounded-md px-3">
          {t("statusBadge", { status: tStatuses(application.status) })}
        </Badge>
        {application.conversationId ? (
          <Button variant="outline" size="sm" asChild>
            <Link href={`/messages/${application.conversationId}`}>
              {t("viewConversation")}
            </Link>
          </Button>
        ) : null}
      </div>
    )
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button>{t("applyToHelp")}</Button>
      </DialogTrigger>
      <DialogContent className="max-w-md">
        <form onSubmit={handleApply}>
          <DialogHeader>
            <DialogTitle>{t("dialogTitle")}</DialogTitle>
            <DialogDescription>{t("dialogDescription")}</DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <Textarea
              value={message}
              onChange={(event) => setMessage(event.target.value)}
              maxLength={1000}
              placeholder={t("messagePlaceholder")}
              rows={5}
            />
          </div>
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => setOpen(false)}
            >
              {t("dialogCancel")}
            </Button>
            <Button type="submit" disabled={isApplying}>
              {isApplying ? t("sending") : t("send")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
