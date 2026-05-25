"use client"

import Link from "next/link"
import { useRouter, useSearchParams } from "next/navigation"
import { toast } from "sonner"

import { Button } from "@/components/ui/button"
import { useAcceptTerms } from "@/lib/api/terms"
import { useAuth } from "@/lib/auth-context"
import { APP_AUTH_ROUTES } from "@/lib/auth/constants"
import {
  getHomePathForRole,
  getPostAuthRedirectPath,
} from "@/lib/auth/functions"

export function TermsAcceptancePanel() {
  const { user, isLoading, refreshSession, logout } = useAuth()
  const acceptTerms = useAcceptTerms()
  const router = useRouter()
  const searchParams = useSearchParams()

  if (isLoading || !user || user.terms.hasAccepted) {
    return null
  }

  const currentUser = user

  async function handleAcceptTerms() {
    const termsVersionId = currentUser.terms.activeVersionId
    if (!termsVersionId) {
      toast.error("Terms are not available right now. Please try again later.")
      return
    }

    try {
      await acceptTerms.mutateAsync(termsVersionId)
      const nextUser = await refreshSession()
      toast.success("Terms accepted")
      router.replace(
        getPostAuthRedirectPath(
          searchParams.get("next"),
          nextUser?.role ?? currentUser.role
        )
      )
      router.refresh()
    } catch {
      toast.error("Unable to record your acceptance. Please try again.")
    }
  }

  async function handleUseAnotherAccount() {
    await logout()
    router.replace(APP_AUTH_ROUTES.login)
    router.refresh()
  }

  return (
    <section className="mb-10 rounded-lg border bg-card p-4 text-card-foreground shadow-xs">
      <div className="grid gap-2">
        <h2 className="text-base font-semibold">
          Accept Terms & Conditions to continue
        </h2>
        <p className="text-sm text-muted-foreground">
          Your account needs an acceptance record for the current terms before
          you can continue into the app.
        </p>
        <p className="text-sm">
          {currentUser.terms.activeTitle ?? "Current terms"}
          {currentUser.terms.activeVersion ? (
            <span className="text-muted-foreground">
              {" "}
              version {currentUser.terms.activeVersion}
            </span>
          ) : null}
        </p>
      </div>
      <div className="mt-4 flex flex-col gap-2 sm:flex-row">
        <Button
          type="button"
          disabled={acceptTerms.isPending}
          onClick={() => void handleAcceptTerms()}
        >
          {acceptTerms.isPending ? "Accepting..." : "Accept and continue"}
        </Button>
        <Button
          type="button"
          variant="outline"
          disabled={acceptTerms.isPending}
          onClick={() => void handleUseAnotherAccount()}
        >
          Use another account
        </Button>
      </div>
    </section>
  )
}

export function TermsBackLink() {
  const { user, isLoading } = useAuth()
  if (user && !user.terms.hasAccepted) {
    return null
  }

  const href = user ? getHomePathForRole(user.role) : APP_AUTH_ROUTES.register
  const label = user ? "Back to app" : "Back to registration"

  return (
    <Link
      href={href}
      className="mb-8 inline-block text-sm text-muted-foreground hover:text-foreground"
    >
      &larr; {isLoading ? "Back" : label}
    </Link>
  )
}
