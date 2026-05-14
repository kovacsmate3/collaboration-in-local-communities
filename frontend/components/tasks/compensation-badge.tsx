import { Badge } from "@/components/ui/badge"
import { formatCurrency } from "@/lib/format"

interface CompensationBadgeProps {
  compensationType: string
  compensationAmount?: number | null
}

export function CompensationBadge({
  compensationType,
  compensationAmount,
}: CompensationBadgeProps) {
  const type = compensationType.toLowerCase()

  const variant =
    type === "paid" ? "success" : type === "barter" ? "warning" : "secondary"

  const label =
    type === "paid" && typeof compensationAmount === "number"
      ? formatCurrency(compensationAmount, "HUF")
      : type === "paid"
        ? "Paid"
        : type === "barter"
          ? "Barter"
          : "Voluntary"

  return <Badge variant={variant}>{label}</Badge>
}
