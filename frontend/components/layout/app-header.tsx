import Link from "next/link"

import { Button } from "@/components/ui/button"
import { MainNav } from "@/components/layout/main-nav"
import { UserMenu } from "@/components/layout/user-menu"
import { APP_NAME } from "@/lib/constants"
import { currentUser } from "@/lib/mock-data"

/**
 * Header shown on every authenticated page.
 *
 * The mobile bottom bar lives in a separate component (`MobileNav`)
 * rendered from the same layout - this header focuses on desktop.
 */
export function AppHeader() {
  return (
    <header className="sticky top-0 z-20 border-b border-border bg-background/85 backdrop-blur">
      <div className="mx-auto flex h-14 max-w-6xl items-center gap-4 px-4 sm:px-6">
        <Link
          href="/feed"
          className="text-base font-semibold tracking-tight"
        >
          {APP_NAME}
        </Link>

        <MainNav className="hidden md:flex" />

        <div className="ml-auto flex items-center gap-2">
          <Button asChild size="sm" className="hidden sm:inline-flex">
            <Link href="/post-task">Post a task</Link>
          </Button>
          {/* TODO: replace `currentUser` with the authenticated user from session */}
          <UserMenu user={currentUser} />
        </div>
      </div>
    </header>
  )
}
