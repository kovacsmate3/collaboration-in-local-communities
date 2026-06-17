import { Suspense } from "react"
import type { Metadata } from "next"
import { getTranslations } from "next-intl/server"

import { ResetPasswordForm } from "@/components/auth/reset-password-form"

export const metadata: Metadata = {
  title: "Set a new password",
}

export default async function ResetPasswordPage() {
  const t = await getTranslations("auth.reset")

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-col gap-1">
        <h1 className="font-heading text-page-title">{t("headerTitle")}</h1>
        <p className="text-sm text-muted-foreground">{t("headerSubtitle")}</p>
      </header>
      <Suspense>
        <ResetPasswordForm />
      </Suspense>
    </div>
  )
}
