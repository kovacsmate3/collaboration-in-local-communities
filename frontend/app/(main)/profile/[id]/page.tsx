"use client"

import * as React from "react"
import { notFound } from "next/navigation"
import Link from "next/link"

import { Button } from "@/components/ui/button"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { ProfileHeader } from "@/components/profile/profile-header"
import { ReputationCard } from "@/components/profile/reputation-card"
import { ReviewsList } from "@/components/profile/reviews-list"
import { TaskHistory } from "@/components/profile/task-history"
import { usePublicProfile } from "@/lib/api/profiles"

interface PublicProfilePageProps {
  params: Promise<{ id: string }>
}

export default function PublicProfilePage({ params }: PublicProfilePageProps) {
  const { id } = React.use(params)
  const { data: profile, isLoading, isError } = usePublicProfile(id)

  if (isLoading) {
    return <p className="text-sm text-muted-foreground">Loading…</p>
  }

  if (isError || !profile) {
    notFound()
  }

  return (
    <div className="flex flex-col gap-6">
      <ProfileHeader
        displayName={profile.displayName}
        bio={profile.bio}
        workplace={profile.workplace}
        position={profile.position}
        locationText={profile.locationText}
        photoUrl={profile.photoUrl}
        actions={
          <Button asChild>
            <Link href="/messages">Message</Link>
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
