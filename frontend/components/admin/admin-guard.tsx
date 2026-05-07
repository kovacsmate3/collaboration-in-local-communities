"use client"

import { Suspense } from "react"

import { AdminGuardInner } from "./admin-guard-inner"
import { AdminGuardSkeleton } from "./admin-guard-skeleton"

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
export function AdminGuard({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <Suspense fallback={<AdminGuardSkeleton />}>
      <AdminGuardInner>{children}</AdminGuardInner>
    </Suspense>
  )
}
