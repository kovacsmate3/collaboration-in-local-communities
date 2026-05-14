"use client"

import * as React from "react"
import { toast } from "sonner"

import { Button } from "@/components/ui/button"
import { useAuth } from "@/lib/auth-context"
import { resendVerificationEmail } from "@/lib/auth/functions"

export function EmailVerificationBanner() {
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
      toast.success("Verification email sent — check your inbox.")
    } catch {
      toast.error("Could not send the email. Please try again.")
    } finally {
      setIsResending(false)
    }
  }

  return (
    <div className="rounded-md border border-amber-200 bg-amber-50 px-5 py-4 dark:border-amber-900 dark:bg-amber-950">
      <p className="font-medium text-amber-800 dark:text-amber-200">
        Your email is not verified
      </p>
      <p className="mt-1 text-sm text-amber-700 dark:text-amber-300">
        {sent
          ? `A new verification link was sent to ${user.email}. Check your inbox.`
          : `Verify ${user.email} to unlock posting tasks and other features.`}
      </p>
      {!sent ? (
        <Button
          size="sm"
          variant="outline"
          disabled={isResending}
          onClick={handleResend}
          className="mt-3 border-amber-300 bg-transparent text-amber-800 hover:bg-amber-100 dark:border-amber-700 dark:text-amber-200 dark:hover:bg-amber-900"
        >
          {isResending ? "Sending..." : "Resend verification email"}
        </Button>
      ) : null}
    </div>
  )
}
