import Link from "next/link"

import { AdminHeaderLink } from "@/components/layout/admin-header-link"
import { LanguageSwitcher } from "@/components/layout/language-switcher"
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
      <div className="mx-auto grid h-14 max-w-6xl grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] items-center gap-4 px-4 sm:px-6">
        <Link
          href="/feed"
          className="col-start-1 row-start-1 justify-self-start text-base font-semibold tracking-tight"
        >
          {APP_NAME}
        </Link>

        <MainNav className="col-start-2 row-start-1 hidden md:flex" />

        <div className="col-start-3 row-start-1 flex items-center gap-2 justify-self-end">
          <AdminHeaderLink />
          <LanguageSwitcher />
          <UserMenu />
        </div>
      </div>
    </header>
  )
}
