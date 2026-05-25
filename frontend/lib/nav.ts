import {
  BubbleChatIcon,
  Compass01Icon,
  TaskDaily01Icon,
} from "@hugeicons/core-free-icons"

import type { IconSvgElement } from "@hugeicons/react"

export interface NavItem {
  href: string
  label: string
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
    label: "Feed",
    icon: Compass01Icon,
    matches: (p) => p === "/feed" || p.startsWith("/feed/"),
  },
  {
    href: "/tasks",
    label: "My tasks",
    icon: TaskDaily01Icon,
    matches: (p) => p.startsWith("/tasks"),
  },
  {
    href: "/messages",
    label: "Messages",
    icon: BubbleChatIcon,
    matches: (p) => p.startsWith("/messages"),
  },
]
