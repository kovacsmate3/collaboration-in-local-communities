import type { Metadata } from "next"
import { SkillsManager } from "@/components/admin/skills-manager"

export const metadata: Metadata = { title: "Skills – Admin" }

export default function AdminSkillsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Skills</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Manage the skill catalog — approve submissions, deactivate stale
          skills.
        </p>
      </div>
      <SkillsManager />
    </div>
  )
}
