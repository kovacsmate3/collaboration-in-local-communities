import {
  Analytics01Icon,
  ArchiveIcon,
  BookOpen01Icon,
  FlipBottomIcon,
  ShieldUserIcon,
  Tag01Icon,
  UserMultiple02Icon,
} from "@hugeicons/core-free-icons"
import type { IconSvgElement } from "@hugeicons/react"

export interface AdminNavItem {
  href: string
  label: string
  icon: IconSvgElement
  badge?: string
  disabled?: boolean
  matches?: (pathname: string) => boolean
}

export interface AdminNavSection {
  title: string
  items: AdminNavItem[]
}

export interface AdminPageMeta {
  label: string
  parent?: string
}

export const ADMIN_NAV_SECTIONS: AdminNavSection[] = [
  {
    title: "Overview",
    items: [
      {
        href: "/admin",
        label: "Dashboard",
        icon: Analytics01Icon,
        matches: (pathname) => pathname === "/admin",
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
        matches: (pathname) => pathname.startsWith("/admin/categories"),
      },
      {
        href: "/admin/skills",
        label: "Skills",
        icon: FlipBottomIcon,
        badge: "Soon",
        disabled: true,
        matches: (pathname) => pathname.startsWith("/admin/skills"),
      },
      {
        href: "/admin/users",
        label: "Users",
        icon: UserMultiple02Icon,
        badge: "Soon",
        disabled: true,
        matches: (pathname) => pathname.startsWith("/admin/users"),
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
        matches: (pathname) => pathname.startsWith("/admin/reports"),
      },
      {
        href: "/admin/audit-log",
        label: "Audit Log",
        icon: ArchiveIcon,
        badge: "Soon",
        disabled: true,
        matches: (pathname) => pathname.startsWith("/admin/audit-log"),
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
        matches: (pathname) => pathname.startsWith("/admin/data-library"),
      },
    ],
  },
]

export const ADMIN_PAGE_META: Record<string, AdminPageMeta> = {
  "/admin": { label: "Dashboard" },
  "/admin/categories": { label: "Categories", parent: "/admin" },
  "/admin/skills": { label: "Skills", parent: "/admin" },
  "/admin/users": { label: "Users", parent: "/admin" },
  "/admin/reports": { label: "Reports", parent: "/admin" },
  "/admin/data-library": { label: "Data Library", parent: "/admin" },
  "/admin/audit-log": { label: "Audit Log", parent: "/admin" },
  "/admin/profile": { label: "My Account", parent: "/admin" },
}

export function isAdminNavItemActive(
  item: AdminNavItem,
  pathname: string
): boolean {
  return item.matches ? item.matches(pathname) : pathname === item.href
}
