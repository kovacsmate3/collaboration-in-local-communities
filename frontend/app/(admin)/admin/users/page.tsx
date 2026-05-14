import type { Metadata } from "next"
import { UsersManager } from "@/components/admin/users-manager"

export const metadata: Metadata = { title: "Users – Admin" }

export default function AdminUsersPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Users</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          View and manage user accounts and admin access.
        </p>
      </div>
      <UsersManager />
    </div>
  )
}
