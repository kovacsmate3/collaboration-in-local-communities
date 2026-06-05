"use client"

import Link from "next/link"
import { HugeiconsIcon } from "@hugeicons/react"
import {
  Logout01Icon,
  Settings02Icon,
  UserCircleIcon,
} from "@hugeicons/core-free-icons"
import { useTranslations } from "next-intl"

import { LanguageSubmenu } from "@/components/layout/language-submenu"
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
  const t = useTranslations("nav")

  return (
    <>
      <DropdownMenuItem asChild>
        <Link href={profileHref}>
          <HugeiconsIcon icon={UserCircleIcon} className="size-4" />
          {t("profile")}
        </Link>
      </DropdownMenuItem>
      <DropdownMenuItem asChild>
        <Link href={settingsHref}>
          <HugeiconsIcon icon={Settings02Icon} className="size-4" />
          {t("settings")}
        </Link>
      </DropdownMenuItem>
      <LanguageSubmenu />
      <DropdownMenuSeparator />
      <DropdownMenuItem
        variant="destructive"
        onSelect={(event) => {
          event.preventDefault()
          onLogout()
        }}
      >
        <HugeiconsIcon icon={Logout01Icon} className="size-4" />
        {t("logout")}
      </DropdownMenuItem>
    </>
  )
}
