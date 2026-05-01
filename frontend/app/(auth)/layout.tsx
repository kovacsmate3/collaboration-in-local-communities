import Link from "next/link"

import { APP_NAME, APP_TAGLINE } from "@/lib/constants"

/**
 * Layout for `/login` and `/register`.
 *
 * Kept minimal on purpose so the auth screens stay focused. Marketing
 * copy lives in the right column on `md+`, the form takes the full
 * width on small screens.
 */
export default function AuthLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <div className="grid min-h-svh md:grid-cols-2">
      <main className="flex items-center justify-center p-6 sm:p-10">
        <div className="flex w-full max-w-sm flex-col gap-6">
          <Link href="/" className="text-base font-semibold tracking-tight">
            {APP_NAME}
          </Link>
          {children}
        </div>
      </main>

      <aside className="hidden flex-col justify-end gap-2 bg-muted p-10 md:flex">
        <p className="max-w-md text-3xl leading-tight font-semibold tracking-tight text-foreground">
          {APP_TAGLINE}
        </p>
        <p className="max-w-md text-sm text-muted-foreground">
          Find a hand or lend one. Reputation, transparency, and real neighbours
          - replacing scattered group chats with structured micro-cooperation.
        </p>
      </aside>
    </div>
  )
}
