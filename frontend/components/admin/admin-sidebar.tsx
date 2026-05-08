"use client"

import { Menu01Icon } from "@hugeicons/core-free-icons"
import { HugeiconsIcon } from "@hugeicons/react"

import { AdminSidebarContent } from "@/components/admin/admin-sidebar-content"
import { Button } from "@/components/ui/button"
import { Sheet, SheetContent, SheetTitle } from "@/components/ui/sheet"

export function AdminDesktopSidebar() {
  return (
    <aside className="hidden w-60 shrink-0 border-r bg-card lg:flex lg:flex-col">
      <AdminSidebarContent />
    </aside>
  )
}

interface AdminMobileSidebarProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function AdminMobileSidebar({
  open,
  onOpenChange,
}: AdminMobileSidebarProps) {
  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent
        side="left"
        className="w-60 p-0"
        aria-describedby={undefined}
      >
        <SheetTitle className="sr-only">Navigation</SheetTitle>
        <AdminSidebarContent onNavigate={() => onOpenChange(false)} />
      </SheetContent>
    </Sheet>
  )
}

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
