import { HugeiconsIcon } from "@hugeicons/react"

import { Badge } from "@/components/ui/badge"
import { TASK_CATEGORIES } from "@/lib/constants"
import { getIconForKey } from "@/lib/icon-registry"
import type { TaskCategory } from "@/lib/types"

interface CategoryBadgeProps {
  category: TaskCategory
  icon?: string
}

/**
 * Visible label for a task's category.
 *
 */
export function CategoryBadge({ category, icon }: CategoryBadgeProps) {
  const Icon = icon ? getIconForKey(icon) : null

  return (
    <Badge variant="muted">
      {Icon ? (
        <HugeiconsIcon
          icon={Icon}
          data-icon="inline-start"
          aria-hidden="true"
        />
      ) : null}
      {TASK_CATEGORIES[category].label}
    </Badge>
  )
}
