import type { Metadata } from "next"
import { HugeiconsIcon } from "@hugeicons/react"
import { UserMultiple02Icon } from "@hugeicons/core-free-icons"
import { Card, CardContent } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"

export const metadata: Metadata = { title: "Users – Admin" }

export default function AdminUsersPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Users</h1>
        <p className="mt-1 text-sm text-muted-foreground">View and manage user accounts, verification status, and profile health.</p>
      </div>

      <Card className="border-dashed">
        <CardContent className="flex flex-col items-center gap-4 py-16 text-center">
          <div className="flex h-14 w-14 items-center justify-center rounded-full bg-muted">
            <HugeiconsIcon
              icon={UserMultiple02Icon}
              className="size-7 text-muted-foreground"
              strokeWidth={1.5}
            />
          </div>
          <div className="space-y-1.5 max-w-sm">
            <p className="font-medium">Users not yet available</p>
            <p className="text-sm text-muted-foreground">
              User management, email verification review, and account health tooling will appear here. Suspension/ban actions require a separate backend authorization and audit design before they can be added.
            </p>
          </div>
          <Badge variant="outline" className="gap-1.5 text-xs">
            Waiting on #20 — email verification & user lifecycle
          </Badge>
        </CardContent>
      </Card>
    </div>
  )
}
