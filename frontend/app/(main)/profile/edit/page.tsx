"use client"

import { PageHeader } from "@/components/shared/page-header"
import { ProfileEditForm } from "@/components/profile/profile-edit-form"
import { useOwnProfile } from "@/lib/api/profiles"

export default function ProfileEditPage() {
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
    <div className="mx-auto flex max-w-2xl flex-col gap-6">
      <PageHeader
        title="Edit profile"
        description="Update your details, skills, and what you'd like to share publicly."
      />
      <ProfileEditForm profile={profile} />
    </div>
  )
}
