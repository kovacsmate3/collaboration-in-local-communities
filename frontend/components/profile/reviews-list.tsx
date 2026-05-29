import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader } from "@/components/ui/card"
import { RatingStars } from "@/components/shared/rating-stars"
import { UserAvatar } from "@/components/shared/user-avatar"
import { EmptyState } from "@/components/shared/empty-state"
import { formatRelativeTime } from "@/lib/format"
import type { Review } from "@/lib/types"

interface ReviewsListProps {
  reviews: Review[]
  /** Optional pagination controls — omit for unpaginated rendering. */
  page?: number
  totalPages?: number
  onPageChange?: (page: number) => void
  isFetching?: boolean
}

/**
 * Public review feed for a profile.
 *
 * Per US-14 / issue #62, written reviews are visible alongside ratings, and
 * the list is paginated when paging props are supplied. The card intentionally
 * does not display task title or ID (issue #115 privacy).
 */
export function ReviewsList({
  reviews,
  page,
  totalPages,
  onPageChange,
  isFetching,
}: ReviewsListProps) {
  if (reviews.length === 0) {
    return (
      <EmptyState
        title="No reviews yet"
        description="Reviews will appear here once tasks are completed."
      />
    )
  }

  const showPager =
    typeof page === "number" &&
    typeof totalPages === "number" &&
    typeof onPageChange === "function" &&
    totalPages > 1

  return (
    <div className="flex flex-col gap-4">
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

      {showPager ? (
        <div className="flex items-center justify-between gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={page <= 1 || isFetching}
            onClick={() => onPageChange(page - 1)}
          >
            Previous
          </Button>
          <span className="text-xs text-muted-foreground">
            Page {page} of {totalPages}
          </span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={page >= totalPages || isFetching}
            onClick={() => onPageChange(page + 1)}
          >
            Next
          </Button>
        </div>
      ) : null}
    </div>
  )
}
