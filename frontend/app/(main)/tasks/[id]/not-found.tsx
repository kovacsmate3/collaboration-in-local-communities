import Link from "next/link"

import { Button } from "@/components/ui/button"

export default function TaskNotFound() {
  return (
    <main className="flex flex-1 items-center justify-center px-6 py-24">
      <div className="flex max-w-md flex-col items-center gap-4 text-center">
        <p className="font-mono text-xs tracking-[0.25em] text-muted-foreground uppercase">
          404
        </p>
        <h1 className="text-2xl font-semibold tracking-tight">
          This task is no longer available
        </h1>
        <p className="text-sm text-muted-foreground">
          It may have been removed or the link may be incorrect.
        </p>
        <Button asChild>
          <Link href="/tasks">Back to my tasks</Link>
        </Button>
      </div>
    </main>
  )
}
