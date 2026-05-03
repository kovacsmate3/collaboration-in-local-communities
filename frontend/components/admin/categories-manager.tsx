"use client"

import * as React from "react"
import {
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  getFilteredRowModel,
  flexRender,
  type ColumnDef,
  type SortingState,
  type ColumnFiltersState,
} from "@tanstack/react-table"
import { HugeiconsIcon } from "@hugeicons/react"
import {
  Add01Icon,
  Edit02Icon,
  Delete02Icon,
  ArrowUpDownIcon,
  SearchIcon,
  InformationCircleIcon,
} from "@hugeicons/core-free-icons"

import { useAdminCategories, type AdminCategoryResponse } from "@/lib/api/admin/categories"
import { ICON_REGISTRY } from "@/lib/icon-registry"

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Badge } from "@/components/ui/badge"
import { Skeleton } from "@/components/ui/skeleton"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import { CreateCategoryDialog } from "./category-form-dialog"
import { EditCategoryDialog } from "./category-form-dialog"
import { DeleteCategoryDialog } from "./delete-category-dialog"

// ── Column helpers ────────────────────────────────────────────────────────────

function IconCell({ iconName }: { iconName: string }) {
  const icon = ICON_REGISTRY[iconName as keyof typeof ICON_REGISTRY]
  if (!icon) {
    return (
      <Tooltip>
        <TooltipTrigger asChild>
          <span className="flex items-center gap-1.5 text-muted-foreground text-xs">
            <HugeiconsIcon icon={InformationCircleIcon} className="size-4" strokeWidth={1.5} />
            Unknown
          </span>
        </TooltipTrigger>
        <TooltipContent>Icon &ldquo;{iconName}&rdquo; not in registry</TooltipContent>
      </Tooltip>
    )
  }
  return (
    <span className="flex items-center gap-2">
      <HugeiconsIcon icon={icon} className="size-5" strokeWidth={1.5} />
      <span className="hidden text-xs text-muted-foreground xl:inline">{iconName}</span>
    </span>
  )
}

function SortButton({
  children,
  sorted,
  onClick,
}: {
  children: React.ReactNode
  sorted: false | "asc" | "desc"
  onClick: () => void
}) {
  return (
    <button
      onClick={onClick}
      className="flex items-center gap-1 hover:text-foreground transition-colors"
    >
      {children}
      <HugeiconsIcon
        icon={ArrowUpDownIcon}
        className={
          sorted
            ? "size-3.5 text-primary"
            : "size-3.5 text-muted-foreground/50"
        }
        strokeWidth={1.5}
      />
    </button>
  )
}

// ── Main component ────────────────────────────────────────────────────────────

