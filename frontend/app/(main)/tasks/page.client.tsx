"use client"

import Link from "next/link"
import { InboxIcon } from "@hugeicons/core-free-icons"
import { useTranslations } from "next-intl"

import { Button } from "@/components/ui/button"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { PageHeader } from "@/components/shared/page-header"
import { TaskCard } from "@/components/tasks/task-card"
import { TaskList } from "@/components/tasks/task-list"
import { EmptyState } from "@/components/shared/empty-state"
import { ErrorState } from "@/components/shared/error-state"
import { LoadingState } from "@/components/shared/loading-state"
import { useAuth } from "@/lib/auth-context"
import { useMyTaskApplications, useTaskList } from "@/lib/api/tasks"

export function TasksPageClient() {
  const t = useTranslations("tasks.myTasks")
  const { user } = useAuth()
  const {
    data: tasks = [],
    isLoading,
    isError,
    refetch: refetchTasks,
  } = useTaskList()
  const {
    data: applications = [],
    isLoading: isLoadingApplications,
    isError: isApplicationsError,
    refetch: refetchApplications,
  } = useMyTaskApplications()

  function handleRetry() {
    if (isError) void refetchTasks()
    if (isApplicationsError) void refetchApplications()
  }

  const posted = tasks.filter((t) => t.seekerProfileId === user?.profileId)
  const accepted = tasks.filter(
    (t) => t.acceptedHelperProfileId === user?.profileId
  )

  const countOrEllipsis = (loading: boolean, count: number) =>
    loading ? "…" : String(count)

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title={t("title")}
        description={t("description")}
        actions={
          <Button asChild>
            <Link href="/post-task">{t("postTask")}</Link>
          </Button>
        }
      />

      <Tabs defaultValue="posted">
        <TabsList>
          <TabsTrigger value="posted">
            {t("tabPosted")} ({countOrEllipsis(isLoading, posted.length)})
          </TabsTrigger>
          <TabsTrigger value="accepted">
            {t("tabAccepted")} ({countOrEllipsis(isLoading, accepted.length)})
          </TabsTrigger>
          <TabsTrigger value="applied">
            {t("tabApplied")} (
            {countOrEllipsis(isLoadingApplications, applications.length)})
          </TabsTrigger>
        </TabsList>

        {isError || isApplicationsError ? (
          <div className="mt-4">
            <ErrorState onRetry={handleRetry} />
          </div>
        ) : (
          <>
            <TabsContent value="posted">
              {isLoading ? (
                <div className="mt-4">
                  <LoadingState rows={3} />
                </div>
              ) : (
                <TaskList
                  tasks={posted}
                  emptyTitle={t("emptyPostedTitle")}
                  emptyDescription={t("emptyPostedBody")}
                />
              )}
            </TabsContent>
            <TabsContent value="accepted">
              {isLoading ? (
                <div className="mt-4">
                  <LoadingState rows={3} />
                </div>
              ) : (
                <TaskList
                  tasks={accepted}
                  emptyTitle={t("emptyAcceptedTitle")}
                  emptyDescription={t("emptyAcceptedBody")}
                />
              )}
            </TabsContent>
            <TabsContent value="applied">
              {isLoadingApplications ? (
                <div className="mt-4">
                  <LoadingState rows={3} />
                </div>
              ) : applications.length === 0 ? (
                <EmptyState
                  icon={InboxIcon}
                  title={t("emptyAppliedTitle")}
                  description={t("emptyAppliedBody")}
                />
              ) : (
                <ul className="flex flex-col gap-3">
                  {applications.map((application) => (
                    <li key={application.id}>
                      <TaskCard
                        task={application.task}
                        applicationStatus={application.status}
                      />
                    </li>
                  ))}
                </ul>
              )}
            </TabsContent>
          </>
        )}
      </Tabs>
    </div>
  )
}
