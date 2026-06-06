import { useMutation, useQueryClient } from "@tanstack/react-query"

import { apiClient } from "@/lib/api/client"
import {
  invalidateProfileData,
  invalidateTaskData,
} from "@/lib/api/query-invalidation"

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
 * @param _revieweeProfileId - The profile ID of the person being reviewed.
 *   Kept at the call site for clarity; success invalidates all profile data
 *   because reviews also affect reputation and trend queries.
 */
export function useSubmitReview(taskId: string, _revieweeProfileId: string) {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (data: SubmitReviewInput) =>
      apiClient.post<ApiReviewResponse>(`/tasks/${taskId}/reviews`, data),
    onSuccess: () => {
      invalidateProfileData(qc)
      invalidateTaskData(qc)
    },
  })
}
