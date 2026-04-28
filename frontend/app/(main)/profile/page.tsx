import type { Metadata } from "next"
import Link from "next/link"

import { Button } from "@/components/ui/button"
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from "@/components/ui/tabs"
import { ProfileHeader } from "@/components/profile/profile-header"
import { ReputationCard } from "@/components/profile/reputation-card"
import { ReviewsList } from "@/components/profile/reviews-list"
import { TaskHistory } from "@/components/profile/task-history"
import { currentUser, mockReviews, mockTasks } from "@/lib/mock-data"

export const metadata: Metadata = {
  title: "Profile",
}

/**
 * Authenticated user's own profile (Module 3).
 *
 * Displays identity, reputation, reviews, and task history. The same
 * components power the public `/profile/[id]` view - the only
 * difference is the action buttons (Edit vs Message).
 */
export default function ProfilePage() {
  const reviews = mockReviews.filter((r) => r.targetUserId === currentUser.id)
  const taskHistory = mockTasks.filter(
    (t) =>
      t.seeker.id === currentUser.id || t.helper?.id === currentUser.id
  )

  return (
    <div className="flex flex-col gap-6">
      <ProfileHeader
        user={currentUser}
        actions={
          <Button asChild variant="outline">
            <Link href="/profile/edit">Edit profile</Link>
          </Button>
        }
      />
      <ReputationCard reputation={currentUser.reputation} />

      <Tabs defaultValue="reviews">
        <TabsList>
          <TabsTrigger value="reviews">Reviews ({reviews.length})</TabsTrigger>
          <TabsTrigger value="history">History ({taskHistory.length})</TabsTrigger>
        </TabsList>
        <TabsContent value="reviews">
          <ReviewsList reviews={reviews} />
        </TabsContent>
        <TabsContent value="history">
          <TaskHistory tasks={taskHistory} />
        </TabsContent>
      </Tabs>
    </div>
  )
}
