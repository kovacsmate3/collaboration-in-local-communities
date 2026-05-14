import Link from "next/link"

import { Button } from "@/components/ui/button"

export default function NotFound() {
  return (
    <main className="flex min-h-svh items-center justify-center px-6">
      <div className="flex max-w-md flex-col items-center gap-4 text-center">
        <p className="font-mono text-xs tracking-[0.25em] text-muted-foreground uppercase">
          404
        </p>
        <h1 className="text-2xl font-semibold tracking-tight">
          We couldn&apos;t find that page
        </h1>
        <p className="text-sm text-muted-foreground">
          The link may be broken or the page may have moved.
        </p>
        <Button asChild>
          <Link href="/feed">Back to the feed</Link>
        </Button>
      </div>
    </main>
  )
}
