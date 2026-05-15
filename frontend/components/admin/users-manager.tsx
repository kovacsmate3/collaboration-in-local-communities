"use client"

import * as React from "react"
import {
  CheckmarkCircle01Icon,
  Cancel01Icon,
  SearchIcon,
} from "@hugeicons/core-free-icons"
import { HugeiconsIcon } from "@hugeicons/react"

import {
  useAdminUsers,
  useMakeAdmin,
  useRevokeAdmin,
  type AdminUserResponse,
} from "@/lib/api/admin/users"
import { Alert, AlertDescription } from "@/components/ui/alert"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { formatDate } from "@/lib/format"
import { cn } from "@/lib/utils"

const PAGE_SIZE = 20

function useDebounce<T>(value: T, delayMs: number): T {
  const [debounced, setDebounced] = React.useState(value)
  React.useEffect(() => {
    const id = setTimeout(() => setDebounced(value), delayMs)
    return () => clearTimeout(id)
  }, [value, delayMs])
  return debounced
}

function UserAvatar({
  user,
}: {
  user: Pick<AdminUserResponse, "displayName" | "email" | "photoUrl">
}) {
  const initials = (user.displayName ?? user.email)
    .split(" ")
    .slice(0, 2)
    .map((s) => s[0]?.toUpperCase() ?? "")
    .join("")

  if (user.photoUrl) {
    return (
      // eslint-disable-next-line @next/next/no-img-element
      <img
        src={user.photoUrl}
        alt={user.displayName ?? user.email}
        className="size-8 rounded-full object-cover"
      />
    )
  }

  return (
    <span className="inline-flex size-8 items-center justify-center rounded-full bg-muted text-xs font-medium text-muted-foreground">
      {initials}
    </span>
  )
}

function RoleBadge({ role }: { role: string }) {
  if (role === "Admin") {
    return (
      <Badge variant="warning" className="capitalize">
        {role}
      </Badge>
    )
  }
  return (
    <Badge variant="muted" className="capitalize">
      {role}
    </Badge>
  )
}

function UsersTableSkeleton({ columnCount }: { columnCount: number }) {
  return Array.from({ length: 8 }, (_, index) => (
    <TableRow key={`user-skeleton-${index}`}>
      {Array.from({ length: columnCount }, (__, col) => (
        <TableCell key={col} className="first:pl-4 last:pr-4">
          <Skeleton className="h-4 w-full max-w-32" />
        </TableCell>
      ))}
    </TableRow>
  ))
}

type ConfirmAction = {
  userId: string
  displayName: string
  action: "make-admin" | "revoke-admin"
}

