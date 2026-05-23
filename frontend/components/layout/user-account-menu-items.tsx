"use client"

import Link from "next/link"
import { HugeiconsIcon } from "@hugeicons/react"
import {
  Logout01Icon,
  Settings02Icon,
  UserCircleIcon,
} from "@hugeicons/core-free-icons"

import {
  DropdownMenuItem,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu"

interface UserAccountMenuItemsProps {
  onLogout: () => void
  profileHref?: string
  settingsHref?: string
}

export function UserAccountMenuItems({
  onLogout,
  profileHref = "/profile",
  settingsHref = "/profile/edit",
}: UserAccountMenuItemsProps) {
  return (
    <>
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
