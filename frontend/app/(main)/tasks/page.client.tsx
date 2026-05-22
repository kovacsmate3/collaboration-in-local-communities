"use client"

import Link from "next/link"

import { Button } from "@/components/ui/button"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { PageHeader } from "@/components/shared/page-header"
import { TaskList } from "@/components/tasks/task-list"
import { useAuth } from "@/lib/auth-context"
import { useMyTaskApplications, useTaskList } from "@/lib/api/tasks"

export function TasksPageClient() {
  const { user } = useAuth()
  const { data: tasks = [], isLoading, isError } = useTaskList()
  const {
    data: applications = [],
    isLoading: isLoadingApplications,
    isError: isApplicationsError,
  } = useMyTaskApplications()

  const posted = tasks.filter((t) => t.seekerProfileId === user?.profileId)
  const accepted = tasks.filter(
    (t) => t.acceptedHelperProfileId === user?.profileId
  )
  const applied = applications.map((a) => a.task)

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="My tasks"
        description="Track everything you've posted or accepted, from open to completed."
        actions={
          <Button asChild>
            <Link href="/post-task">Post a task</Link>
          </Button>
        }
      />

      <Tabs defaultValue="posted">
        <TabsList>
          <TabsTrigger value="posted">
            Posted ({isLoading ? "…" : posted.length})
          </TabsTrigger>
          <TabsTrigger value="accepted">
            Accepted ({isLoading ? "…" : accepted.length})
          </TabsTrigger>
          <TabsTrigger value="applied">
            Applied ({isLoadingApplications ? "…" : applied.length})
          </TabsTrigger>
        </TabsList>

        {isError || isApplicationsError ? (
          <p className="mt-4 text-sm text-destructive">
            Could not load tasks. Please try again.
          </p>
        ) : (
          <>
            <TabsContent value="posted">
              {isLoading ? (
                <p className="mt-4 text-sm text-muted-foreground">Loading…</p>
              ) : (
                <TaskList
                  tasks={posted}
                  emptyTitle="You haven't posted anything yet"
                  emptyDescription="Post your first request and get help from the community."
                />
              )}
            </TabsContent>
            <TabsContent value="accepted">
              {isLoading ? (
                <p className="mt-4 text-sm text-muted-foreground">Loading…</p>
              ) : (
                <TaskList
                  tasks={accepted}
                  emptyTitle="No accepted tasks"
                  emptyDescription="Browse the feed and accept a task to see it here."
                />
              )}
            </TabsContent>
            <TabsContent value="applied">
              {isLoadingApplications ? (
                <p className="mt-4 text-sm text-muted-foreground">Loading…</p>
              ) : (
                <TaskList
                  tasks={applied}
                  emptyTitle="No applications yet"
                  emptyDescription="Apply to open tasks from the helper feed to track them here."
                />
              )}
            </TabsContent>
          </>
        )}
      </Tabs>
    </div>
  )
}
