import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"

import { apiClient } from "@/lib/api/client"
import { COMPLETED_TASK_WEIGHT, REVIEW_QUALITY_WEIGHT } from "@/lib/reputation"
import type {
  Reputation,
  Review,
  TaskCategory,
  TaskStatus,
  User,
} from "@/lib/types"

export interface ProfilePrivacySettingsResponse {
  showWorkplace: boolean
  showPosition: boolean
  showLocation: boolean
  showAvailability: boolean
}

export interface OwnProfileResponse {
  id: string
  userId: string
  displayName: string
  bio?: string | null
  workplace?: string | null
  position?: string | null
  availability?: string | null
  photoUrl?: string | null
  locationText?: string | null
  latitude?: number | null
  longitude?: number | null
  isProfileCompleted: boolean
  averageRating: number
  reviewCount: number
  completedTaskCount: number
  /**
   * Aggregate reputation score computed by the backend. Optional on the
   * client so older payloads or offline fixtures can fall back to
   * {@link computeReputationScore}.
   */
  reputationScore?: number
  createdAt: string
  updatedAt: string
  skillIds: string[]
  privacySettings: ProfilePrivacySettingsResponse
}

export interface PublicProfileResponse {
  id: string
  displayName: string
  bio?: string | null
  workplace?: string | null
  position?: string | null
  availability?: string | null
  photoUrl?: string | null
  locationText?: string | null
  averageRating: number
  reviewCount: number
  completedTaskCount: number
  reputationScore?: number
}

export interface UpdateOwnProfileRequest {
  displayName: string
  bio?: string
  workplace?: string
  position?: string
  availability?: string
  locationText?: string
  latitude?: number
  longitude?: number
  skillIds?: string[]
}

export interface UploadProfilePhotoResponse {
  photoUrl: string
}

export interface SkillResponse {
  id: string
  code: string
  name: string
  description?: string | null
  status: string
}

export interface ProfileReviewResponse {
  id: string
  authorId: string
  authorName: string
  authorAvatarUrl?: string | null
  targetUserId: string
  rating: number
  comment: string
  createdAt: string
}

export interface ProfileReviewPagedResponse {
  items: ProfileReviewResponse[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface ProfileReviewsPage {
  items: Review[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export const REVIEWS_PAGE_SIZE = 10

export interface ProfileTaskHistoryResponse {
  id: string
  title: string
  categoryId: string
  categoryCode: string
  categoryName: string
  categoryIcon: string
  status: string
  createdAt: string
}

export interface ProfileReputationTrendPointResponse {
  date: string
  score: number
  averageRating: number
  reviewCount: number
  completedTaskCount: number
}

export interface ProfileTaskHistoryItem {
  id: string
  title: string
  category: TaskCategory
  icon: string
  status: TaskStatus
  createdAt: string
}

export const profileKeys = {
  all: ["profiles"] as const,
  me: () => [...profileKeys.all, "me"] as const,
  public: (id: string) => [...profileKeys.all, "public", id] as const,
  reviews: (id: string) => [...profileKeys.public(id), "reviews"] as const,
  reviewsPage: (id: string, page: number, pageSize: number) =>
    [...profileKeys.reviews(id), { page, pageSize }] as const,
  taskHistory: (id: string) =>
    [...profileKeys.public(id), "task-history"] as const,
  reputationTrend: (id: string) =>
    [...profileKeys.public(id), "reputation-trend"] as const,
  skill: (id: string) => ["skills", "detail", id] as const,
  skills: (prefix: string) => ["skills", "search", prefix] as const,
}

export function useOwnProfile(enabled = true) {
  return useQuery({
    queryKey: profileKeys.me(),
    queryFn: () => apiClient.get<OwnProfileResponse>("/profiles/me"),
    enabled,
  })
}

export function usePublicProfile(id: string) {
  return useQuery({
    queryKey: profileKeys.public(id),
    queryFn: () => apiClient.get<PublicProfileResponse>(`/profiles/${id}`),
    enabled: id.length > 0,
  })
}

export function useUpdateOwnProfile() {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (data: UpdateOwnProfileRequest) =>
      apiClient.put<OwnProfileResponse>("/profiles/me", data),
    onSuccess: (profile) => {
      qc.setQueryData(profileKeys.me(), profile)
      void qc.invalidateQueries({ queryKey: profileKeys.all })
    },
  })
}

export function useUploadProfilePhoto() {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (file: File) => {
      const formData = new FormData()
      formData.append("photo", file)
      return apiClient.upload<UploadProfilePhotoResponse>(
        "/profiles/me/photo",
        formData
      )
    },
    onSuccess: (data) => {
      qc.setQueryData<OwnProfileResponse | undefined>(
        profileKeys.me(),
        (prev) => (prev ? { ...prev, photoUrl: data.photoUrl } : prev)
      )
      void qc.invalidateQueries({ queryKey: profileKeys.all })
    },
  })
}

