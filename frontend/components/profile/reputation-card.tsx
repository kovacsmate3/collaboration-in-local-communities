import type { ReactNode } from "react"

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { RatingStars } from "@/components/shared/rating-stars"

interface ReputationCardProps {
  averageRating: number
  reviewCount: number
  completedTaskCount: number
}

export function ReputationCard({
  averageRating,
  reviewCount,
  completedTaskCount,
}: ReputationCardProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-sm tracking-wide text-muted-foreground uppercase">
          Reputation
        </CardTitle>
      </CardHeader>
      <CardContent className="grid grid-cols-2 gap-4 sm:grid-cols-3">
        <Stat
          label="Rating"
          value={
            <RatingStars
              size="md"
              value={averageRating}
              reviewCount={reviewCount}
            />
          }
        />
        <Stat label="Reviews" value={reviewCount.toString()} />
        <Stat label="Completed" value={completedTaskCount.toString()} />
      </CardContent>
    </Card>
  )
}

function Stat({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-xs text-muted-foreground">{label}</span>
      <span className="text-base leading-none font-semibold">{value}</span>
    </div>
  )
}
