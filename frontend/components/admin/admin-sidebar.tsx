"use client"

import * as React from "react"
import Link from "next/link"
import { usePathname } from "next/navigation"
import { HugeiconsIcon } from "@hugeicons/react"
import {
  Analytics01Icon,
  Tag01Icon,
  UserMultiple02Icon,
  ShieldUserIcon,
  BookOpen01Icon,
  ArchiveIcon,
  FlipBottomIcon,
  Menu01Icon,
  BriefcaseIcon,
  MoreHorizontalCircle01Icon,
  UserCircle02Icon,
  Logout01Icon,
} from "@hugeicons/core-free-icons"
import type { IconSvgElement } from "@hugeicons/react"

import { cn } from "@/lib/utils"
import { useAuth } from "@/lib/auth-context"
import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import { Sheet, SheetContent, SheetTitle } from "@/components/ui/sheet"
import { Badge } from "@/components/ui/badge"
import { Avatar, AvatarImage, AvatarFallback } from "@/components/ui/avatar"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"

// ── Nav definition ──────────────────────────────────────────────────────────

interface AdminNavItem {
  href: string
  label: string
  icon: IconSvgElement
  badge?: string
  disabled?: boolean
  matches?: (pathname: string) => boolean
}

interface AdminNavSection {
  title: string
  items: AdminNavItem[]
}

const NAV: AdminNavSection[] = [
  {
    title: "Overview",
    items: [
      {
        href: "/admin",
        label: "Dashboard",
        icon: Analytics01Icon,
        matches: (p) => p === "/admin",
      },
    ],
  },
  {
    title: "Management",
    items: [
      {
        href: "/admin/categories",
        label: "Categories",
        icon: Tag01Icon,
        matches: (p) => p.startsWith("/admin/categories"),
      },
      {
        href: "/admin/skills",
        label: "Skills",
        icon: FlipBottomIcon,
        badge: "Soon",
        disabled: true,
        matches: (p) => p.startsWith("/admin/skills"),
      },
      {
        href: "/admin/users",
        label: "Users",
        icon: UserMultiple02Icon,
        badge: "Soon",
        disabled: true,
        matches: (p) => p.startsWith("/admin/users"),
      },
    ],
  },
  {
    title: "Trust & Safety",
    items: [
      {
        href: "/admin/reports",
        label: "Reports",
        icon: ShieldUserIcon,
        badge: "Soon",
        disabled: true,
        matches: (p) => p.startsWith("/admin/reports"),
      },
      {
        href: "/admin/audit-log",
        label: "Audit Log",
        icon: ArchiveIcon,
        badge: "Soon",
        disabled: true,
        matches: (p) => p.startsWith("/admin/audit-log"),
      },
    ],
  },
  {
    title: "Configuration",
    items: [
      {
        href: "/admin/data-library",
        label: "Data Library",
        icon: BookOpen01Icon,
        badge: "Soon",
        disabled: true,
        matches: (p) => p.startsWith("/admin/data-library"),
      },
    ],
  },
]

// ── Sidebar item ────────────────────────────────────────────────────────────

function SidebarItem({
  item,
  onNavigate,
}: {
  item: AdminNavItem
  onNavigate?: () => void
}) {
  const pathname = usePathname()
  const isActive = item.matches
    ? item.matches(pathname)
    : pathname === item.href

  const inner = (
    <span
      className={cn(
        "group flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors",
        isActive
          ? "bg-primary/10 text-primary"
          : "text-muted-foreground hover:bg-muted hover:text-foreground",
        item.disabled && "pointer-events-none opacity-50"
      )}
    >
      <HugeiconsIcon
        icon={item.icon}
        className={cn(
          "size-4 shrink-0",
          isActive
            ? "text-primary"
            : "text-muted-foreground group-hover:text-foreground"
        )}
        strokeWidth={isActive ? 2 : 1.5}
      />
      <span className="flex-1 truncate">{item.label}</span>
      {item.badge && (
        <Badge variant="secondary" className="h-4 px-1.5 py-0 text-[10px]">
          {item.badge}
        </Badge>
      )}
    </span>
  )

  if (item.disabled) return inner

  return (
    <Link href={item.href} onClick={onNavigate}>
      {inner}
    </Link>
  )
}

// ── User profile block ───────────────────────────────────────────────────────

function SidebarUserProfile() {
  const { user, logout } = useAuth()

  if (!user) return null

  const initials = user.name
    .split(" ")
    .map((part) => part[0])
    .join("")
    .toUpperCase()
    .slice(0, 2)

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
          <Avatar size="sm">
            {user.avatarUrl && (
              <AvatarImage src={user.avatarUrl} alt={user.name} />
            )}
            <AvatarFallback>{initials}</AvatarFallback>
          </Avatar>

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

        <DropdownMenuItem variant="destructive" onSelect={() => logout()}>
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

// ── Sidebar body ────────────────────────────────────────────────────────────

function SidebarContent({ onNavigate }: { onNavigate?: () => void }) {
  return (
    <div className="flex h-full flex-col">
      {/* Logo / branding */}
      <div className="flex items-center gap-2 px-4 py-5">
        <div className="flex h-8 w-8 items-center justify-center rounded-md bg-primary">
          <HugeiconsIcon
            icon={BriefcaseIcon}
            className="size-4 text-primary-foreground"
            strokeWidth={2}
          />
        </div>
        <div className="flex flex-col leading-none">
          <span className="text-sm font-semibold">Admin Console</span>
          <span className="text-[11px] text-muted-foreground">Management</span>
        </div>
      </div>

      <Separator />

      {/* Navigation sections */}
      <nav className="flex-1 overflow-y-auto px-3 py-4">
        <div className="space-y-6">
          {NAV.map((section) => (
            <div key={section.title}>
              <p className="mb-1.5 px-3 text-[11px] font-semibold tracking-wider text-muted-foreground uppercase">
                {section.title}
              </p>
              <div className="space-y-0.5">
                {section.items.map((item) => (
                  <SidebarItem
                    key={item.href}
                    item={item}
                    onNavigate={onNavigate}
                  />
                ))}
              </div>
            </div>
          ))}
        </div>
      </nav>

      <Separator />

      {/* User profile */}
      <div className="p-3">
        <SidebarUserProfile />
      </div>
    </div>
  )
}

// ── Exported sidebar components ─────────────────────────────────────────────

/** Fixed desktop sidebar */
export function AdminDesktopSidebar() {
  return (
    <aside className="hidden w-60 shrink-0 border-r bg-card lg:flex lg:flex-col">
      <SidebarContent />
    </aside>
  )
}

/** Mobile slide-in sidebar via Sheet */
export function AdminMobileSidebar({
  open,
  onOpenChange,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
}) {
  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent
        side="left"
        className="w-60 p-0"
        aria-describedby={undefined}
      >
        <SheetTitle className="sr-only">Navigation</SheetTitle>
        <SidebarContent onNavigate={() => onOpenChange(false)} />
      </SheetContent>
    </Sheet>
  )
}

/** Mobile menu trigger button */
export function AdminMobileMenuButton({ onClick }: { onClick: () => void }) {
  return (
    <Button
      variant="ghost"
      size="icon"
      className="lg:hidden"
      onClick={onClick}
      aria-label="Open navigation menu"
    >
      <HugeiconsIcon icon={Menu01Icon} className="size-5" strokeWidth={1.5} />
    </Button>
  )
}
