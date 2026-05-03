"use client"

import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import { UserAvatar } from "@/components/shared/user-avatar"
import { useAuth } from "@/lib/auth-context"

export default function AdminProfilePage() {
  const { user } = useAuth()

  if (!user) return null

  return (
    <div className="mx-auto max-w-2xl space-y-6 p-6">
      {/* Identity header */}
      <div className="flex items-center gap-5">
        <UserAvatar name={user.name} src={user.avatarUrl} size="lg" />
        <div className="flex flex-col gap-1">
          <div className="flex items-center gap-2">
            <h1 className="text-xl font-semibold leading-tight">{user.name}</h1>
            <Badge variant="secondary">{user.role}</Badge>
          </div>
          <p className="text-sm text-muted-foreground">{user.email}</p>
        </div>
      </div>

      <Separator />

      {/* Account details card */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Account details</CardTitle>
          <CardDescription>
            Your identity as seen across the platform. Full editing will be
            available once the authentication system ships.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <dl className="space-y-4 text-sm">
            <div className="grid grid-cols-3 gap-2">
              <dt className="font-medium text-muted-foreground">Full name</dt>
              <dd className="col-span-2">{user.name}</dd>
            </div>
            <Separator />
            <div className="grid grid-cols-3 gap-2">
              <dt className="font-medium text-muted-foreground">Email</dt>
              <dd className="col-span-2">{user.email}</dd>
            </div>
            <Separator />
            <div className="grid grid-cols-3 gap-2">
              <dt className="font-medium text-muted-foreground">Role</dt>
              <dd className="col-span-2">
                <Badge variant="secondary">{user.role}</Badge>
              </dd>
            </div>
            <Separator />
            <div className="grid grid-cols-3 gap-2">
              <dt className="font-medium text-muted-foreground">User ID</dt>
              <dd className="col-span-2 font-mono text-xs text-muted-foreground">
                {user.id}
              </dd>
            </div>
          </dl>
        </CardContent>
      </Card>
    </div>
  )
}
