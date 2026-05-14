import type { Metadata } from "next"

import { PageHeader } from "@/components/shared/page-header"
import { ProfileEditPageContent } from "@/components/profile/profile-edit-page-content"

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
      <ProfileEditPageContent />
    </div>
  )
}
