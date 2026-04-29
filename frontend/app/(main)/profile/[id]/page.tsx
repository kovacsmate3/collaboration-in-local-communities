import { notFound } from "next/navigation"
import Link from "next/link"

import { Button } from "@/components/ui/button"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { ProfileHeader } from "@/components/profile/profile-header"
import { ReputationCard } from "@/components/profile/reputation-card"
import { ReviewsList } from "@/components/profile/reviews-list"
import { TaskHistory } from "@/components/profile/task-history"
import { currentUser, mockReviews, mockTasks } from "@/lib/mock-data"

interface PublicProfilePageProps {
  params: Promise<{ id: string }>
}

/**
 * Public profile view at `/profile/[id]`.
 *
 * Mirrors the self-profile but swaps the primary action to "Message".
 * Once the API is wired we'll fetch the user by id; for now we just
 * fall back to the current user as a placeholder so the page renders.
 */
export default async function PublicProfilePage({
  params,
}: PublicProfilePageProps) {
  const { id } = await params

  // TODO: fetch by id from the API
  const user = id === currentUser.id ? currentUser : null
  if (!user) notFound()

  const reviews = mockReviews.filter((r) => r.targetUserId === user.id)
  const taskHistory = mockTasks.filter(
    (t) => t.seeker.id === user.id || t.helper?.id === user.id
  )

  return (
    <div className="flex flex-col gap-6">
      <ProfileHeader
        user={user}
        actions={
          <Button asChild>
            <Link href="/messages">Message</Link>
          </Button>
        }
      />
      <ReputationCard reputation={user.reputation} />

      <Tabs defaultValue="reviews">
        <TabsList>
          <TabsTrigger value="reviews">Reviews ({reviews.length})</TabsTrigger>
          <TabsTrigger value="history">
            History ({taskHistory.length})
          </TabsTrigger>
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
