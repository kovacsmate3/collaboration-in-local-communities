import { Badge } from "@/components/ui/badge"
import { COMPENSATION_LABELS } from "@/lib/constants"
import { formatCurrency } from "@/lib/format"
import type { Compensation } from "@/lib/types"

interface CompensationBadgeProps {
  compensation: Compensation
}

/**
 * Visual marker for the compensation type of a task.
 *
 * Per US-12 (compensation transparency), this badge is always shown next
 * to a task title so seekers/helpers know the deal up front.
 */
export function CompensationBadge({ compensation }: CompensationBadgeProps) {
  const variant =
    compensation.type === "paid"
      ? "success"
      : compensation.type === "barter"
        ? "warning"
        : "secondary"

  const label =
    compensation.type === "paid" && typeof compensation.amount === "number"
      ? formatCurrency(compensation.amount, compensation.currency)
      : COMPENSATION_LABELS[compensation.type]

  const ariaLabel =
    compensation.type === "barter" && compensation.barterOffer
      ? `Barter offered: ${compensation.barterOffer}`
      : compensation.type === "paid" && typeof compensation.amount === "number"
        ? `Paid: ${formatCurrency(compensation.amount, compensation.currency)}`
        : `Compensation: ${COMPENSATION_LABELS[compensation.type]}`

  return (
    <Badge variant={variant} aria-label={ariaLabel}>
      {label}
    </Badge>
  )
}
