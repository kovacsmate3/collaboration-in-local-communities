"use client"

import * as React from "react"
import Link from "next/link"
import { usePathname } from "next/navigation"
import { HugeiconsIcon } from "@hugeicons/react"
import { ArrowRight01Icon } from "@hugeicons/core-free-icons"

import { Separator } from "@/components/ui/separator"
import { AdminMobileMenuButton, AdminMobileSidebar } from "./admin-sidebar"

// Page title map  →  href : { label, parent? }
const PAGE_META: Record<string, { label: string; parent?: string }> = {
  "/admin": { label: "Dashboard" },
  "/admin/categories": { label: "Categories", parent: "/admin" },
  "/admin/skills": { label: "Skills", parent: "/admin" },
  "/admin/users": { label: "Users", parent: "/admin" },
  "/admin/reports": { label: "Reports", parent: "/admin" },
  "/admin/data-library": { label: "Data Library", parent: "/admin" },
  "/admin/audit-log": { label: "Audit Log", parent: "/admin" },
  "/admin/profile": { label: "My Account", parent: "/admin" },
}

function Breadcrumb({ pathname }: { pathname: string }) {
  const meta = PAGE_META[pathname]
  if (!meta) return <span className="text-sm font-medium">Admin</span>

  if (!meta.parent) {
    return <span className="text-sm font-medium">{meta.label}</span>
  }

  const parentMeta = PAGE_META[meta.parent]

  return (
    <nav aria-label="Breadcrumb" className="flex items-center gap-1.5 text-sm">
      <Link
        href={meta.parent}
        className="text-muted-foreground hover:text-foreground transition-colors"
      >
        {parentMeta?.label ?? "Admin"}
      </Link>
      <HugeiconsIcon icon={ArrowRight01Icon} className="size-3.5 text-muted-foreground" strokeWidth={1.5} />
      <span className="font-medium">{meta.label}</span>
    </nav>
  )
}

export function AdminHeader() {
  const pathname = usePathname()
  const [mobileSidebarOpen, setMobileSidebarOpen] = React.useState(false)

  return (
    <>
      <header className="sticky top-0 z-40 flex h-14 items-center gap-3 border-b bg-background/95 px-4 backdrop-blur supports-backdrop-filter:bg-background/60 lg:px-6">
        <AdminMobileMenuButton onClick={() => setMobileSidebarOpen(true)} />
        <Separator orientation="vertical" className="h-5 lg:hidden" />
        <Breadcrumb pathname={pathname} />
      </header>
      <AdminMobileSidebar
        open={mobileSidebarOpen}
        onOpenChange={setMobileSidebarOpen}
      />
    </>
  )
}
