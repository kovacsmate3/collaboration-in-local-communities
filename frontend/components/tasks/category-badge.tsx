import { Badge } from "@/components/ui/badge"
import { TASK_CATEGORIES } from "@/lib/constants"
import type { TaskCategory } from "@/lib/types"

interface CategoryBadgeProps {
  category: TaskCategory
}

export function CategoryBadge({ category }: CategoryBadgeProps) {
  const meta = TASK_CATEGORIES[category]
  return (
    <Badge variant="muted" aria-label={`Category: ${meta.label}`}>
      {meta.label}
    </Badge>
  )
}
