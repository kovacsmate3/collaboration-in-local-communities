import type { Metadata } from "next"
import Link from "next/link"

import { Button } from "@/components/ui/button"
import { PageHeader } from "@/components/shared/page-header"
import { TaskList } from "@/components/tasks/task-list"
import { mockTasks } from "@/lib/mock-data"

export const metadata: Metadata = {
  title: "Seeker feed",
}

/**
 * Seeker Feed (US-01, US-04, US-07, US-08, US-12).
 *
 * The default landing for authenticated users: a social-style feed of
 * help requests posted by the community. From here a Seeker can post a
 * new task; a Helper can browse and switch to the Helper Feed for
 * filtered search.
 */
export default function FeedPage() {
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
      <TaskList
        tasks={mockTasks.filter((t) => t.status === "open")}
        emptyTitle="No open requests right now"
        emptyDescription="Be the first to post one - your neighbours might be ready to help."
        hideStatus
      />
    </div>
  )
}
