import Link from "next/link"
import { ArrowRight01Icon } from "@hugeicons/core-free-icons"
import { HugeiconsIcon } from "@hugeicons/react"

import { ADMIN_PAGE_META } from "@/components/admin/nav"

interface AdminBreadcrumbProps {
  pathname: string
}

export function AdminBreadcrumb({ pathname }: AdminBreadcrumbProps) {
  const meta = ADMIN_PAGE_META[pathname]

  if (!meta) {
    return <span className="text-sm font-medium">Admin</span>
  }

  if (!meta.parent) {
    return <span className="text-sm font-medium">{meta.label}</span>
  }

  const parentMeta = ADMIN_PAGE_META[meta.parent]

  return (
    <nav aria-label="Breadcrumb" className="flex items-center gap-1.5 text-sm">
      <Link
        href={meta.parent}
        className="text-muted-foreground transition-colors hover:text-foreground"
      >
        {parentMeta?.label ?? "Admin"}
      </Link>
      <HugeiconsIcon
        icon={ArrowRight01Icon}
        className="size-3.5 text-muted-foreground"
        strokeWidth={1.5}
      />
      <span className="font-medium">{meta.label}</span>
    </nav>
  )
}
