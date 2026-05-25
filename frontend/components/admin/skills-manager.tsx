"use client"

import * as React from "react"
import {
  useReactTable,
  getCoreRowModel,
  type ColumnDef,
  flexRender,
} from "@tanstack/react-table"
import { toast } from "sonner"
import { Loading03Icon } from "@hugeicons/core-free-icons"
import { HugeiconsIcon } from "@hugeicons/react"

import {
  useAdminSkills,
  useApproveSkill,
  useActivateSkill,
  useDeactivateSkill,
  useDeleteSkill,
  type AdminSkillResponse,
} from "@/lib/api/admin/skills"
import { ApiError } from "@/lib/api/client"
import { formatDate } from "@/lib/format"
import { Alert, AlertDescription } from "@/components/ui/alert"
import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"

const PAGE_SIZE = 20

// ── Row actions ───────────────────────────────────────────────────────────────

interface SkillRowActionsProps {
  skill: AdminSkillResponse
  onDelete: (skill: AdminSkillResponse) => void
}

function SkillRowActions({ skill, onDelete }: SkillRowActionsProps) {
  const approve = useApproveSkill()
  const activate = useActivateSkill()
  const deactivate = useDeactivateSkill()

  const isPending =
    approve.isPending || activate.isPending || deactivate.isPending

  async function handleApprove() {
    try {
      await approve.mutateAsync(skill.id)
      toast.success(`"${skill.name}" approved`)
    } catch {
      toast.error("Failed to approve skill")
    }
  }

  async function handleActivate() {
    try {
      await activate.mutateAsync(skill.id)
      toast.success(`"${skill.name}" activated`)
    } catch {
      toast.error("Failed to activate skill")
    }
  }

  async function handleDeactivate() {
    try {
      await deactivate.mutateAsync(skill.id)
      toast.success(`"${skill.name}" deactivated`)
    } catch {
      toast.error("Failed to deactivate skill")
    }
  }

  return (
    <div className="flex items-center justify-end gap-2">
      {skill.status === "Pending" && (
        <Button
          size="sm"
          variant="outline"
          onClick={() => void handleApprove()}
          disabled={isPending}
        >
          {approve.isPending && (
            <HugeiconsIcon
              icon={Loading03Icon}
              className="size-3 animate-spin"
              strokeWidth={2}
            />
          )}
          Approve
        </Button>
      )}
      {skill.status === "Approved" && skill.isActive && (
        <Button
          size="sm"
          variant="outline"
          onClick={() => void handleDeactivate()}
          disabled={isPending}
        >
          {deactivate.isPending && (
            <HugeiconsIcon
              icon={Loading03Icon}
              className="size-3 animate-spin"
              strokeWidth={2}
            />
          )}
          Deactivate
        </Button>
      )}
      {skill.status === "Approved" && !skill.isActive && (
        <Button
          size="sm"
          variant="outline"
          onClick={() => void handleActivate()}
          disabled={isPending}
        >
          {activate.isPending && (
            <HugeiconsIcon
              icon={Loading03Icon}
              className="size-3 animate-spin"
              strokeWidth={2}
            />
          )}
          Activate
        </Button>
      )}
      <Button
        size="sm"
        variant="destructive"
        onClick={() => onDelete(skill)}
        disabled={isPending}
      >
        Delete
      </Button>
    </div>
  )
}

// ── Delete confirmation dialog ────────────────────────────────────────────────

interface DeleteSkillDialogProps {
  skill: AdminSkillResponse | null
  onOpenChange: (open: boolean) => void
}

