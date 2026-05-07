"use client"

import * as React from "react"
import { usePathname, useRouter, useSearchParams } from "next/navigation"

import { useAuth } from "@/lib/auth-context"
import { APP_AUTH_ROUTES, APP_HOME_ROUTES } from "@/lib/auth/constants"

import { AdminGuardSkeleton } from "./admin-guard-skeleton"

export function AdminGuardInner({
  children,
}: {
  children: React.ReactNode
}) {
  const { isLoading, user, isAdmin } = useAuth()
  const router = useRouter()
  const pathname = usePathname()
  const searchParams = useSearchParams()

  React.useEffect(() => {
    if (isLoading) return

    if (!user) {
      const search = searchParams.toString()
      const returnTo =
        `${pathname}${search ? `?${search}` : ""}` ||
        APP_HOME_ROUTES.admin
      const next = encodeURIComponent(returnTo)
      router.replace(
        `${APP_AUTH_ROUTES.login}?next=${next}`
      )
      return
    }

    if (!isAdmin) {
      router.replace(APP_HOME_ROUTES.user)
    }
  }, [isLoading, user, isAdmin, router, pathname, searchParams])

  if (isLoading) {
    return <AdminGuardSkeleton />
  }

  if (!user || !isAdmin) {
    // Redirect is in flight; render nothing to avoid flashing admin chrome.
    return null
  }

  return <>{children}</>
}
