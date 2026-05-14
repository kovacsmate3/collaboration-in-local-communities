"use client"

import Link from "next/link"

import { Button } from "@/components/ui/button"
import { PageHeader } from "@/components/shared/page-header"
import { TaskList } from "@/components/tasks/task-list"
import { useTaskList } from "@/lib/api/tasks"

export function FeedPageClient() {
  const {
    data: tasks = [],
    isLoading,
    isError,
  } = useTaskList({ status: "Open" })

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="What's happening locally"
        description="Open requests from your community. Need help yourself? Post a task in seconds."
        actions={
          <Button asChild>
            <Link href="/post-task">Post a task</Link>
          </Button>
        }
      />
      {isLoading ? (
        <p className="text-sm text-muted-foreground">Loading tasks…</p>
      ) : isError ? (
        <p className="text-sm text-destructive">
          Could not load tasks. Please try again.
        </p>
      ) : (
        <TaskList
          tasks={tasks}
          emptyTitle="No open requests right now"
          emptyDescription="Be the first to post one - your neighbours might be ready to help."
          hideStatus
        />
      )}
    </div>
  )
}
