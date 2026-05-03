import type { Metadata } from "next"
import { HugeiconsIcon } from "@hugeicons/react"
import { ArchiveIcon } from "@hugeicons/core-free-icons"
import { Card, CardContent } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"

export const metadata: Metadata = { title: "Audit Log – Admin" }

export default function AdminAuditLogPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Audit Log</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Explore the audit trail for admin and safety actions across the
          platform.
        </p>
      </div>

      <Card className="border-dashed">
        <CardContent className="flex flex-col items-center gap-4 py-16 text-center">
          <div className="flex h-14 w-14 items-center justify-center rounded-full bg-muted">
            <HugeiconsIcon
              icon={ArchiveIcon}
              className="size-7 text-muted-foreground"
              strokeWidth={1.5}
            />
          </div>
          <div className="max-w-sm space-y-1.5">
            <p className="font-medium">Audit Log not yet available</p>
            <p className="text-sm text-muted-foreground">
              Every admin mutation, safety action, verification event, and terms
              acceptance will appear in a searchable audit trail once the
              backend audit event model is wired up (issue #67).
            </p>
          </div>
          <Badge variant="outline" className="gap-1.5 text-xs">
            Waiting on #67 — security audit
          </Badge>
        </CardContent>
      </Card>
    </div>
  )
}
