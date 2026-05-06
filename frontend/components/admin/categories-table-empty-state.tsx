import { TableCell, TableRow } from "@/components/ui/table"

interface CategoriesTableEmptyStateProps {
  totalCount: number
  columnCount: number
}

export function CategoriesTableEmptyState({
  totalCount,
  columnCount,
}: CategoriesTableEmptyStateProps) {
  return (
    <TableRow>
      <TableCell
        colSpan={columnCount}
        className="h-32 text-center text-muted-foreground"
      >
        {totalCount === 0
          ? "No categories yet - create one to get started."
          : "No categories match your filter."}
      </TableCell>
    </TableRow>
  )
}
