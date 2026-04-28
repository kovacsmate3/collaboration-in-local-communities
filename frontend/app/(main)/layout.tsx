import { AppHeader } from "@/components/layout/app-header"
import { MobileNav } from "@/components/layout/mobile-nav"

/**
 * Layout for every authenticated route. Renders the persistent header
 * (top), the page content, and a mobile bottom nav.
 *
 * The bottom nav adds 4rem of padding to the page bottom so content is
 * never hidden behind it on small screens.
 */
export default function MainLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <div className="flex min-h-svh flex-col">
      <AppHeader />
      <main className="flex-1 pb-20 md:pb-10">
        <div className="mx-auto w-full max-w-6xl px-4 py-6 sm:px-6">
          {children}
        </div>
      </main>
      <MobileNav />
    </div>
  )
}
