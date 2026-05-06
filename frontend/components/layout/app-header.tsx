import Link from "next/link"

import { AdminHeaderLink } from "@/components/layout/admin-header-link"
import { Button } from "@/components/ui/button"
import { MainNav } from "@/components/layout/main-nav"
import { UserMenu } from "@/components/layout/user-menu"
import { APP_NAME } from "@/lib/constants"

/**
 * Header shown on every authenticated page.
 *
 * The mobile bottom bar lives in a separate component (`MobileNav`)
 * rendered from the same layout - this header focuses on desktop.
 *
 * Admins also see an "Admin" shortcut (rendered via the small client
 * component <AdminHeaderLink />) so they can jump into /admin from
 * anywhere on the user-facing site. This is the unified admin UX: admins
 * remain full participants on the main site rather than being walled off
 * in the back office.
 */
export function AppHeader() {
  return (
    <header className="sticky top-0 z-20 border-b border-border bg-background/85 backdrop-blur">
      <div className="mx-auto flex h-14 max-w-6xl items-center gap-4 px-4 sm:px-6">
        <Link href="/feed" className="text-base font-semibold tracking-tight">
          {APP_NAME}
        </Link>

        <MainNav className="hidden md:flex" />

        <div className="ml-auto flex items-center gap-2">
          <AdminHeaderLink />
          <Button asChild size="sm" className="hidden sm:inline-flex">
            <Link href="/post-task">Post a task</Link>
          </Button>
          <UserMenu />
        </div>
      </div>
    </header>
  )
}
