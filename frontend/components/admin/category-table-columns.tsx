"use client"

import type { ColumnDef } from "@tanstack/react-table"

import { type AdminCategoryResponse } from "@/lib/api/admin/categories"
import { Badge } from "@/components/ui/badge"
import { CategoryIconCell } from "@/components/admin/category-icon-cell"
import { CategoryRowActions } from "@/components/admin/category-row-actions"
import { CategorySortButton } from "@/components/admin/category-sort-button"

interface CategoryTableActions {
  onEdit: (category: AdminCategoryResponse) => void
  onDelete: (category: AdminCategoryResponse) => void
}

export function createCategoryColumns({
  onEdit,
  onDelete,
}: CategoryTableActions): ColumnDef<AdminCategoryResponse>[] {
  return [
    {
      accessorKey: "icon",
      header: "Icon",
      cell: ({ row }) => <CategoryIconCell iconName={row.original.icon} />,
      enableSorting: false,
    },
    {
      accessorKey: "name",
      header: ({ column }) => (
        <CategorySortButton
          sorted={column.getIsSorted()}
          onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
        >
          Name
        </CategorySortButton>
      ),
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
      filterFn: "includesString",
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
      accessorKey: "sortOrder",
      header: ({ column }) => (
        <CategorySortButton
          sorted={column.getIsSorted()}
          onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
        >
          Order
        </CategorySortButton>
      ),
      cell: ({ row }) => (
        <span className="text-sm tabular-nums">{row.original.sortOrder}</span>
      ),
    },
    {
      accessorKey: "isActive",
      header: "Status",
      cell: ({ row }) =>
        row.original.isActive ? (
          <Badge variant="success" className="text-xs">
            Active
          </Badge>
        ) : (
          <Badge variant="destructive" className="text-xs">
            Inactive
          </Badge>
        ),
    },
    {
      id: "actions",
      header: "",
      cell: ({ row }) => (
        <CategoryRowActions
          category={row.original}
          onEdit={onEdit}
          onDelete={onDelete}
        />
      ),
      enableSorting: false,
    },
  ]
}
