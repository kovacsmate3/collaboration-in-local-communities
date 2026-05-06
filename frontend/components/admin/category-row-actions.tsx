"use client"

import { Delete02Icon, Edit02Icon } from "@hugeicons/core-free-icons"
import { HugeiconsIcon } from "@hugeicons/react"

import type { AdminCategoryResponse } from "@/lib/api/admin/categories"
import { Button } from "@/components/ui/button"
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip"

interface CategoryRowActionsProps {
  category: AdminCategoryResponse
  onEdit: (category: AdminCategoryResponse) => void
  onDelete: (category: AdminCategoryResponse) => void
}

export function CategoryRowActions({
  category,
  onEdit,
  onDelete,
}: CategoryRowActionsProps) {
  return (
    <div className="flex items-center justify-end gap-1">
      <Tooltip>
        <TooltipTrigger asChild>
          <Button
            variant="ghost"
            size="icon"
            className="size-8"
            onClick={() => onEdit(category)}
          >
            <HugeiconsIcon
              icon={Edit02Icon}
              className="size-4"
              strokeWidth={1.5}
            />
            <span className="sr-only">Edit {category.name}</span>
          </Button>
        </TooltipTrigger>
        <TooltipContent>Edit</TooltipContent>
      </Tooltip>
      <Tooltip>
        <TooltipTrigger asChild>
          <Button
            variant="ghost"
            size="icon"
            className="size-8 text-destructive hover:text-destructive"
            onClick={() => onDelete(category)}
            disabled={!category.isActive}
          >
            <HugeiconsIcon
              icon={Delete02Icon}
              className="size-4"
              strokeWidth={1.5}
            />
            <span className="sr-only">Deactivate {category.name}</span>
          </Button>
        </TooltipTrigger>
        <TooltipContent>
          {category.isActive ? "Deactivate" : "Already inactive"}
        </TooltipContent>
      </Tooltip>
    </div>
  )
}
