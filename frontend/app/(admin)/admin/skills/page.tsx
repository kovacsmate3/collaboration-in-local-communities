import type { Metadata } from "next"
import { HugeiconsIcon } from "@hugeicons/react"
import { BookOpen01Icon } from "@hugeicons/core-free-icons"
import { Card, CardContent } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"

export const metadata: Metadata = { title: "Skills – Admin" }

export default function AdminSkillsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Skills</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Manage the curated skill catalog that helpers can add to their
          profiles.
        </p>
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
          <div className="max-w-sm space-y-1.5">
            <p className="font-medium">Skills not yet available</p>
            <p className="text-sm text-muted-foreground">
              Once the skills endpoint (issue #119) ships, this page will allow
              you to create, rename, and deactivate skills in the catalog.
              Helpers will be able to add skills from this list to their
              profiles.
            </p>
          </div>
          <Badge variant="outline" className="gap-1.5 text-xs">
            Waiting on #119 — admin skill CRUD
          </Badge>
        </CardContent>
      </Card>
    </div>
  )
}
