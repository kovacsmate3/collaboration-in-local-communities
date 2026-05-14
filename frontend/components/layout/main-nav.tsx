"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"
import { HugeiconsIcon } from "@hugeicons/react"

import { PRIMARY_NAV } from "@/lib/nav"
import { cn } from "@/lib/utils"

interface MainNavProps {
  className?: string
}

/**
 * Top navigation for desktop. Mobile users see `MobileNav` (bottom bar).
 *
 * Active state is derived from the current pathname using each item's
 * `matches` predicate, falling back to an exact-prefix match.
 */
export function MainNav({ className }: MainNavProps) {
  const pathname = usePathname()

  return (
    <nav className={cn("flex items-center gap-1", className)}>
      {PRIMARY_NAV.map((item) => {
        const active = item.matches
          ? item.matches(pathname)
          : pathname === item.href
        return (
          <Link
            key={item.href}
            href={item.href}
            aria-current={active ? "page" : undefined}
            className={cn(
              "flex items-center gap-2 rounded-md px-3 py-1.5 text-sm font-medium transition-colors",
              active
                ? "bg-muted text-foreground"
                : "text-muted-foreground hover:bg-muted/60 hover:text-foreground"
            )}
          >
            <HugeiconsIcon icon={item.icon} className="size-4" />
            {item.label}
          </Link>
        )
      })}
    </nav>
  )
}
