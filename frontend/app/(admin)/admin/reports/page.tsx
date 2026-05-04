import type { Metadata } from "next"
import { HugeiconsIcon } from "@hugeicons/react"
import { ShieldUserIcon } from "@hugeicons/core-free-icons"
import { Card, CardContent } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"

export const metadata: Metadata = { title: "Reports – Admin" }

export default function AdminReportsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Reports</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Trust and safety reports, disputes, and cancelled-task review queue.
        </p>
      </div>

      <Card className="border-dashed">
        <CardContent className="flex flex-col items-center gap-4 py-16 text-center">
          <div className="flex h-14 w-14 items-center justify-center rounded-full bg-muted">
            <HugeiconsIcon
              icon={ShieldUserIcon}
              className="size-7 text-muted-foreground"
              strokeWidth={1.5}
            />
          </div>
          <div className="max-w-sm space-y-1.5">
            <p className="font-medium">Reports not yet available</p>
            <p className="text-sm text-muted-foreground">
              The reports queue will surface trust-and-safety flags, user
              disputes, and cancelled task reasons once backend support is in
              place (issue #55). No placeholder actions are shown — real report
              data will drive this interface.
            </p>
          </div>
          <Badge variant="outline" className="gap-1.5 text-xs">
            Waiting on #55 — cancellation reasons + audit log
          </Badge>
        </CardContent>
      </Card>
    </div>
  )
}
