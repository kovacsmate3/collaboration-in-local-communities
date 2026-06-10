"use client"

import { useQueries } from "@tanstack/react-query"
import { useTranslations } from "next-intl"

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Skeleton } from "@/components/ui/skeleton"
import { ProfileEditForm } from "@/components/profile/profile-edit-form"
import { apiClient } from "@/lib/api/client"
import {
  type SkillResponse,
  profileKeys,
  useOwnProfile,
} from "@/lib/api/profile"

interface ProfileEditPageContentProps {
  returnHref?: string
}

export function ProfileEditPageContent({
  returnHref = "/profile",
}: ProfileEditPageContentProps) {
  const t = useTranslations("profile.editPage")
  const profileQuery = useOwnProfile()
  const skillIds = profileQuery.data?.skillIds ?? []
  const skillQueries = useQueries({
    queries: skillIds.map((id) => ({
      queryKey: profileKeys.skill(id),
      queryFn: () => apiClient.get<SkillResponse>(`/skills/${id}`),
      enabled: id.length > 0,
    })),
  })

  if (profileQuery.isLoading) {
    return (
      <div className="flex flex-col gap-5">
        <Skeleton className="h-12 w-full" />
        <Skeleton className="h-20 w-full" />
        <Skeleton className="h-28 w-full" />
        <Skeleton className="h-10 w-40 self-end" />
      </div>
    )
  }

  if (profileQuery.isError || !profileQuery.data) {
    return (
      <Alert variant="destructive">
        <AlertTitle>{t("loadErrorTitle")}</AlertTitle>
        <AlertDescription>{t("loadErrorBody")}</AlertDescription>
      </Alert>
    )
  }

  const selectedSkills = skillQueries
    .map((query) => query.data)
    .filter((skill): skill is SkillResponse => Boolean(skill))

  return (
    <ProfileEditForm
      profile={profileQuery.data}
      selectedSkills={selectedSkills}
      returnHref={returnHref}
    />
  )
}
