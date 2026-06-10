import type { Metadata } from "next"
import { getTranslations } from "next-intl/server"

import { PageHeader } from "@/components/shared/page-header"
import { ProfileEditPageContent } from "@/components/profile/profile-edit-page-content"

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("profile.editPage")
  return {
    title: t("title"),
  }
}

export default async function ProfileEditPage() {
  const t = await getTranslations("profile.editPage")
  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-6">
      <PageHeader title={t("title")} description={t("description")} />
      <ProfileEditPageContent />
    </div>
  )
}
