import { HugeiconsIcon } from "@hugeicons/react"

import { Badge } from "@/components/ui/badge"
import { getIconForKey } from "@/lib/icon-registry"

interface CategoryBadgeProps {
  label: string
  icon?: string
}

export function CategoryBadge({ label, icon }: CategoryBadgeProps) {
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
      {label}
    </Badge>
  )
}
