import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { RatingStars } from "@/components/shared/rating-stars"
import type { Reputation } from "@/lib/types"

interface ReputationCardProps {
  reputation: Reputation
}

/**
 * Snapshot of a user's reputation (US-10/11/14): points, rating, reviews,
 * completed tasks. Read-only - editing happens elsewhere.
 */
export function ReputationCard({ reputation }: ReputationCardProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-sm uppercase tracking-wide text-muted-foreground">
          Reputation
        </CardTitle>
      </CardHeader>
      <CardContent className="grid grid-cols-2 gap-4 sm:grid-cols-4">
        <Stat label="Points" value={reputation.points.toString()} />
        <Stat
          label="Rating"
          value={
            <RatingStars
              size="md"
              value={reputation.averageRating}
              reviewCount={reputation.reviewCount}
            />
          }
        />
        <Stat label="Reviews" value={reputation.reviewCount.toString()} />
        <Stat label="Completed" value={reputation.completedTasks.toString()} />
      </CardContent>
    </Card>
  )
}

function Stat({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-xs text-muted-foreground">{label}</span>
      <span className="text-base font-semibold leading-none">{value}</span>
    </div>
  )
}
