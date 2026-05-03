"use client"

import * as React from "react"
import { useRouter } from "next/navigation"
import { useAuth } from "@/lib/auth-context"
import { Skeleton } from "@/components/ui/skeleton"

/**
 * Wraps all /admin/* routes. While the session is loading it shows a
 * skeleton; once settled it redirects non-admin visitors to /feed.
 *
 * TODO: once real auth ships, ensure the redirect target is the post-login
 * landing page for the user's role (currently /feed for regular users).
 */
export function AdminGuard({ children }: { children: React.ReactNode }) {
  const { isLoading, isAdmin } = useAuth()
  const router = useRouter()

  React.useEffect(() => {
    if (!isLoading && !isAdmin) {
      router.replace("/feed")
    }
  }, [isLoading, isAdmin, router])

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

  if (!isAdmin) {
    // Will redirect via useEffect; render nothing in the meantime
    return null
  }

  return <>{children}</>
}
