import { HugeiconsIcon } from "@hugeicons/react"
import { Award01Icon, TaskDone01Icon } from "@hugeicons/core-free-icons"

import { RatingStars } from "@/components/shared/rating-stars"
import { cn } from "@/lib/utils"

interface ReputationSummaryProps {
  score: number
  averageRating: number
  reviewCount: number
  completedTaskCount: number
  /** Visual density. `sm` is suitable for inline use inside cards/chips. */
  size?: "sm" | "md"
  /**
   * When `false` the score chip is hidden — handy on dense list rows where
   * stars + completion count are enough.
   */
  showScore?: boolean
  className?: string
}

const SIZES = {
  sm: {
    gap: "gap-2.5",
    chip: "gap-1 px-2 py-0.5 text-xs",
    icon: "size-3.5",
    counter: "text-xs",
  },
  md: {
    gap: "gap-3",
    chip: "gap-1.5 px-2.5 py-1 text-sm",
    icon: "size-4",
    counter: "text-sm",
  },
} as const

/**
 * Compact reputation triple — score · average rating · completed count —
 * reusable on profile cards, feed rows, helper chips, and the dashboard
 * widget. Render-only; data fetching belongs upstream.
 */
export function ReputationSummary({
  score,
  averageRating,
  reviewCount,
  completedTaskCount,
  size = "sm",
  showScore = true,
  className,
}: ReputationSummaryProps) {
  const sizeCx = SIZES[size]
  const safeScore = Number.isFinite(score) ? Math.max(0, Math.round(score)) : 0

  return (
    <div
      className={cn(
        "inline-flex flex-wrap items-center",
        sizeCx.gap,
        className
      )}
      aria-label={`Reputation score ${safeScore}, ${averageRating.toFixed(
        1
      )} stars from ${reviewCount} reviews, ${completedTaskCount} completed tasks`}
    >
      {showScore ? (
        <span
          className={cn(
            "inline-flex items-center rounded-full bg-reputation/15 font-medium text-reputation tabular-nums",
            sizeCx.chip
          )}
        >
          <HugeiconsIcon
            icon={Award01Icon}
            className={sizeCx.icon}
            strokeWidth={1.5}
          />
          {safeScore.toLocaleString()}
        </span>
      ) : null}
      <RatingStars
        size={size === "md" ? "md" : "sm"}
        value={averageRating}
        reviewCount={reviewCount}
      />
      <span
        className={cn(
          "inline-flex items-center gap-1 text-muted-foreground tabular-nums",
          sizeCx.counter
        )}
      >
        <HugeiconsIcon
          icon={TaskDone01Icon}
          className={sizeCx.icon}
          strokeWidth={1.5}
        />
        {completedTaskCount.toLocaleString()}
      </span>
    </div>
  )
}
