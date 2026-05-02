import { Badge } from "@/components/ui/badge"
import { TASK_CATEGORIES } from "@/lib/constants"
import type { TaskCategory } from "@/lib/types"

interface CategoryBadgeProps {
  category: TaskCategory
}

/**
 * Visible label for a task's category.
 *
 */
export function CategoryBadge({ category }: CategoryBadgeProps) {
  return <Badge variant="muted">{TASK_CATEGORIES[category].label}</Badge>
}
