import type { Metadata } from "next"
import Link from "next/link"

import { Button } from "@/components/ui/button"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { PageHeader } from "@/components/shared/page-header"
import { TaskList } from "@/components/tasks/task-list"
import { currentUser, mockTasks } from "@/lib/mock-data"

export const metadata: Metadata = {
  title: "My tasks",
}

/**
 * Job/Task management page (Module 7).
 *
 * Two views in one screen:
 *   - "Posted" - tasks where the current user is the Seeker
 *   - "Accepted" - tasks where the current user is the Helper
 *
 * Both are filtered from the same source so a single backend endpoint
 * (`GET /tasks?involves=me`) can drive them.
 */
export default function TasksPage() {
  const posted = mockTasks.filter((t) => t.seeker.id === currentUser.id)
  const accepted = mockTasks.filter((t) => t.helper?.id === currentUser.id)

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="My tasks"
        description="Track everything you've posted or accepted, from open to reviewed."
        actions={
          <Button asChild>
            <Link href="/post-task">Post a task</Link>
          </Button>
        }
      />

      <Tabs defaultValue="posted">
        <TabsList>
          <TabsTrigger value="posted">Posted ({posted.length})</TabsTrigger>
          <TabsTrigger value="accepted">
            Accepted ({accepted.length})
          </TabsTrigger>
        </TabsList>

        <TabsContent value="posted">
          <TaskList
            tasks={posted}
            emptyTitle="You haven't posted anything yet"
            emptyDescription="Post your first request and get help from the community."
          />
        </TabsContent>
        <TabsContent value="accepted">
          <TaskList
            tasks={accepted}
            emptyTitle="No accepted tasks"
            emptyDescription="Browse the Helper Feed and accept a task to see it here."
          />
        </TabsContent>
      </Tabs>
    </div>
  )
}
