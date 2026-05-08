"use client"

import * as React from "react"
import {
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  getFilteredRowModel,
  type SortingState,
  type ColumnFiltersState,
} from "@tanstack/react-table"
import { Add01Icon } from "@hugeicons/core-free-icons"
import { HugeiconsIcon } from "@hugeicons/react"

import {
  useAdminCategories,
  type AdminCategoryResponse,
} from "@/lib/api/admin/categories"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"
import { CategoriesTable } from "@/components/admin/categories-table"
import { createCategoryColumns } from "@/components/admin/category-table-columns"
import {
  CreateCategoryDialog,
  EditCategoryDialog,
} from "@/components/admin/category-form-dialog"
import { DeleteCategoryDialog } from "@/components/admin/delete-category-dialog"

export function CategoriesManager() {
  const { data, isLoading, isError, error, refetch } = useAdminCategories()

  const [sorting, setSorting] = React.useState<SortingState>([
    { id: "sortOrder", desc: false },
  ])
  const [columnFilters, setColumnFilters] = React.useState<ColumnFiltersState>(
    []
  )
  const [createOpen, setCreateOpen] = React.useState(false)
  const [editTarget, setEditTarget] =
    React.useState<AdminCategoryResponse | null>(null)
  const [deleteTarget, setDeleteTarget] =
    React.useState<AdminCategoryResponse | null>(null)

  const columns = React.useMemo(
    () =>
      createCategoryColumns({
        onEdit: setEditTarget,
        onDelete: setDeleteTarget,
      }),
    []
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

  return (
    <div className="space-y-6">
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

      {isError ? (
        <Alert variant="destructive">
          <AlertDescription>
            Failed to load categories -{" "}
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

      <CategoriesTable
        table={table}
        isLoading={isLoading}
        totalCount={data?.length ?? 0}
        columnCount={columns.length}
      />

      <CreateCategoryDialog open={createOpen} onOpenChange={setCreateOpen} />
      <EditCategoryDialog
        category={editTarget}
        onOpenChange={(open) => {
          if (!open) {
            setEditTarget(null)
          }
        }}
      />
      <DeleteCategoryDialog
        category={deleteTarget}
        onOpenChange={(open) => {
          if (!open) {
            setDeleteTarget(null)
          }
        }}
      />
    </div>
  )
}
