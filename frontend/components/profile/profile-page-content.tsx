"use client"

import Link from "next/link"
import { useQueries } from "@tanstack/react-query"

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { ProfileHeader } from "@/components/profile/profile-header"
import { ReputationCard } from "@/components/profile/reputation-card"
import { ReviewsList } from "@/components/profile/reviews-list"
import { TaskHistory } from "@/components/profile/task-history"
import { useAuth } from "@/lib/auth-context"
import {
  type OwnProfileResponse,
  type PublicProfileResponse,
  profileKeys,
  toProfileUser,
  useOwnProfile,
  useProfileReviews,
  useProfileTaskHistory,
  usePublicProfile,
} from "@/lib/api/profile"
import { apiClient } from "@/lib/api/client"

interface ProfilePageContentProps {
  profileId?: string
  editHref?: string
}

export function ProfilePageContent({
  profileId,
  editHref = "/profile/edit",
}: ProfilePageContentProps) {
  const { user: authUser } = useAuth()
  const isOwnRoute = !profileId
  const isOwner = Boolean(profileId && authUser?.profileId === profileId)
  const ownProfile = useOwnProfile(isOwnRoute)
  const publicProfile = usePublicProfile(profileId ?? "")
  const query = isOwnRoute ? ownProfile : publicProfile

  if (query.isLoading) {
    return <ProfileSkeleton />
  }

  if (query.isError || !query.data) {
    return (
      <Alert variant="destructive">
        <AlertTitle>Profile unavailable</AlertTitle>
        <AlertDescription>
          The profile could not be loaded. It may have been removed or you may
          not have access to it.
        </AlertDescription>
      </Alert>
    )
  }

  return (
    <ProfileLoaded
      profile={query.data}
      canEdit={isOwnRoute || isOwner}
      editHref={editHref}
      showMessage={!isOwnRoute && !isOwner}
    />
  )
}

function ProfileLoaded({
  profile,
  canEdit,
  editHref,
  showMessage,
}: {
  profile: OwnProfileResponse | PublicProfileResponse
  canEdit: boolean
  editHref: string
  showMessage: boolean
}) {
  const skillIds = "skillIds" in profile ? profile.skillIds : []
  const skillQueries = useQueries({
    queries: skillIds.map((id) => ({
      queryKey: [...profileKeys.skill(id), "name-only"],
      queryFn: () => apiClient.get<{ name: string }>(`/skills/${id}`),
      enabled: id.length > 0,
    })),
  })
  const skillNames = skillQueries
    .map((query, index) => query.data?.name ?? skillIds[index])
    .filter((name): name is string => Boolean(name))
  const profileUser = toProfileUser(profile, skillNames)
  const reviewsQuery = useProfileReviews(profile.id)
  const taskHistoryQuery = useProfileTaskHistory(profile.id)
  const reviews = reviewsQuery.data ?? []
  const taskHistory = taskHistoryQuery.data ?? []
  const reviewsCount = reviewsQuery.data?.length ?? profile.reviewCount
  const taskHistoryCount =
    taskHistoryQuery.data?.length ?? profile.completedTaskCount

  return (
    <div className="flex flex-col gap-6">
      <ProfileHeader
        user={profileUser}
        actions={
          canEdit ? (
            <Button asChild variant="outline">
              <Link href={editHref}>Edit profile</Link>
            </Button>
          ) : showMessage ? (
            <Button asChild>
              <Link href="/messages">Message</Link>
            </Button>
          ) : null
        }
      />
      <ReputationCard reputation={profileUser.reputation} />

      <Tabs defaultValue="reviews">
        <TabsList>
          <TabsTrigger value="reviews">Reviews ({reviewsCount})</TabsTrigger>
          <TabsTrigger value="history">
            History ({taskHistoryCount})
          </TabsTrigger>
        </TabsList>
        <TabsContent value="reviews">
          {reviewsQuery.isLoading ? (
            <ProfileTabSkeleton />
          ) : reviewsQuery.isError ? (
            <ProfileTabError label="Reviews" />
          ) : (
            <ReviewsList reviews={reviews} />
          )}
        </TabsContent>
        <TabsContent value="history">
          {taskHistoryQuery.isLoading ? (
            <ProfileTabSkeleton />
          ) : taskHistoryQuery.isError ? (
            <ProfileTabError label="Task history" />
          ) : (
            <TaskHistory tasks={taskHistory} />
          )}
        </TabsContent>
      </Tabs>
    </div>
  )
}

function ProfileTabError({ label }: { label: string }) {
  return (
    <Alert variant="destructive">
      <AlertTitle>{label} unavailable</AlertTitle>
      <AlertDescription>
        This profile section could not be loaded. Please try again shortly.
      </AlertDescription>
    </Alert>
  )
}

function ProfileTabSkeleton() {
  return (
    <div className="flex flex-col gap-3">
      <Skeleton className="h-20 w-full" />
      <Skeleton className="h-20 w-full" />
    </div>
  )
}

function ProfileSkeleton() {
  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center gap-4">
        <Skeleton className="size-10 rounded-full" />
        <div className="flex flex-1 flex-col gap-2">
          <Skeleton className="h-6 w-48" />
          <Skeleton className="h-4 w-full max-w-xl" />
          <Skeleton className="h-4 w-72" />
        </div>
      </div>
      <Skeleton className="h-32 w-full" />
      <Skeleton className="h-56 w-full" />
    </div>
  )
}
