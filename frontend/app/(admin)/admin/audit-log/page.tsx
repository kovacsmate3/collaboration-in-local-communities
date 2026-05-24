import type { Metadata } from "next"
import { AuditLogViewer } from "@/components/admin/audit-log-viewer"

export const metadata: Metadata = { title: "Audit Log – Admin" }

export default function AdminAuditLogPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Audit Log</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Searchable trail of all admin and system events.
        </p>
      </div>
      <AuditLogViewer />
    </div>
  )
}
