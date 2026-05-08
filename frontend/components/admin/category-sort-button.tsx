import type { ReactNode } from "react"
import { ArrowUpDownIcon } from "@hugeicons/core-free-icons"
import { HugeiconsIcon } from "@hugeicons/react"

interface CategorySortButtonProps {
  children: ReactNode
  sorted: false | "asc" | "desc"
  onClick: () => void
}

export function CategorySortButton({
  children,
  sorted,
  onClick,
}: CategorySortButtonProps) {
  return (
    <button
      onClick={onClick}
      className="flex items-center gap-1 transition-colors hover:text-foreground"
    >
      {children}
      <HugeiconsIcon
        icon={ArrowUpDownIcon}
        className={
          sorted ? "size-3.5 text-primary" : "size-3.5 text-muted-foreground/50"
        }
        strokeWidth={1.5}
      />
    </button>
  )
}
