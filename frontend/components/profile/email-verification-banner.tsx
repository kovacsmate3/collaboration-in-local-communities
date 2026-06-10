"use client"

import * as React from "react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"

import { Button } from "@/components/ui/button"
import { useAuth } from "@/lib/auth-context"
import { resendVerificationEmail } from "@/lib/auth/functions"

export function EmailVerificationBanner() {
  const t = useTranslations("profile.emailBanner")
  const { user } = useAuth()
  const [isResending, setIsResending] = React.useState(false)
  const [sent, setSent] = React.useState(false)

  if (!user || user.emailVerified) {
    return null
  }

  async function handleResend() {
    if (!user?.email) return
    setIsResending(true)
    try {
      await resendVerificationEmail(user.email)
      setSent(true)
      toast.success(t("sentToast"))
    } catch {
      toast.error(t("sendError"))
    } finally {
      setIsResending(false)
    }
  }

  return (
    <div className="rounded-md border border-amber-200 bg-amber-50 px-5 py-4 dark:border-amber-900 dark:bg-amber-950">
      <p className="font-medium text-amber-800 dark:text-amber-200">
        {t("title")}
      </p>
      <p className="mt-1 text-sm text-amber-700 dark:text-amber-300">
        {sent
          ? t("bodySent", { email: user.email })
          : t("bodyUnverified", { email: user.email })}
      </p>
      {!sent ? (
        <Button
          size="sm"
          variant="outline"
          disabled={isResending}
          onClick={handleResend}
          className="mt-3 border-amber-300 bg-transparent text-amber-800 hover:bg-amber-100 dark:border-amber-700 dark:text-amber-200 dark:hover:bg-amber-900"
        >
          {isResending ? t("sending") : t("resend")}
        </Button>
      ) : null}
    </div>
  )
}
