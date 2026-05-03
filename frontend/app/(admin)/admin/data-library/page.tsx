import type { Metadata } from "next"
import { HugeiconsIcon } from "@hugeicons/react"
import { BookOpen01Icon } from "@hugeicons/core-free-icons"
import { Card, CardContent } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"

export const metadata: Metadata = { title: "Data Library – Admin" }

export default function AdminDataLibraryPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Data Library</h1>
        <p className="mt-1 text-sm text-muted-foreground">Manage reference data: terms versions, icon lists, and other configuration.</p>
      </div>

      <Card className="border-dashed">
        <CardContent className="flex flex-col items-center gap-4 py-16 text-center">
          <div className="flex h-14 w-14 items-center justify-center rounded-full bg-muted">
            <HugeiconsIcon
              icon={BookOpen01Icon}
              className="size-7 text-muted-foreground"
              strokeWidth={1.5}
            />
          </div>
          <div className="space-y-1.5 max-w-sm">
            <p className="font-medium">Data Library not yet available</p>
            <p className="text-sm text-muted-foreground">
              Reference data management — terms versions (#116 / #23), icon lists (#125), and other configuration tables — will be accessible here without requiring migrations or redeploys.
            </p>
          </div>
          <Badge variant="outline" className="gap-1.5 text-xs">
            Waiting on #116 — terms endpoints
          </Badge>
        </CardContent>
      </Card>
    </div>
  )
}
