"use client"

import Link from "next/link"
import { HugeiconsIcon } from "@hugeicons/react"
import { ShieldUserIcon } from "@hugeicons/core-free-icons"

import { Button } from "@/components/ui/button"
import { useAuth } from "@/lib/auth-context"
import { APP_HOME_ROUTES } from "@/lib/auth/constants"

/**
 * Admin shortcut shown in the main app header for users with the Admin
 * role. Hidden for non-admins and during the initial session check (so we
 * don't briefly reveal admin chrome to a user who isn't actually an admin).
 *
 * Renders nothing for regular users or while auth is still loading; the
 * AppHeader keeps server-side rendering for everything else.
 */
export function AdminHeaderLink() {
  const { isLoading, isAdmin } = useAuth()

  if (isLoading || !isAdmin) {
    return null
  }

  return (
    <Button
      asChild
      size="sm"
      variant="outline"
      className="hidden gap-2 sm:inline-flex"
    >
      <Link href={APP_HOME_ROUTES.admin}>
        <HugeiconsIcon icon={ShieldUserIcon} className="size-4" />
        Admin
      </Link>
    </Button>
  )
}
