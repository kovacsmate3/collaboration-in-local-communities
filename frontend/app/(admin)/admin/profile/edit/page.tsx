import type { Metadata } from "next"

import { ProfileEditPageContent } from "@/components/profile/profile-edit-page-content"
import { PageHeader } from "@/components/shared/page-header"

export const metadata: Metadata = {
  title: "Edit profile",
}

export default function AdminProfileEditPage() {
  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-6">
      <PageHeader
        title="Edit profile"
        description="Update your details, skills, and what you'd like to share publicly."
      />
      <ProfileEditPageContent returnHref="/admin/profile" />
    </div>
  )
}
