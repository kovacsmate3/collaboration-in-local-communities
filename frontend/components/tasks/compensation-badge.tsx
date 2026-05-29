import { Badge } from "@/components/ui/badge"
import { COMPENSATION_LABELS } from "@/lib/constants"
import { formatCurrency } from "@/lib/format"
import type { CompensationType } from "@/lib/types"

interface CompensationBadgeProps {
  compensationType: string
  compensationAmount?: number | null
}

const VARIANT_BY_TYPE: Record<
  CompensationType,
  "success" | "warning" | "secondary"
> = {
  paid: "success",
  points: "secondary",
  barter: "warning",
  voluntary: "secondary",
}

function normalizeCompensationType(value: string): CompensationType {
  const lower = value.toLowerCase()
  if (
    lower === "paid" ||
    lower === "points" ||
    lower === "barter" ||
    lower === "voluntary"
  ) {
    return lower
  }
  // Fallback for any unexpected value keeps the previous secondary-styled badge.
  return "voluntary"
}

export function CompensationBadge({
  compensationType,
  compensationAmount,
}: CompensationBadgeProps) {
  const type = normalizeCompensationType(compensationType)
  const variant = VARIANT_BY_TYPE[type]

  const label =
    type === "paid" && typeof compensationAmount === "number"
      ? `${formatCurrency(compensationAmount, "HUF")} + points`
      : COMPENSATION_LABELS[type]

  return <Badge variant={variant}>{label}</Badge>
}
