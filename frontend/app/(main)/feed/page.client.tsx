"use client"

import * as React from "react"
import Link from "next/link"

import { Button } from "@/components/ui/button"
import { PageHeader } from "@/components/shared/page-header"
import { TaskList } from "@/components/tasks/task-list"
import { useInfiniteTaskList } from "@/lib/api/tasks"

export function FeedPageClient() {
  const sentinelRef = React.useRef<HTMLDivElement | null>(null)
  const {
    data,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    isLoading,
    isError,
  } = useInfiniteTaskList({ status: "Open" })

  const tasks = React.useMemo(() => data?.pages.flat() ?? [], [data])

  React.useEffect(() => {
    const sentinel = sentinelRef.current
    if (!sentinel || !hasNextPage) return

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry?.isIntersecting && !isFetchingNextPage) {
          void fetchNextPage()
        }
      },
      { rootMargin: "400px 0px" }
    )

    observer.observe(sentinel)
    return () => observer.disconnect()
  }, [fetchNextPage, hasNextPage, isFetchingNextPage])

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
        <>
          <TaskList
            tasks={tasks}
            emptyTitle="No open requests right now"
            emptyDescription="Be the first to post one - your neighbours might be ready to help."
            hideStatus
          />
          {tasks.length > 0 ? (
            <div ref={sentinelRef} className="flex justify-center py-2">
              {isFetchingNextPage ? (
                <p className="text-sm text-muted-foreground">
                  Loading more tasks…
                </p>
              ) : hasNextPage ? (
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => void fetchNextPage()}
                >
                  Load more
                </Button>
              ) : (
                <p className="text-sm text-muted-foreground">
                  You are all caught up.
                </p>
              )}
            </div>
          ) : null}
        </>
      )}
    </div>
  )
}
