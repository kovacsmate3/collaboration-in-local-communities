import { HugeiconsIcon } from "@hugeicons/react"
import { StarIcon } from "@hugeicons/core-free-icons"

import { cn } from "@/lib/utils"

interface RatingStarsProps {
  /** Rating value, 0–5. Halves are rounded to the nearest 0.5 for display. */
  value: number
  size?: "sm" | "md"
  /** Optional review count, rendered next to the stars. */
  reviewCount?: number
  className?: string
}

const SIZES = {
  sm: { icon: "size-3.5", label: "text-xs" },
  md: { icon: "size-4", label: "text-sm" },
} as const

/**
 * Read-only rating display. Kept dumb on purpose - input/edit lives in a
 * different component that posts to the backend.
 */
export function RatingStars({
  value,
  size = "sm",
  reviewCount,
  className,
}: RatingStarsProps) {
  const clamped = Math.max(0, Math.min(5, value))
  const sizeCx = SIZES[size]

  return (
    <div
      className={cn(
        "inline-flex items-center gap-1.5 text-amber-500",
        className
      )}
      aria-label={`Rated ${clamped.toFixed(1)} out of 5`}
    >
      <div className="flex items-center gap-0.5">
        {Array.from({ length: 5 }).map((_, i) => {
          const filled = i + 1 <= Math.round(clamped)
          return (
            <HugeiconsIcon
              key={i}
              icon={StarIcon}
              className={cn(
                sizeCx.icon,
                filled
                  ? "fill-current"
                  : "fill-transparent text-muted-foreground/40"
              )}
            />
          )
        })}
      </div>
      <span className={cn("font-medium text-foreground", sizeCx.label)}>
        {clamped.toFixed(1)}
      </span>
      {typeof reviewCount === "number" ? (
        <span className={cn("text-muted-foreground", sizeCx.label)}>
          ({reviewCount})
        </span>
      ) : null}
    </div>
  )
}
