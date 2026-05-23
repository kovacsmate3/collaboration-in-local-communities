"use client"

import Link from "next/link"
import { useRouter } from "next/navigation"
import { toast } from "sonner"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { UserAccountMenuItems } from "@/components/layout/user-account-menu-items"
import { UserAvatar } from "@/components/shared/user-avatar"
import { useAuth } from "@/lib/auth-context"
import { APP_AUTH_ROUTES } from "@/lib/auth/constants"

/**
 * Header drop-down with quick links to profile, settings and logout.
 *
 * Accepts only the user fields it actually displays so we don't tightly
 * couple it to the full `User` shape.
 */
export function UserMenu() {
  const { user, logout, isAdmin } = useAuth()
  const router = useRouter()

  if (!user) {
    return (
      <Button asChild size="sm" variant="outline">
        <Link href={APP_AUTH_ROUTES.login}>Sign in</Link>
      </Button>
    )
  }

  async function handleLogout() {
    await logout()
    toast.success("Signed out")
    router.replace(APP_AUTH_ROUTES.login)
    router.refresh()
  }

  return (
    <DropdownMenu modal={false}>
      <DropdownMenuTrigger
        aria-label="Open user menu"
        className="rounded-full outline-none focus-visible:ring-3 focus-visible:ring-ring/50"
      >
        <UserAvatar name={user.name} src={user.avatarUrl} />
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuLabel>{user.name}</DropdownMenuLabel>
        <DropdownMenuSeparator />
        <UserAccountMenuItems
          onLogout={() => void handleLogout()}
          showAdminLink={isAdmin}
        />
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
