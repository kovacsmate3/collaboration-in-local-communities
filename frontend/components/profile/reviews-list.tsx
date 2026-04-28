import { Card, CardContent, CardHeader } from "@/components/ui/card"
import { RatingStars } from "@/components/shared/rating-stars"
import { UserAvatar } from "@/components/shared/user-avatar"
import { EmptyState } from "@/components/shared/empty-state"
import { formatRelativeTime } from "@/lib/format"
import type { Review } from "@/lib/types"

interface ReviewsListProps {
  reviews: Review[]
}

/**
 * Public review feed for a profile.
 *
 * Per US-14, written reviews are visible alongside ratings - the comment
 * is the primary content here, the star rating is supporting context.
 */
export function ReviewsList({ reviews }: ReviewsListProps) {
  if (reviews.length === 0) {
    return (
      <EmptyState
        title="No reviews yet"
        description="Reviews will appear here once tasks are completed."
      />
    )
  }

  return (
    <ul className="flex flex-col gap-3">
      {reviews.map((review) => (
        <li key={review.id}>
          <Card>
            <CardHeader>
              <div className="flex items-start gap-3">
                <UserAvatar
                  size="sm"
                  name={review.authorName}
                  src={review.authorAvatarUrl}
                />
                <div className="flex flex-1 flex-col gap-0.5">
                  <div className="flex items-center justify-between gap-2">
                    <span className="text-sm font-medium">
                      {review.authorName}
                    </span>
                    <span className="text-xs text-muted-foreground">
                      {formatRelativeTime(review.createdAt)}
                    </span>
                  </div>
                  <RatingStars value={review.rating} />
                </div>
              </div>
            </CardHeader>
            <CardContent>
              <p className="text-sm">{review.comment}</p>
            </CardContent>
          </Card>
        </li>
      ))}
    </ul>
  )
}