export function useDeleteProfilePhoto() {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: () => apiClient.delete("/profiles/me/photo"),
    onSuccess: () => {
      qc.setQueryData<OwnProfileResponse | undefined>(
        profileKeys.me(),
        (prev) => (prev ? { ...prev, photoUrl: null } : prev)
      )
      void qc.invalidateQueries({ queryKey: profileKeys.all })
    },
  })
}

export function useProfileReviews(
  id: string,
  page: number = 1,
  pageSize: number = REVIEWS_PAGE_SIZE
) {
  return useQuery({
    queryKey: profileKeys.reviewsPage(id, page, pageSize),
    queryFn: async (): Promise<ProfileReviewsPage> => {
      const response = await apiClient.get<ProfileReviewPagedResponse>(
        `/profiles/${id}/reviews?page=${page}&pageSize=${pageSize}`
      )

      return {
        items: response.items.map(toReview),
        totalCount: response.totalCount,
        page: response.page,
        pageSize: response.pageSize,
        totalPages: response.totalPages,
      }
    },
    enabled: id.length > 0,
    // Reuse the previous page's data only when it belongs to the SAME profile
    // — otherwise client-side navigation between profiles briefly renders one
    // user's reviews (and totalCount) under another user's header. queryKey[2]
    // is the profile id (see profileKeys.reviewsPage).
    placeholderData: (previousData, previousQuery) =>
      previousQuery?.queryKey[2] === id ? previousData : undefined,
  })
}

export function useProfileTaskHistory(id: string) {
  return useQuery({
    queryKey: profileKeys.taskHistory(id),
    queryFn: async () => {
      const tasks = await apiClient.get<ProfileTaskHistoryResponse[]>(
        `/profiles/${id}/task-history`
      )

      return tasks.map(toProfileTaskHistoryItem)
    },
    enabled: id.length > 0,
  })
}

export function useProfileReputationTrend(id: string) {
  return useQuery({
    queryKey: profileKeys.reputationTrend(id),
    queryFn: () =>
      apiClient.get<ProfileReputationTrendPointResponse[]>(
        `/profiles/${id}/reputation-trend`
      ),
    enabled: id.length > 0,
  })
}

export function useSkills(prefix: string) {
  return useQuery({
    queryKey: profileKeys.skills(prefix),
    queryFn: () =>
      apiClient.get<SkillResponse[]>(
        `/skills?prefix=${encodeURIComponent(prefix)}`
      ),
    enabled: prefix.length > 0,
  })
}

export interface CreateSkillRequest {
  name: string
  description?: string
}

export function useCreateSkill() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateSkillRequest) =>
      apiClient.post<SkillResponse>("/skills", data),
    onSuccess: (skill) => {
      qc.setQueryData(profileKeys.skill(skill.id), skill)
    },
  })
}

