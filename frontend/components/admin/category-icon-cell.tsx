import { InformationCircleIcon } from "@hugeicons/core-free-icons"
import { HugeiconsIcon } from "@hugeicons/react"

import { ICON_REGISTRY } from "@/lib/icon-registry"
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip"

interface CategoryIconCellProps {
  iconName: string
}

export function CategoryIconCell({ iconName }: CategoryIconCellProps) {
  const icon = ICON_REGISTRY[iconName as keyof typeof ICON_REGISTRY]

  if (!icon) {
    return (
      <Tooltip>
        <TooltipTrigger asChild>
          <span className="flex items-center gap-1.5 text-xs text-muted-foreground">
            <HugeiconsIcon
              icon={InformationCircleIcon}
              className="size-4"
              strokeWidth={1.5}
            />
            Unknown
          </span>
        </TooltipTrigger>
        <TooltipContent>
          Icon &quot;{iconName}&quot; not in registry
        </TooltipContent>
      </Tooltip>
    )
  }

  return (
    <span className="flex items-center gap-2">
      <HugeiconsIcon icon={icon} className="size-5" strokeWidth={1.5} />
      <span className="hidden text-xs text-muted-foreground xl:inline">
        {iconName}
      </span>
    </span>
  )
}
