import { Badge } from "@/components/ui/badge"
import { TASK_CATEGORIES } from "@/lib/constants"
import type { TaskCategory } from "@/lib/types"

interface CategoryBadgeProps {
  category: TaskCategory
}

export function CategoryBadge({ category }: CategoryBadgeProps) {
  return <Badge variant="muted">{TASK_CATEGORIES[category].label}</Badge>
}