export function UsersManager() {
  const [page, setPage] = React.useState(1)
  const [searchInput, setSearchInput] = React.useState("")
  const [roleFilter, setRoleFilter] = React.useState<string>("")
  const [confirmAction, setConfirmAction] =
    React.useState<ConfirmAction | null>(null)

  const search = useDebounce(searchInput, 300)

  const { data, isLoading, isError, error, refetch } = useAdminUsers({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    role: roleFilter || undefined,
  })

  const makeAdmin = useMakeAdmin()
  const revokeAdmin = useRevokeAdmin()

  const handleConfirm = () => {
    if (!confirmAction) return
    if (confirmAction.action === "make-admin") {
      makeAdmin.mutate(confirmAction.userId)
    } else {
      revokeAdmin.mutate(confirmAction.userId)
    }
    setConfirmAction(null)
  }

  const totalPages = data?.totalPages ?? 1
  const totalCount = data?.totalCount ?? 0

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <div className="relative max-w-sm flex-1">
          <HugeiconsIcon
            icon={SearchIcon}
            className="absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground"
            strokeWidth={1.5}
          />
          <Input
            placeholder="Search by name or email..."
            value={searchInput}
            onChange={(e) => {
              setSearchInput(e.target.value)
              setPage(1)
            }}
            className="pl-8"
          />
        </div>

        <Select
          value={roleFilter}
          onValueChange={(val) => {
            setRoleFilter(val === "all" ? "" : val)
            setPage(1)
          }}
        >
          <SelectTrigger className="w-36">
            <SelectValue placeholder="All roles" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All roles</SelectItem>
            <SelectItem value="Admin">Admin</SelectItem>
            <SelectItem value="User">User</SelectItem>
          </SelectContent>
        </Select>

        <span className="shrink-0 text-sm text-muted-foreground">
          {totalCount} user{totalCount !== 1 ? "s" : ""}
        </span>
      </div>

      {isError ? (
        <Alert variant="destructive">
          <AlertDescription>
            Failed to load users —{" "}
            {error instanceof Error ? error.message : "unknown error"}.{" "}
            <button
              className="underline hover:no-underline"
              onClick={() => void refetch()}
            >
              Retry
            </button>
          </AlertDescription>
        </Alert>
      ) : null}

      <div className="rounded-lg border bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="pl-4">User</TableHead>
              <TableHead>Email</TableHead>
              <TableHead>Roles</TableHead>
              <TableHead>Verified</TableHead>
              <TableHead>Joined</TableHead>
              <TableHead className="pr-4 text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <UsersTableSkeleton columnCount={6} />
            ) : !data || data.items.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={6}
                  className="py-12 text-center text-sm text-muted-foreground"
                >
                  No users found.
                </TableCell>
              </TableRow>
            ) : (
              data.items.map((user) => {
                const isAdmin = user.roles.includes("Admin")
                return (
                  <TableRow key={user.id}>
                    <TableCell className="pl-4">
                      <div className="flex items-center gap-2.5">
                        <UserAvatar user={user} />
                        <span className="font-medium">
                          {user.displayName ?? (
                            <span className="text-muted-foreground italic">
                              No profile
                            </span>
                          )}
                        </span>
                      </div>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {user.email}
                    </TableCell>
                    <TableCell>
                      <div className="flex flex-wrap gap-1">
                        {user.roles.length === 0 ? (
                          <span className="text-xs text-muted-foreground">
                            —
                          </span>
                        ) : (
                          user.roles.map((r) => <RoleBadge key={r} role={r} />)
                        )}
                      </div>
                    </TableCell>
                    <TableCell>
                      <HugeiconsIcon
                        icon={
                          user.emailConfirmed
                            ? CheckmarkCircle01Icon
                            : Cancel01Icon
                        }
                        className={cn(
                          "size-4",
                          user.emailConfirmed
                            ? "text-success"
                            : "text-muted-foreground"
                        )}
                        strokeWidth={1.5}
                      />
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {user.joinedAt ? formatDate(user.joinedAt) : "—"}
                    </TableCell>
                    <TableCell className="pr-4 text-right">
                      {isAdmin ? (
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() =>
                            setConfirmAction({
                              userId: user.id,
                              displayName: user.displayName ?? user.email,
                              action: "revoke-admin",
                            })
                          }
                        >
                          Remove Admin
                        </Button>
                      ) : (
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() =>
                            setConfirmAction({
                              userId: user.id,
                              displayName: user.displayName ?? user.email,
                              action: "make-admin",
                            })
                          }
                        >
                          Make Admin
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                )
              })
            )}
          </TableBody>
        </Table>
      </div>

      {totalPages > 1 ? (
        <div className="flex items-center justify-between text-sm text-muted-foreground">
          <span>
            Page {page} of {totalPages}
          </span>
          <div className="flex gap-2">
            <Button
              size="sm"
              variant="outline"
              disabled={page <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
            >
              Previous
            </Button>
            <Button
              size="sm"
              variant="outline"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            >
              Next
            </Button>
          </div>
        </div>
      ) : null}

      <AlertDialog
        open={confirmAction !== null}
        onOpenChange={(open) => {
          if (!open) setConfirmAction(null)
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {confirmAction?.action === "make-admin"
                ? "Promote to Admin"
                : "Remove Admin Access"}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {confirmAction?.action === "make-admin"
                ? `Grant admin privileges to ${confirmAction?.displayName}? They will have full access to the admin panel.`
                : `Remove admin privileges from ${confirmAction?.displayName}? They will lose access to the admin panel.`}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleConfirm}>
              Confirm
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
