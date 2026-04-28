import type { Metadata } from "next"

import { PageHeader } from "@/components/shared/page-header"
import { ProfileEditForm } from "@/components/profile/profile-edit-form"
import { currentUser } from "@/lib/mock-data"

export const metadata: Metadata = {
  title: "Edit profile",
}

export default function ProfileEditPage() {
  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-6">
      <PageHeader
        title="Edit profile"
        description="Update your details, skills, and what you'd like to share publicly."
      />
      {/* TODO: replace `currentUser` with the authenticated user from session */}
      <ProfileEditForm user={currentUser} />
    </div>
  )
}
