"use client"

import Link from "next/link"
import { useLocale, useTranslations } from "next-intl"
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
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { UserAvatar } from "@/components/shared/user-avatar"
import { formatRelativeTime } from "@/lib/format"
import {
  type ApiTaskApplication,
  usePatchTaskApplication,
} from "@/lib/api/tasks"

interface TaskApplicationsPanelProps {
  applications: ApiTaskApplication[]
  isLoading: boolean
  taskId: string
}

/**
 * Seeker-side panel listing applications received for a task. Each row
 * has accept/reject actions while the application is still Pending.
 */
export function TaskApplicationsPanel({
  applications,
  isLoading,
  taskId,
}: TaskApplicationsPanelProps) {
  const t = useTranslations("tasks.applications")
  const tStatuses = useTranslations("tasks.applications.statuses")
  const tCommon = useTranslations("common")
  const locale = useLocale()
  const { mutate: patchApplication, isPending } =
    usePatchTaskApplication(taskId)

  function handleAction(applicationId: string, action: "accept" | "reject") {
    patchApplication(
      { applicationId, action },
      {
        onSuccess: () =>
          toast.success(
            action === "accept" ? t("acceptedToast") : t("rejectedToast")
          ),
        onError: () => toast.error(t("updateErrorToast")),
      }
    )
  }

  return (
    <section className="rounded-lg border bg-card p-4">
      <div className="mb-3 flex items-center justify-between gap-3">
        <h2 className="text-sm font-medium">{t("title")}</h2>
        <Badge variant="muted">{isLoading ? "…" : applications.length}</Badge>
      </div>
      {isLoading ? (
        <p className="text-sm text-muted-foreground">{t("loading")}</p>
      ) : applications.length === 0 ? (
        <p className="text-sm text-muted-foreground">{t("empty")}</p>
      ) : (
        <div className="divide-y">
          {applications.map((application) => (
            <div
              key={application.id}
              className="flex flex-col gap-3 py-3 first:pt-0 last:pb-0"
            >
              <div className="flex flex-wrap items-center gap-2">
                <UserAvatar size="sm" name={application.helperDisplayName} />
                <span className="text-sm font-medium">
                  {application.helperDisplayName}
                </span>
                <Badge variant="outline">{tStatuses(application.status)}</Badge>
                <span className="ml-auto text-xs text-muted-foreground">
                  {formatRelativeTime(application.createdAt, locale)}
                </span>
              </div>
              {application.message ? (
                <p className="text-sm text-muted-foreground">
                  {application.message}
                </p>
              ) : null}
              <div className="flex flex-wrap gap-2">
                {application.conversationId ? (
                  <Button size="sm" variant="outline" asChild>
                    <Link href={`/messages/${application.conversationId}`}>
                      {t("viewChat")}
                    </Link>
                  </Button>
                ) : null}
                {application.status === "Pending" ? (
                  <>
                    <AlertDialog>
                      <AlertDialogTrigger asChild>
                        <Button size="sm" disabled={isPending}>
                          {t("accept")}
                        </Button>
                      </AlertDialogTrigger>
                      <AlertDialogContent>
                        <AlertDialogHeader>
                          <AlertDialogTitle>
                            {t("acceptDialogTitle")}
                          </AlertDialogTitle>
                          <AlertDialogDescription>
                            {t("acceptDialogBody", {
                              name: application.helperDisplayName,
                            })}
                          </AlertDialogDescription>
                        </AlertDialogHeader>
                        <AlertDialogFooter>
                          <AlertDialogCancel>
                            {tCommon("goBack")}
                          </AlertDialogCancel>
                          <AlertDialogAction
                            disabled={isPending}
                            onClick={() =>
                              handleAction(application.id, "accept")
                            }
                          >
                            {isPending ? t("accepting") : t("accept")}
                          </AlertDialogAction>
                        </AlertDialogFooter>
                      </AlertDialogContent>
                    </AlertDialog>
                    <Button
                      size="sm"
                      variant="ghost"
                      disabled={isPending}
                      onClick={() => handleAction(application.id, "reject")}
                    >
                      {t("reject")}
                    </Button>
                  </>
                ) : null}
              </div>
            </div>
          ))}
        </div>
      )}
    </section>
  )
}
