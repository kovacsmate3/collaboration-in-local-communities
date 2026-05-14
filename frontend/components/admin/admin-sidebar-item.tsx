"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"
import { HugeiconsIcon } from "@hugeicons/react"

import { Badge } from "@/components/ui/badge"
import { cn } from "@/lib/utils"
import { isAdminNavItemActive, type AdminNavItem } from "@/components/admin/nav"

interface AdminSidebarItemProps {
  item: AdminNavItem
  onNavigate?: () => void
}

export function AdminSidebarItem({ item, onNavigate }: AdminSidebarItemProps) {
  const pathname = usePathname()
  const isActive = isAdminNavItemActive(item, pathname)

  const content = (
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
      {item.badge ? (
        <Badge variant="secondary" className="h-4 px-1.5 py-0 text-[10px]">
          {item.badge}
        </Badge>
      ) : null}
    </span>
  )

  if (item.disabled) {
    return content
  }

  return (
    <Link href={item.href} onClick={onNavigate}>
      {content}
    </Link>
  )
}