export function CategoriesManager() {
  const { data, isLoading, isError, error, refetch } = useAdminCategories()

  const [sorting, setSorting] = React.useState<SortingState>([
    { id: "sortOrder", desc: false },
  ])
  const [columnFilters, setColumnFilters] = React.useState<ColumnFiltersState>([])
  const [createOpen, setCreateOpen] = React.useState(false)
  const [editTarget, setEditTarget] = React.useState<AdminCategoryResponse | null>(null)
  const [deleteTarget, setDeleteTarget] = React.useState<AdminCategoryResponse | null>(null)

  const columns = React.useMemo<ColumnDef<AdminCategoryResponse>[]>(
    () => [
      {
        accessorKey: "icon",
        header: "Icon",
        cell: ({ row }) => <IconCell iconName={row.original.icon} />,
        enableSorting: false,
      },
      {
        accessorKey: "name",
        header: ({ column }) => (
          <SortButton
            sorted={column.getIsSorted()}
            onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
          >
            Name
          </SortButton>
        ),
        cell: ({ row }) => (
          <div>
            <span className="font-medium">{row.original.name}</span>
            {row.original.description && (
              <p className="mt-0.5 text-xs text-muted-foreground line-clamp-1">
                {row.original.description}
              </p>
            )}
          </div>
        ),
        filterFn: "includesString",
      },
      {
        accessorKey: "code",
        header: "Code",
        cell: ({ row }) => (
          <code className="rounded bg-muted px-1.5 py-0.5 text-xs font-mono text-muted-foreground">
            {row.original.code}
          </code>
        ),
      },
      {
        accessorKey: "sortOrder",
        header: ({ column }) => (
          <SortButton
            sorted={column.getIsSorted()}
            onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
          >
            Order
          </SortButton>
        ),
        cell: ({ row }) => (
          <span className="tabular-nums text-sm">{row.original.sortOrder}</span>
        ),
      },
      {
        accessorKey: "isActive",
        header: "Status",
        cell: ({ row }) =>
          row.original.isActive ? (
            <Badge variant="success" className="text-xs">Active</Badge>
          ) : (
            <Badge variant="destructive" className="text-xs">Inactive</Badge>
          ),
      },
      {
        id: "actions",
        header: "",
        cell: ({ row }) => {
          const cat = row.original
          return (
            <div className="flex items-center justify-end gap-1">
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="size-8"
                    onClick={() => setEditTarget(cat)}
                  >
                    <HugeiconsIcon icon={Edit02Icon} className="size-4" strokeWidth={1.5} />
                    <span className="sr-only">Edit {cat.name}</span>
                  </Button>
                </TooltipTrigger>
                <TooltipContent>Edit</TooltipContent>
              </Tooltip>
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="size-8 text-destructive hover:text-destructive"
                    onClick={() => setDeleteTarget(cat)}
                    disabled={!cat.isActive}
                  >
                    <HugeiconsIcon icon={Delete02Icon} className="size-4" strokeWidth={1.5} />
                    <span className="sr-only">Deactivate {cat.name}</span>
                  </Button>
                </TooltipTrigger>
                <TooltipContent>
                  {cat.isActive ? "Deactivate" : "Already inactive"}
                </TooltipContent>
              </Tooltip>
            </div>
          )
        },
        enableSorting: false,
      },
    ],
    [],
  )

  // eslint-disable-next-line react-hooks/incompatible-library
  const table = useReactTable({
    data: data ?? [],
    columns,
    state: { sorting, columnFilters },
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
  })

  // ── Render ──────────────────────────────────────────────────────────────────

  return (
    <div className="space-y-6">
      {/* Header row */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Categories</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Manage task categories available to seekers
          </p>
        </div>
        <Button onClick={() => setCreateOpen(true)} className="shrink-0">
          <HugeiconsIcon icon={Add01Icon} className="size-4" strokeWidth={2} />
          New category
        </Button>
      </div>

      {/* Error state */}
      {isError && (
        <Alert variant="destructive">
          <AlertDescription>
            Failed to load categories —{" "}
            {error instanceof Error ? error.message : "unknown error"}.{" "}
            <button
              className="underline hover:no-underline"
              onClick={() => void refetch()}
            >
              Retry
            </button>
          </AlertDescription>
        </Alert>
      )}

      {/* Table card */}
      <div className="rounded-lg border bg-card">
        {/* Toolbar */}
        <div className="flex items-center gap-3 border-b px-4 py-3">
          <div className="relative flex-1 max-w-sm">
            <HugeiconsIcon
              icon={SearchIcon}
              className="absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
              strokeWidth={1.5}
            />
            <Input
              placeholder="Filter by name…"
              value={
                (table.getColumn("name")?.getFilterValue() as string) ?? ""
              }
              onChange={(e) =>
                table.getColumn("name")?.setFilterValue(e.target.value)
              }
              className="pl-8"
            />
          </div>
          <span className="shrink-0 text-sm text-muted-foreground">
            {table.getFilteredRowModel().rows.length} of {data?.length ?? 0}
          </span>
        </div>

        {/* Table */}
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map((hg) => (
              <TableRow key={hg.id}>
                {hg.headers.map((header) => (
                  <TableHead key={header.id} className="first:pl-4 last:pr-4">
                    {header.isPlaceholder
                      ? null
                      : flexRender(
                          header.column.columnDef.header,
                          header.getContext(),
                        )}
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {isLoading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  <TableCell className="pl-4"><Skeleton className="size-5 rounded" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-8" /></TableCell>
                  <TableCell><Skeleton className="h-5 w-14 rounded-full" /></TableCell>
                  <TableCell className="pr-4" />
                </TableRow>
              ))
            ) : table.getRowModel().rows.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={columns.length}
                  className="h-32 text-center text-muted-foreground"
                >
                  {(data?.length ?? 0) === 0
                    ? "No categories yet — create one to get started."
                    : "No categories match your filter."}
                </TableCell>
              </TableRow>
            ) : (
              table.getRowModel().rows.map((row) => (
                <TableRow
                  key={row.id}
                  data-state={row.getIsSelected() ? "selected" : undefined}
                >
                  {row.getVisibleCells().map((cell) => (
                    <TableCell key={cell.id} className="first:pl-4 last:pr-4">
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {/* Dialogs */}
      <CreateCategoryDialog open={createOpen} onOpenChange={setCreateOpen} />
      <EditCategoryDialog
        category={editTarget}
        onOpenChange={(o) => { if (!o) setEditTarget(null) }}
      />
      <DeleteCategoryDialog
        category={deleteTarget}
        onOpenChange={(o) => { if (!o) setDeleteTarget(null) }}
      />
    </div>
  )
}
