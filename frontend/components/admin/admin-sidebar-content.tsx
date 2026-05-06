import { BriefcaseIcon } from "@hugeicons/core-free-icons"
import { HugeiconsIcon } from "@hugeicons/react"

import { AdminSidebarItem } from "@/components/admin/admin-sidebar-item"
import { AdminSidebarUserProfile } from "@/components/admin/admin-sidebar-user-profile"
import { ADMIN_NAV_SECTIONS } from "@/components/admin/nav"
import { Separator } from "@/components/ui/separator"

interface AdminSidebarContentProps {
  onNavigate?: () => void
}

export function AdminSidebarContent({ onNavigate }: AdminSidebarContentProps) {
  return (
    <div className="flex h-full flex-col">
      <div className="flex items-center gap-2 px-4 py-5">
        <div className="flex h-8 w-8 items-center justify-center rounded-md bg-primary">
          <HugeiconsIcon
            icon={BriefcaseIcon}
            className="size-4 text-primary-foreground"
            strokeWidth={2}
          />
        </div>
        <div className="flex flex-col leading-none">
          <span className="text-sm font-semibold">Admin Console</span>
          <span className="text-[11px] text-muted-foreground">Management</span>
        </div>
      </div>

      <Separator />

      <nav className="flex-1 overflow-y-auto px-3 py-4">
        <div className="space-y-6">
          {ADMIN_NAV_SECTIONS.map((section) => (
            <div key={section.title}>
              <p className="mb-1.5 px-3 text-[11px] font-semibold tracking-wider text-muted-foreground uppercase">
                {section.title}
              </p>
              <div className="space-y-0.5">
                {section.items.map((item) => (
                  <AdminSidebarItem
                    key={item.href}
                    item={item}
                    onNavigate={onNavigate}
                  />
                ))}
              </div>
            </div>
          ))}
        </div>
      </nav>

      <Separator />

      <div className="p-3">
        <AdminSidebarUserProfile />
      </div>
    </div>
  )
}
