import { useMutation, useQueryClient } from "@tanstack/react-query"

import { apiClient } from "@/lib/api/client"
import { profileKeys } from "@/lib/api/profile"
import { taskKeys } from "@/lib/api/tasks"

// ── Types ─────────────────────────────────────────────────────────────────────

export interface SubmitReviewInput {
  rating: number
  comment?: string
}

/**
 * Server response for a successful review submission.
 * The internal task linkage is intentionally not exposed (issue #115 privacy).
 */
export interface ApiReviewResponse {
  id: string
  authorId: string
  authorName: string
  authorAvatarUrl?: string | null
  targetUserId: string
  rating: number
  comment: string
  createdAt: string
}

// ── Hooks ─────────────────────────────────────────────────────────────────────

/**
 * Submit a review for the other participant in a completed task.
 *
 * @param taskId - The completed task being reviewed.
 * @param revieweeProfileId - The profile ID of the person being reviewed (used
 *   to invalidate their public profile and reviews caches after success).
 */
export function useSubmitReview(taskId: string, revieweeProfileId: string) {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (data: SubmitReviewInput) =>
      apiClient.post<ApiReviewResponse>(`/tasks/${taskId}/reviews`, data),
    onSuccess: () => {
      // Refresh the reviewee's public profile (AverageRating / ReviewCount changed)
      void qc.invalidateQueries({
        queryKey: profileKeys.public(revieweeProfileId),
      })
      // Refresh the reviewee's reviews list
      void qc.invalidateQueries({
        queryKey: profileKeys.reviews(revieweeProfileId),
      })
      // Refresh the task itself (status may change in the future; keeps data fresh)
      void qc.invalidateQueries({ queryKey: taskKeys.detail(taskId) })
    },
  })
}