export function useSkill(id: string) {
  return useQuery({
    queryKey: profileKeys.skill(id),
    queryFn: () => apiClient.get<SkillResponse>(`/skills/${id}`),
    enabled: id.length > 0,
  })
}

export function toProfileUser(
  profile: OwnProfileResponse | PublicProfileResponse,
  skills: string[] = []
): User {
  return {
    id: profile.id,
    name: profile.displayName,
    avatarUrl: profile.photoUrl ?? undefined,
    bio: profile.bio ?? undefined,
    workplace: profile.workplace ?? undefined,
    position: profile.position ?? undefined,
    availability: profile.availability ?? undefined,
    location: profile.locationText ?? undefined,
    skills,
    reputation: toReputation(profile),
    verified: false,
    joinedAt: "createdAt" in profile ? profile.createdAt : "",
  }
}

const taskCategoryByBackendCode: Record<string, TaskCategory> = {
  moving: "moving",
  tutoring: "tutoring",
  repairs: "household",
  cleaning: "household",
  pet_care: "petcare",
  shopping: "errands",
  errands: "errands",
  other: "other",
}

function toReview(review: ProfileReviewResponse): Review {
  return {
    id: review.id,
    authorId: review.authorId,
    authorName: review.authorName,
    authorAvatarUrl: review.authorAvatarUrl ?? undefined,
    targetUserId: review.targetUserId,
    rating: review.rating,
    comment: review.comment,
    createdAt: review.createdAt,
  }
}

function toProfileTaskHistoryItem(
  task: ProfileTaskHistoryResponse
): ProfileTaskHistoryItem {
  return {
    id: task.id,
    title: task.title,
    category: taskCategoryByBackendCode[task.categoryCode] ?? "other",
    icon: task.categoryIcon,
    status: toTaskStatus(task.status),
    createdAt: task.createdAt,
  }
}

function toTaskStatus(status: string): TaskStatus {
  switch (status.toLowerCase()) {
    case "open":
      return "open"
    case "inprogress":
    case "in_progress":
      return "in_progress"
    case "completed":
      return "completed"
    case "cancelled":
    case "canceled":
      return "cancelled"
    default:
      return "open"
  }
}

function toReputation(
  profile: Pick<
    OwnProfileResponse | PublicProfileResponse,
    "averageRating" | "reviewCount" | "completedTaskCount" | "reputationScore"
  >
): Reputation {
  const averageRating = Number(profile.averageRating)
  const fallbackScore = computeReputationScore({
    averageRating,
    reviewCount: profile.reviewCount,
    completedTaskCount: profile.completedTaskCount,
  })
  return {
    // Prefer the backend value when present so the client mirrors the
    // canonical formula; fall back to the local computation otherwise.
    points:
      typeof profile.reputationScore === "number" &&
      Number.isFinite(profile.reputationScore)
        ? Math.max(0, Math.round(profile.reputationScore))
        : fallbackScore,
    averageRating,
    reviewCount: profile.reviewCount,
    completedTasks: profile.completedTaskCount,
  }
}

/**
 * Aggregate reputation score derived from completed tasks weighted by review
 * quality. Mirrors `Backend.Domain.Reputation.ReputationScoreCalculator` so
 * the client can render a score offline or when an older API payload omits it.
 *
 * Formula: round(completedTasks x COMPLETED_TASK_WEIGHT
 *               + averageRating x reviewCount x REVIEW_QUALITY_WEIGHT)
 */
export function computeReputationScore(input: {
  averageRating: number
  reviewCount: number
  completedTaskCount: number
}): number {
  const rating = Number.isFinite(input.averageRating)
    ? Math.max(0, Math.min(5, input.averageRating))
    : 0
  const reviews = Math.max(0, input.reviewCount)
  const completed = Math.max(0, input.completedTaskCount)
  return Math.round(
    completed * COMPLETED_TASK_WEIGHT + rating * reviews * REVIEW_QUALITY_WEIGHT
  )
}
