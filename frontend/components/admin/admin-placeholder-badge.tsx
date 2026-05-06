import { InformationCircleIcon } from "@hugeicons/core-free-icons"
import { HugeiconsIcon } from "@hugeicons/react"

import { Badge } from "@/components/ui/badge"

export function AdminPlaceholderBadge() {
  return (
    <Badge variant="outline" className="gap-1 text-[10px]">
      <HugeiconsIcon
        icon={InformationCircleIcon}
        className="size-3"
        strokeWidth={1.5}
      />
      Placeholder data
    </Badge>
  )
}
