import {
  BubbleChatIcon,
  Compass01Icon,
  TaskDaily01Icon,
} from "@hugeicons/core-free-icons"

import type { IconSvgElement } from "@hugeicons/react"

/**
 * Translation key under the `nav` namespace. Nav items declare the key
 * instead of a literal label so consumers (MainNav, MobileNav) resolve
 * the active locale at render time without re-deriving the array.
 */
export type NavLabelKey = "feed" | "myTasks" | "messages"

export interface NavItem {
  href: string
  labelKey: NavLabelKey
  icon: IconSvgElement
  /** Used by sub-routes that should still highlight the parent. */
  matches?: (pathname: string) => boolean
}

/**
 * The primary navigation surface for authenticated users.
 *
 * Order matters: this drives both the desktop top nav and the mobile bottom bar.
 */
export const PRIMARY_NAV: NavItem[] = [
  {
    href: "/feed",
    labelKey: "feed",
    icon: Compass01Icon,
    matches: (p) => p === "/feed" || p.startsWith("/feed/"),
  },
  {
    href: "/tasks",
    labelKey: "myTasks",
    icon: TaskDaily01Icon,
    matches: (p) => p.startsWith("/tasks"),
  },
  {
    href: "/messages",
    labelKey: "messages",
    icon: BubbleChatIcon,
    matches: (p) => p.startsWith("/messages"),
  },
]
