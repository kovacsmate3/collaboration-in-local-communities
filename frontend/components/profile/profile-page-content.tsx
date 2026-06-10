"use client"

import { useState } from "react"
import Link from "next/link"
import { useQueries } from "@tanstack/react-query"
import { useTranslations } from "next-intl"

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { ProfileHeader } from "@/components/profile/profile-header"
import { PointsWallet } from "@/components/profile/points-wallet"
import { ReputationCard } from "@/components/profile/reputation-card"
import { ReviewsList } from "@/components/profile/reviews-list"
import { TaskHistory } from "@/components/profile/task-history"
import { useAuth } from "@/lib/auth-context"
import {
  type OwnProfileResponse,
  type PublicProfileResponse,
  POINTS_LEDGER_PAGE_SIZE,
  profileKeys,
  toProfileUser,
  useOwnProfile,
  usePointsBalance,
  usePointsLedger,
  useProfileReputationTrend,
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
  const t = useTranslations("profile.page")
  const { user: authUser } = useAuth()
  const isOwnRoute = !profileId
  const isOwner = Boolean(profileId && authUser?.profileId === profileId)
  const isOwnView = isOwnRoute || isOwner
  const ownProfile = useOwnProfile(isOwnRoute)
  const publicProfile = usePublicProfile(profileId ?? "")
  const query = isOwnRoute ? ownProfile : publicProfile

  if (query.isLoading) {
    return <ProfileSkeleton />
  }

  if (query.isError || !query.data) {
    return (
      <Alert variant="destructive">
        <AlertTitle>
          {t(isOwnView ? "loadErrorTitleOwn" : "loadErrorTitle")}
        </AlertTitle>
        <AlertDescription>
          {t(isOwnView ? "loadErrorBodyOwn" : "loadErrorBody")}
        </AlertDescription>
      </Alert>
    )
  }

  return (
    <ProfileLoaded
      profile={query.data}
      canEdit={isOwnView}
      editHref={editHref}
      showMessage={!isOwnView}
      showOwnerCtas={isOwnView}
      isOwn={isOwnView}
    />
  )
}

function ProfileLoaded({
  profile,
  canEdit,
  editHref,
  showMessage,
  showOwnerCtas,
  isOwn,
}: {
  profile: OwnProfileResponse | PublicProfileResponse
  canEdit: boolean
  editHref: string
  showMessage: boolean
  showOwnerCtas: boolean
  isOwn: boolean
}) {
  const t = useTranslations("profile.page")
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
  const [reviewsPage, setReviewsPage] = useState(1)
  // Reset the page whenever ProfileLoaded is reused for a different profile
  // during client-side navigation, otherwise the prior profile's page can
  // leak across and surface as an empty reviews tab on a profile with fewer
  // pages. Done during render via a previous-prop snapshot per React's
  // "Adjusting state when a prop changes" guidance (preferred over useEffect).
  const [ledgerPage, setLedgerPage] = useState(1)
  const [prevProfileId, setPrevProfileId] = useState(profile.id)
  if (prevProfileId !== profile.id) {
    setPrevProfileId(profile.id)
    setReviewsPage(1)
    setLedgerPage(1)
  }
  const reviewsQuery = useProfileReviews(profile.id, reviewsPage)
  const taskHistoryQuery = useProfileTaskHistory(profile.id)
  const reputationTrendQuery = useProfileReputationTrend(profile.id)
  // Points are private (me-only endpoints), so only query and surface them on
  // the viewer's own profile.
  const pointsBalanceQuery = usePointsBalance(isOwn)
  const pointsLedgerQuery = usePointsLedger(
    ledgerPage,
    POINTS_LEDGER_PAGE_SIZE,
    isOwn
  )
  const reviews = reviewsQuery.data?.items ?? []
  const taskHistory = taskHistoryQuery.data ?? []
  const reviewsCount = reviewsQuery.data?.totalCount ?? profile.reviewCount
  const reviewsTotalPages = reviewsQuery.data?.totalPages ?? 0
  const taskHistoryCount =
    taskHistoryQuery.data?.length ?? profile.completedTaskCount

  return (
    <div className="flex flex-col gap-6">
      <ProfileHeader
        user={profileUser}
        actions={
          canEdit ? (
            <Button asChild variant="outline">
              <Link href={editHref}>{t("editProfile")}</Link>
            </Button>
          ) : showMessage ? (
            <Button asChild>
              <Link href="/messages">{t("message")}</Link>
            </Button>
          ) : null
        }
      />
      <ReputationCard
        score={profileUser.reputation.points}
        averageRating={profileUser.reputation.averageRating}
        reviewCount={profileUser.reputation.reviewCount}
        completedTaskCount={profileUser.reputation.completedTasks}
        trend={reputationTrendQuery.data ?? []}
        showOwnerCtas={showOwnerCtas}
      />

      <Tabs defaultValue="reviews">
        <TabsList>
          <TabsTrigger value="reviews">
            {t(isOwn ? "reviewsTabOwn" : "reviewsTab", { count: reviewsCount })}
          </TabsTrigger>
          <TabsTrigger value="history">
            {t(isOwn ? "historyTabOwn" : "historyTab", {
              count: taskHistoryCount,
            })}
          </TabsTrigger>
          {isOwn ? (
            <TabsTrigger value="points">{t("pointsTabOwn")}</TabsTrigger>
          ) : null}
        </TabsList>
        <TabsContent value="reviews">
          {reviewsQuery.isLoading ? (
            <ProfileTabSkeleton />
          ) : reviewsQuery.isError ? (
            <ProfileTabError
              title={t(isOwn ? "reviewsUnavailableOwn" : "reviewsUnavailable")}
            />
          ) : (
            <ReviewsList
              reviews={reviews}
              page={reviewsPage}
              totalPages={reviewsTotalPages}
              onPageChange={setReviewsPage}
              isFetching={reviewsQuery.isFetching}
              isOwn={isOwn}
            />
          )}
        </TabsContent>
        <TabsContent value="history">
          {taskHistoryQuery.isLoading ? (
            <ProfileTabSkeleton />
          ) : taskHistoryQuery.isError ? (
            <ProfileTabError
              title={t(
                isOwn ? "taskHistoryUnavailableOwn" : "taskHistoryUnavailable"
              )}
            />
          ) : (
            <TaskHistory tasks={taskHistory} isOwn={isOwn} />
          )}
        </TabsContent>
        {isOwn ? (
          <TabsContent value="points">
            {pointsBalanceQuery.isLoading || pointsLedgerQuery.isLoading ? (
              <ProfileTabSkeleton />
            ) : pointsBalanceQuery.isError || pointsLedgerQuery.isError ? (
              <ProfileTabError title={t("pointsUnavailableOwn")} />
            ) : (
              <PointsWallet
                balance={pointsBalanceQuery.data?.balance ?? 0}
                entries={pointsLedgerQuery.data?.items ?? []}
                page={ledgerPage}
                totalPages={pointsLedgerQuery.data?.totalPages ?? 0}
                onPageChange={setLedgerPage}
                isFetching={pointsLedgerQuery.isFetching}
              />
            )}
          </TabsContent>
        ) : null}
      </Tabs>
    </div>
  )
}

function ProfileTabError({ title }: { title: string }) {
  const t = useTranslations("profile.page")
  return (
    <Alert variant="destructive">
      <AlertTitle>{title}</AlertTitle>
      <AlertDescription>{t("tabErrorBody")}</AlertDescription>
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
