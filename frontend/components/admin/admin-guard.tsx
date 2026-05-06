"use client"

import * as React from "react"
import { usePathname, useRouter } from "next/navigation"

import { Skeleton } from "@/components/ui/skeleton"
import { useAuth } from "@/lib/auth-context"
import { APP_AUTH_ROUTES, APP_HOME_ROUTES } from "@/lib/auth/constants"

/**
 * Defence-in-depth guard for all /admin/* routes.
 *
 * The edge middleware (middleware.ts → proxy.ts) is the primary gate: it
 * checks the access-token cookie, refreshes when stale, and redirects
 * unauthenticated users to /login and signed-in non-admins to /feed before
 * the page even renders. This client-side guard exists for the cases the
 * edge can't cover cleanly:
 *
 *  - The session expiring while the user is sat on /admin (next nav will
 *    hit middleware, but in-page activity won't).
 *  - Local development with the dev server hot-reloading the middleware.
 *  - Any future scenario where role data on the client diverges from the
 *    cookie (e.g. multi-tab logout).
 *
 * While the session is loading we render a skeleton; once settled we
 * redirect unauthenticated visitors to /login (preserving the original
 * target as ?next=) and signed-in non-admins to /feed.
 */
export function AdminGuard({ children }: { children: React.ReactNode }) {
  const { isLoading, user, isAdmin } = useAuth()
  const router = useRouter()
  const pathname = usePathname()

  React.useEffect(() => {
    if (isLoading) return

    if (!user) {
      const next = encodeURIComponent(pathname || APP_HOME_ROUTES.admin)
      router.replace(`${APP_AUTH_ROUTES.login}?next=${next}`)
      return
    }

    if (!isAdmin) {
      router.replace(APP_HOME_ROUTES.user)
    }
  }, [isLoading, user, isAdmin, router, pathname])

  if (isLoading) {
    return (
      <div className="flex h-svh flex-col gap-4 p-8">
        <Skeleton className="h-8 w-48" />
        <div className="flex flex-1 gap-4">
          <Skeleton className="h-full w-56" />
          <div className="flex-1 space-y-4">
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-32 w-full" />
            <Skeleton className="h-32 w-full" />
          </div>
        </div>
      </div>
    )
  }

  if (!user || !isAdmin) {
    // Redirect is in flight; render nothing to avoid flashing admin chrome.
    return null
  }

  return <>{children}</>
}
