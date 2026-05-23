"use client"

import Link from "next/link"
import { HugeiconsIcon } from "@hugeicons/react"
import {
  Logout01Icon,
  Settings02Icon,
  ShieldUserIcon,
  UserCircleIcon,
} from "@hugeicons/core-free-icons"

import {
  DropdownMenuItem,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu"
import { APP_HOME_ROUTES } from "@/lib/auth/constants"

interface UserAccountMenuItemsProps {
  onLogout: () => void
  profileHref?: string
  settingsHref?: string
  /**
   * When true, renders an "Admin console" entry above the profile/settings
   * group. Acts as the always-available mobile entry point into /admin (the
   * header-bar shortcut may be too cramped to surface on the smallest
   * viewports). Hidden for regular users.
   */
  showAdminLink?: boolean
  adminHref?: string
}

export function UserAccountMenuItems({
  onLogout,
  profileHref = "/profile",
  settingsHref = "/profile/edit",
  showAdminLink = false,
  adminHref = APP_HOME_ROUTES.admin,
}: UserAccountMenuItemsProps) {
  return (
    <>
      {showAdminLink && (
        <>
          <DropdownMenuItem asChild>
            <Link href={adminHref}>
              <HugeiconsIcon icon={ShieldUserIcon} className="size-4" />
              Admin console
            </Link>
          </DropdownMenuItem>
          <DropdownMenuSeparator />
        </>
      )}
      <DropdownMenuItem asChild>
        <Link href={profileHref}>
          <HugeiconsIcon icon={UserCircleIcon} className="size-4" />
          Profile
        </Link>
      </DropdownMenuItem>
      <DropdownMenuItem asChild>
        <Link href={settingsHref}>
          <HugeiconsIcon icon={Settings02Icon} className="size-4" />
          Settings
        </Link>
      </DropdownMenuItem>
      <DropdownMenuSeparator />
      <DropdownMenuItem
        variant="destructive"
        onSelect={(event) => {
          event.preventDefault()
          onLogout()
        }}
      >
        <HugeiconsIcon icon={Logout01Icon} className="size-4" />
        Log out
      </DropdownMenuItem>
    </>
  )
}
