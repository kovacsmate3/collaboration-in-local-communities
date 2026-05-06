"use client"

import Link from "next/link"
import { useRouter } from "next/navigation"
import { HugeiconsIcon } from "@hugeicons/react"
import {
  Logout01Icon,
  Settings02Icon,
  UserCircleIcon,
} from "@hugeicons/core-free-icons"
import { toast } from "sonner"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
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
  const { user, logout } = useAuth()
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
    <DropdownMenu>
      <DropdownMenuTrigger
        aria-label="Open user menu"
        className="rounded-full outline-none focus-visible:ring-3 focus-visible:ring-ring/50"
      >
        <UserAvatar name={user.name} src={user.avatarUrl} />
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuLabel>{user.name}</DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem asChild>
          <Link href="/profile">
            <HugeiconsIcon icon={UserCircleIcon} className="size-4" />
            Profile
          </Link>
        </DropdownMenuItem>
        <DropdownMenuItem asChild>
          <Link href="/profile/edit">
            <HugeiconsIcon icon={Settings02Icon} className="size-4" />
            Settings
          </Link>
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          variant="destructive"
          onSelect={(event) => {
            event.preventDefault()
            void handleLogout()
          }}
        >
          <HugeiconsIcon icon={Logout01Icon} className="size-4" />
          Log out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
