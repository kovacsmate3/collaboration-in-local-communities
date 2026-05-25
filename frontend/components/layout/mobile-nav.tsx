"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"
import { HugeiconsIcon } from "@hugeicons/react"

import { PRIMARY_NAV } from "@/lib/nav"
import { useUnreadCount } from "@/lib/api/conversations"
import { cn } from "@/lib/utils"

/**
 * Bottom-tab navigation for mobile. Mirrors `MainNav` but uses an
 * iconography-first layout sized for thumb reach.
 */
export function MobileNav() {
  const pathname = usePathname()
  const unreadCount = useUnreadCount()

  return (
    <nav
      aria-label="Primary"
      className="fixed inset-x-0 bottom-0 z-30 border-t border-border bg-background/90 backdrop-blur md:hidden"
    >
      <ul className="grid grid-cols-3">
        {PRIMARY_NAV.map((item) => {
          const active = item.matches
            ? item.matches(pathname)
            : pathname === item.href
          return (
            <li key={item.href}>
              <Link
                href={item.href}
                aria-current={active ? "page" : undefined}
                aria-label={
                  item.href === "/messages" && unreadCount > 0
                    ? `${item.label}, ${unreadCount} unread`
                    : undefined
                }
                className={cn(
                  "flex flex-col items-center gap-1 px-2 py-2 text-[0.65rem] font-medium transition-colors",
                  active
                    ? "text-foreground"
                    : "text-muted-foreground hover:text-foreground"
                )}
              >
                <span className="relative">
                  <HugeiconsIcon icon={item.icon} className="size-5" />
                  {item.href === "/messages" && unreadCount > 0 && (
                    <span className="absolute -top-1 -right-1 h-2 w-2 rounded-full bg-destructive" />
                  )}
                </span>
                <span className="truncate">{item.label.split(" ")[0]}</span>
              </Link>
            </li>
          )
        })}
      </ul>
    </nav>
  )
}
