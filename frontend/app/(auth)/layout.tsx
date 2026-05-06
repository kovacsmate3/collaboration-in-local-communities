import Link from "next/link"
import { HugeiconsIcon } from "@hugeicons/react"
import { BriefcaseIcon } from "@hugeicons/core-free-icons"

import { APP_NAME } from "@/lib/constants"

/**
 * Layout for `/login` and `/register`.
 *
 * Uses the shadcn login-03/signup-03 shell: muted background, centered
 * brand mark, and a compact form surface.
 */
export default function AuthLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <main className="flex min-h-svh flex-col items-center justify-center gap-6 bg-muted p-6 md:p-10">
      <div className="flex w-full max-w-sm flex-col gap-6">
        <Link
          href="/"
          className="flex items-center gap-2 self-center font-medium"
        >
          <span className="flex size-6 items-center justify-center rounded-md bg-primary text-primary-foreground">
            <HugeiconsIcon
              icon={BriefcaseIcon}
              className="size-4"
              strokeWidth={2}
            />
          </span>
          <span className="text-sm font-semibold tracking-tight">
            {APP_NAME}
          </span>
        </Link>
        {children}
      </div>
    </main>
  )
}
