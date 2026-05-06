"use client"

import Link from "next/link"
import { useRouter } from "next/navigation"
import { HugeiconsIcon } from "@hugeicons/react"
import {
  Logout01Icon,
  MoreHorizontalCircle01Icon,
  UserCircle02Icon,
} from "@hugeicons/core-free-icons"
import { toast } from "sonner"

import { UserAvatar } from "@/components/shared/user-avatar"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { APP_AUTH_ROUTES } from "@/lib/auth/constants"
import { useAuth } from "@/lib/auth-context"
import { cn } from "@/lib/utils"

export function AdminSidebarUserProfile() {
  const { user, logout } = useAuth()
  const router = useRouter()

  if (!user) {
    return null
  }

  async function handleLogout() {
    await logout()
    toast.success("Signed out")
    router.replace(APP_AUTH_ROUTES.login)
    router.refresh()
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          className={cn(
            "flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm",
            "text-muted-foreground transition-colors hover:bg-muted hover:text-foreground",
            "focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
          )}
        >
          <UserAvatar name={user.name} src={user.avatarUrl} size="sm" />
          <div className="flex min-w-0 flex-1 flex-col items-start leading-none">
            <span className="truncate font-medium text-foreground">
              {user.name}
            </span>
            <span className="truncate text-[11px] text-muted-foreground">
              {user.email}
            </span>
          </div>
          <HugeiconsIcon
            icon={MoreHorizontalCircle01Icon}
            className="size-4 shrink-0 text-muted-foreground"
            strokeWidth={1.5}
          />
        </button>
      </DropdownMenuTrigger>

      <DropdownMenuContent
        side="top"
        align="start"
        sideOffset={8}
        className="w-56"
      >
        <DropdownMenuLabel className="font-normal">
          <div className="flex flex-col gap-0.5">
            <span className="truncate text-sm font-medium text-foreground">
              {user.name}
            </span>
            <span className="truncate text-xs text-muted-foreground">
              {user.email}
            </span>
          </div>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuGroup>
          <DropdownMenuItem asChild>
            <Link href="/admin/profile">
              <HugeiconsIcon
                icon={UserCircle02Icon}
                className="size-4"
                strokeWidth={1.5}
              />
              Account
            </Link>
          </DropdownMenuItem>
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          variant="destructive"
          onSelect={(event) => {
            event.preventDefault()
            void handleLogout()
          }}
        >
          <HugeiconsIcon
            icon={Logout01Icon}
            className="size-4"
            strokeWidth={1.5}
          />
          Log out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
