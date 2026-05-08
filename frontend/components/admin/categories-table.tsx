"use client"

import { SearchIcon } from "@hugeicons/core-free-icons"
import { HugeiconsIcon } from "@hugeicons/react"
import { flexRender } from "@tanstack/react-table"
import type { Table as TanstackTable } from "@tanstack/react-table"

import type { AdminCategoryResponse } from "@/lib/api/admin/categories"
import { CategoriesTableEmptyState } from "@/components/admin/categories-table-empty-state"
import { CategoriesTableSkeleton } from "@/components/admin/categories-table-skeleton"
import { Input } from "@/components/ui/input"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"

interface CategoriesTableProps {
  table: TanstackTable<AdminCategoryResponse>
  isLoading: boolean
  totalCount: number
  columnCount: number
}

export function CategoriesTable({
  table,
  isLoading,
  totalCount,
  columnCount,
}: CategoriesTableProps) {
  const filteredCount = table.getFilteredRowModel().rows.length
  const nameFilter = (table.getColumn("name")?.getFilterValue() as string) ?? ""

  return (
    <div className="rounded-lg border bg-card">
      <div className="flex items-center gap-3 border-b px-4 py-3">
        <div className="relative max-w-sm flex-1">
          <HugeiconsIcon
            icon={SearchIcon}
            className="absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground"
            strokeWidth={1.5}
          />
          <Input
            placeholder="Filter by name..."
            value={nameFilter}
            onChange={(event) =>
              table.getColumn("name")?.setFilterValue(event.target.value)
            }
            className="pl-8"
          />
        </div>
        <span className="shrink-0 text-sm text-muted-foreground">
          {filteredCount} of {totalCount}
        </span>
      </div>

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
            <CategoriesTableSkeleton />
          ) : table.getRowModel().rows.length === 0 ? (
            <CategoriesTableEmptyState
              totalCount={totalCount}
              columnCount={columnCount}
            />
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
  )
}
