"use client"

import * as React from "react"
import { HugeiconsIcon } from "@hugeicons/react"
import { StarIcon } from "@hugeicons/core-free-icons"
import { toast } from "sonner"

import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import { Textarea } from "@/components/ui/textarea"
import { ApiError } from "@/lib/api/client"
import { useSubmitReview } from "@/lib/api/reviews"

interface ReviewDialogProps {
  taskId: string
  revieweeProfileId: string
  revieweeName: string
}

/**
 * Post-completion review modal (issue #59). Shown to the seeker or the
 * accepted helper after the task reaches Completed. Collapses to an
 * "already-reviewed" line on 409 so the submit path is not misleading.
 */
export function ReviewDialog({
  taskId,
  revieweeProfileId,
  revieweeName,
}: ReviewDialogProps) {
  const [open, setOpen] = React.useState(false)
  const [rating, setRating] = React.useState(0)
  const [hovered, setHovered] = React.useState(0)
  const [comment, setComment] = React.useState("")
  const [submitted, setSubmitted] = React.useState(false)
  const [alreadyReviewed, setAlreadyReviewed] = React.useState(false)
  const { mutate: submitReview, isPending } = useSubmitReview(
    taskId,
    revieweeProfileId
  )

  if (submitted || alreadyReviewed) {
    return (
      <p className="text-sm text-muted-foreground">
        {alreadyReviewed
          ? "You've already reviewed this task."
          : "Review submitted — thank you!"}
      </p>
    )
  }

  function handleSubmit(event: React.SyntheticEvent<HTMLFormElement>) {
    event.preventDefault()
    if (rating === 0) {
      toast.error("Please select a star rating.")
      return
    }

    submitReview(
      { rating, comment: comment.trim() || undefined },
      {
        onSuccess: () => {
          setOpen(false)
          setSubmitted(true)
          toast.success("Review submitted!")
        },
        onError: (err: unknown) => {
          // 409 from the backend means the same reviewer already posted
          // a review for this task. Surface a clear, non-generic message
          // and collapse the submit path (issue #59 AC).
          if (err instanceof ApiError && err.status === 409) {
            setOpen(false)
            setAlreadyReviewed(true)
            toast.error("You've already reviewed this task.")
            return
          }
          toast.error("Could not submit the review. Please try again.")
        },
      }
    )
  }

  const displayRating = hovered || rating

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button>Leave a review</Button>
      </DialogTrigger>
      <DialogContent className="max-w-md">
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>Leave a review</DialogTitle>
            <DialogDescription>
              How was your experience working with {revieweeName}?
            </DialogDescription>
          </DialogHeader>

          <div className="flex flex-col gap-4 py-4">
            {/* Star rating picker */}
            <div className="flex gap-1" role="radiogroup" aria-label="Rating">
              {[1, 2, 3, 4, 5].map((star) => (
                <button
                  key={star}
                  type="button"
                  role="radio"
                  aria-checked={rating === star}
                  aria-label={`${star} star${star !== 1 ? "s" : ""}`}
                  className="rounded p-0.5 focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
                  onMouseEnter={() => setHovered(star)}
                  onMouseLeave={() => setHovered(0)}
                  onClick={() => setRating(star)}
                >
                  <HugeiconsIcon
                    icon={StarIcon}
                    className={
                      star <= displayRating
                        ? "size-7 text-amber-400"
                        : "size-7 text-muted-foreground/40"
                    }
                  />
                </button>
              ))}
            </div>

            <Textarea
              value={comment}
              onChange={(e) => setComment(e.target.value)}
              maxLength={2000}
              placeholder="Share what went well or what could be improved…"
              rows={4}
            />
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => setOpen(false)}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={isPending || rating === 0}>
              {isPending ? "Submitting…" : "Submit review"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
