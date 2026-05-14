import Link from "next/link"

import { Button } from "@/components/ui/button"
import { APP_NAME, APP_TAGLINE } from "@/lib/constants"

/**
 * Public landing page. Kept lightweight on purpose: the design choice
 * is "editorial restraint" - large heading, short copy, two CTAs.
 *
 * Marketing-grade content (sections, screenshots, etc.) can be added
 * later without affecting the rest of the app.
 */
export default function HomePage() {
  return (
    <main className="flex min-h-svh flex-col">
      <header className="flex items-center justify-between border-b border-border px-6 py-4">
        <span className="text-base font-semibold tracking-tight">
          {APP_NAME}
        </span>
        <nav className="flex items-center gap-2">
          <Button asChild variant="ghost" size="sm">
            <Link href="/login">Sign in</Link>
          </Button>
          <Button asChild size="sm">
            <Link href="/register">Get started</Link>
          </Button>
        </nav>
      </header>

      <section className="flex flex-1 items-center px-6 py-16">
        <div className="mx-auto flex w-full max-w-3xl flex-col gap-6 text-center">
          <p className="text-sm font-medium tracking-[0.2em] text-muted-foreground uppercase">
            {APP_TAGLINE}
          </p>
          <h1 className="text-4xl leading-tight font-semibold tracking-tight text-balance sm:text-5xl">
            Real help from real neighbours, without the noisy group chats.
          </h1>
          <p className="mx-auto max-w-xl text-base text-balance text-muted-foreground">
            Post a small task or lend a hand nearby. Reputation, transparent
            compensation, and verified profiles - structured trust for everyday
            micro-cooperation.
          </p>
          <div className="mx-auto flex flex-wrap items-center justify-center gap-2">
            <Button asChild size="lg">
              <Link href="/register">Create an account</Link>
            </Button>
            <Button asChild variant="outline" size="lg">
              <Link href="/feed">Browse the feed</Link>
            </Button>
          </div>
        </div>
      </section>
    </main>
  )
}
