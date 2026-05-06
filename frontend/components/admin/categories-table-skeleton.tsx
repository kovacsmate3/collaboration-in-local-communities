import { Skeleton } from "@/components/ui/skeleton"
import { TableCell, TableRow } from "@/components/ui/table"

export function CategoriesTableSkeleton() {
  return Array.from({ length: 5 }, (_, index) => (
    <TableRow key={`category-skeleton-${index}`}>
      <TableCell className="pl-4">
        <Skeleton className="size-5 rounded" />
      </TableCell>
      <TableCell>
        <Skeleton className="h-4 w-32" />
      </TableCell>
      <TableCell>
        <Skeleton className="h-4 w-20" />
      </TableCell>
      <TableCell>
        <Skeleton className="h-4 w-8" />
      </TableCell>
      <TableCell>
        <Skeleton className="h-5 w-14 rounded-full" />
      </TableCell>
      <TableCell className="pr-4" />
    </TableRow>
  ))
}