function DeleteSkillDialog({ skill, onOpenChange }: DeleteSkillDialogProps) {
  const { mutateAsync, isPending } = useDeleteSkill()

  async function handleConfirm() {
    if (!skill) return
    try {
      await mutateAsync(skill.id)
      toast.success(`"${skill.name}" deleted`)
      onOpenChange(false)
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        toast.error(
          typeof err.body === "string"
            ? err.body
            : "This skill is linked to user profiles and cannot be deleted. Deactivate it instead."
        )
        return
      }
      toast.error(err instanceof Error ? err.message : "Failed to delete skill")
    }
  }

  return (
    <AlertDialog
      open={Boolean(skill)}
      onOpenChange={(o) => {
        if (!isPending) onOpenChange(o)
      }}
    >
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Delete skill permanently?</AlertDialogTitle>
          <AlertDialogDescription>
            This will permanently remove <strong>{skill?.name}</strong> (code:{" "}
            <code className="rounded bg-muted px-1 py-0.5 font-mono text-xs">
              {skill?.code}
            </code>
            ). This action cannot be undone. If any profile still links this
            skill the deletion will be blocked — deactivate it instead.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <Button
            variant="destructive-solid"
            onClick={() => void handleConfirm()}
            disabled={isPending}
          >
            {isPending && (
              <HugeiconsIcon
                icon={Loading03Icon}
                className="size-4 animate-spin"
                strokeWidth={2}
              />
            )}
            Delete permanently
          </Button>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

// ── Skeleton rows ─────────────────────────────────────────────────────────────

function SkillsTableSkeleton() {
  return Array.from({ length: 6 }, (_, index) => (
    <TableRow key={`skill-skeleton-${index}`}>
      <TableCell className="pl-4">
        <div className="space-y-1">
          <Skeleton className="h-4 w-32" />
          <Skeleton className="h-3 w-48" />
        </div>
      </TableCell>
      <TableCell>
        <Skeleton className="h-4 w-20" />
      </TableCell>
      <TableCell>
        <Skeleton className="h-5 w-16 rounded-full" />
      </TableCell>
      <TableCell>
        <Skeleton className="h-5 w-14 rounded-full" />
      </TableCell>
      <TableCell>
        <Skeleton className="h-4 w-24" />
      </TableCell>
      <TableCell>
        <Skeleton className="h-4 w-24" />
      </TableCell>
      <TableCell className="pr-4" />
    </TableRow>
  ))
}

// ── Main component ────────────────────────────────────────────────────────────

export function SkillsManager() {
  const [statusFilter, setStatusFilter] = React.useState<
    "all" | "Pending" | "Approved"
  >("all")
  const [page, setPage] = React.useState(1)
  const [deleteTarget, setDeleteTarget] =
    React.useState<AdminSkillResponse | null>(null)

  function handleStatusChange(value: string) {
    setStatusFilter(value as "all" | "Pending" | "Approved")
    setPage(1)
  }

  const params = {
    page,
    pageSize: PAGE_SIZE,
    status: statusFilter === "all" ? undefined : statusFilter,
  }

  const { data, isLoading, isError, error, refetch } = useAdminSkills(params)

  const columns = React.useMemo<ColumnDef<AdminSkillResponse>[]>(
    () => [
      {
        accessorKey: "name",
        header: "Name",
        cell: ({ row }) => (
          <div>
            <span className="font-medium">{row.original.name}</span>
            {row.original.description ? (
              <p className="mt-0.5 line-clamp-1 text-xs text-muted-foreground">
                {row.original.description}
              </p>
            ) : null}
          </div>
        ),
      },
      {
        accessorKey: "code",
        header: "Code",
        cell: ({ row }) => (
          <code className="rounded bg-muted px-1.5 py-0.5 font-mono text-xs text-muted-foreground">
            {row.original.code}
          </code>
        ),
      },
      {
        accessorKey: "status",
        header: "Status",
        cell: ({ row }) =>
          row.original.status === "Approved" ? (
            <Badge variant="success" className="text-xs">
              Approved
            </Badge>
          ) : (
            <Badge variant="warning" className="text-xs">
              Pending
            </Badge>
          ),
      },
      {
        accessorKey: "isActive",
        header: "Active",
        cell: ({ row }) =>
          row.original.isActive ? (
            <Badge variant="default" className="text-xs">
              Active
            </Badge>
          ) : (
            <Badge variant="muted" className="text-xs">
              Inactive
            </Badge>
          ),
      },
      {
        accessorKey: "approvedAt",
        header: "Approved",
        cell: ({ row }) =>
          row.original.approvedAt ? (
            <span className="text-sm tabular-nums">
              {formatDate(row.original.approvedAt)}
            </span>
          ) : (
            <span className="text-sm text-muted-foreground">—</span>
          ),
      },
      {
        accessorKey: "createdAt",
        header: "Created",
        cell: ({ row }) => (
          <span className="text-sm tabular-nums">
            {formatDate(row.original.createdAt)}
          </span>
        ),
      },
      {
        id: "actions",
        header: "",
        cell: ({ row }) => (
          <SkillRowActions skill={row.original} onDelete={setDeleteTarget} />
        ),
        enableSorting: false,
      },
    ],
    []
  )

  // eslint-disable-next-line react-hooks/incompatible-library
  const table = useReactTable({
    data: data?.items ?? [],
    columns,
    getCoreRowModel: getCoreRowModel(),
    manualPagination: true,
  })

  const totalPages = data?.totalPages ?? 1

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <Select value={statusFilter} onValueChange={handleStatusChange}>
          <SelectTrigger className="w-40">
            <SelectValue placeholder="All statuses" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All</SelectItem>
            <SelectItem value="Pending">Pending</SelectItem>
            <SelectItem value="Approved">Approved</SelectItem>
          </SelectContent>
        </Select>
        {data ? (
          <span className="text-sm text-muted-foreground">
            {data.totalCount} skill{data.totalCount === 1 ? "" : "s"}
          </span>
        ) : null}
      </div>

      {isError ? (
        <Alert variant="destructive">
          <AlertDescription>
            Failed to load skills —{" "}
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
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id}>
                {headerGroup.headers.map((header) => (
                  <TableHead key={header.id} className="first:pl-4 last:pr-4">
                    {header.isPlaceholder
                      ? null
                      : flexRender(
                          header.column.columnDef.header,
                          header.getContext()
                        )}
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <SkillsTableSkeleton />
            ) : table.getRowModel().rows.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={columns.length}
                  className="h-32 text-center text-muted-foreground"
                >
                  No skills found.
                </TableCell>
              </TableRow>
            ) : (
              table.getRowModel().rows.map((row) => (
                <TableRow key={row.id}>
                  {row.getVisibleCells().map((cell) => (
                    <TableCell key={cell.id} className="first:pl-4 last:pr-4">
                      {flexRender(
                        cell.column.columnDef.cell,
                        cell.getContext()
                      )}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {totalPages > 1 || page > 1 ? (
        <div className="flex items-center justify-between">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page <= 1 || isLoading}
          >
            Previous
          </Button>
          <span className="text-sm text-muted-foreground">
            Page {page} of {totalPages}
          </span>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPage((p) => p + 1)}
            disabled={page >= totalPages || isLoading}
          >
            Next
          </Button>
        </div>
      ) : null}

      <DeleteSkillDialog
        skill={deleteTarget}
        onOpenChange={(open) => {
          if (!open) setDeleteTarget(null)
        }}
      />
    </div>
  )
}
