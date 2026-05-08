"use client"

import * as React from "react"
import Link from "next/link"
import { usePathname } from "next/navigation"
import { HugeiconsIcon } from "@hugeicons/react"
import { ArrowLeft01Icon } from "@hugeicons/core-free-icons"

import { AdminBreadcrumb } from "@/components/admin/admin-breadcrumb"
import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import { APP_HOME_ROUTES } from "@/lib/auth/constants"
import { AdminMobileMenuButton, AdminMobileSidebar } from "./admin-sidebar"

export function AdminHeader() {
  const pathname = usePathname()
  const [mobileSidebarOpen, setMobileSidebarOpen] = React.useState(false)

  return (
    <>
      <header className="sticky top-0 z-40 flex h-14 items-center gap-3 border-b bg-background/95 px-4 backdrop-blur supports-backdrop-filter:bg-background/60 lg:px-6">
        <AdminMobileMenuButton onClick={() => setMobileSidebarOpen(true)} />
        <Separator orientation="vertical" className="h-5 lg:hidden" />
        <AdminBreadcrumb pathname={pathname} />

        {/* Back-to-site link: unified admin UX. Admins are full users of the
            platform too, so /admin is a context they enter and leave at will
            rather than a sealed-off back office. */}
        <Button
          asChild
          size="sm"
          variant="ghost"
          className="ml-auto gap-2 text-muted-foreground hover:text-foreground"
        >
          <Link href={APP_HOME_ROUTES.user}>
            <HugeiconsIcon icon={ArrowLeft01Icon} className="size-4" />
            <span className="hidden sm:inline">Back to site</span>
          </Link>
        </Button>
      </header>
      <AdminMobileSidebar
        open={mobileSidebarOpen}
        onOpenChange={setMobileSidebarOpen}
      />
    </>
  )
}
