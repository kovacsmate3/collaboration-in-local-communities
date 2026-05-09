"use client"

import * as React from "react"
import Link from "next/link"

import { Button } from "@/components/ui/button"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { EmailVerificationBanner } from "@/components/profile/email-verification-banner"
import { ProfileHeader } from "@/components/profile/profile-header"
import { ReputationCard } from "@/components/profile/reputation-card"
import { ReviewsList } from "@/components/profile/reviews-list"
import { TaskHistory } from "@/components/profile/task-history"
import { useAuth } from "@/lib/auth-context"
import { useOwnProfile } from "@/lib/api/profiles"

export default function ProfilePage() {
  const { user } = useAuth()
  const { data: profile, isLoading, isError } = useOwnProfile()

  if (isLoading) {
    return <p className="text-sm text-muted-foreground">Loading…</p>
  }

  if (isError || !profile) {
    return (
      <p className="text-sm text-destructive">
        Could not load profile. Please try again.
      </p>
    )
  }

  return (
    <div className="flex flex-col gap-6">
      <EmailVerificationBanner />
      <ProfileHeader
        displayName={profile.displayName}
        bio={profile.bio}
        workplace={profile.workplace}
        position={profile.position}
        locationText={profile.locationText}
        photoUrl={profile.photoUrl}
        isVerified={user?.emailVerified}
        actions={
          <Button asChild variant="outline">
            <Link href="/profile/edit">Edit profile</Link>
          </Button>
        }
      />
      <ReputationCard
        averageRating={profile.averageRating}
        reviewCount={profile.reviewCount}
        completedTaskCount={profile.completedTaskCount}
      />

      <Tabs defaultValue="reviews">
        <TabsList>
          <TabsTrigger value="reviews">Reviews</TabsTrigger>
          <TabsTrigger value="history">History</TabsTrigger>
        </TabsList>
        <TabsContent value="reviews">
          <ReviewsList reviews={[]} />
        </TabsContent>
        <TabsContent value="history">
          <TaskHistory tasks={[]} />
        </TabsContent>
      </Tabs>
    </div>
  )
}
