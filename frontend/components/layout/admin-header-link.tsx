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
 * On mobile the button collapses to an icon-only square so it still fits
 * next to the user avatar, giving admins a tap target to /admin from any
 * page on the main site at every screen size. The "Admin" label appears
 * once there's room from the `sm` breakpoint up.
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
      aria-label="Admin console"
      className="size-9 gap-0 px-0 sm:size-auto sm:gap-2 sm:px-3"
    >
      <Link href={APP_HOME_ROUTES.admin}>
        <HugeiconsIcon icon={ShieldUserIcon} className="size-4" />
        <span className="hidden sm:inline">Admin</span>
      </Link>
    </Button>
  )
}
