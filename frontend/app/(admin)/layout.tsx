import { AdminGuard } from "@/components/admin/admin-guard"
import { AdminDesktopSidebar } from "@/components/admin/admin-sidebar"
import { AdminHeader } from "@/components/admin/admin-header"

/**
 * Layout for all /admin/* routes.
 *
 * Completely separate from the (main) layout — no AppHeader, no MobileNav,
 * no user-facing chrome. Uses the dashboard-01 pattern: fixed sidebar on
 * desktop, slide-in drawer on mobile.
 *
 * All child routes are guarded by AdminGuard, which redirects non-admin
 * users to /feed.
 */
export default function AdminLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <AdminGuard>
      <div className="flex h-svh overflow-hidden">
        <AdminDesktopSidebar />
        <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
          <AdminHeader />
          <main className="flex-1 overflow-y-auto">
            <div className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
              {children}
            </div>
          </main>
        </div>
      </div>
    </AdminGuard>
  )
}
