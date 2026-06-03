import {
  useInfiniteQuery,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query"

import { apiClient } from "@/lib/api/client"
import {
  invalidateConversationData,
  invalidateTaskData,
  invalidateTaskParticipantData,
} from "@/lib/api/query-invalidation"

// ── Types ─────────────────────────────────────────────────────────────────────

export interface ApiTask {
  id: string
  publicCode: string
  seekerProfileId: string
  seekerDisplayName: string
  acceptedHelperProfileId: string | null
  acceptedHelperDisplayName: string | null
  categoryId: string
  categoryName: string
  categoryIcon: string
  title: string
  description: string
  locationText: string | null
  latitude: number | null
  longitude: number | null
  compensationType: string
  compensationAmount: number | null
  status: "Open" | "InProgress" | "PendingApproval" | "Completed" | "Cancelled"
  createdAt: string
  updatedAt: string
}

export interface ApiTaskApplication {
  id: string
  taskId: string
  conversationId: string | null
  helperProfileId: string
  helperDisplayName: string
  message: string | null
  status: "Pending" | "Accepted" | "Rejected" | "Withdrawn"
  createdAt: string
  updatedAt: string
}

export interface ApiMyTaskApplication extends ApiTaskApplication {
  task: ApiTask
}

export interface CreateTaskInput {
  title: string
  description: string
  categoryId: string
  compensationType: string
  compensationAmount?: number
  locationText?: string
  latitude?: number
  longitude?: number
}

export interface UpdateTaskInput {
  title?: string
  description?: string
  categoryId?: string
  compensationType?: string
  compensationAmount?: number
  locationText?: string
  latitude?: number
  longitude?: number
  status?: string
  cancellationReason?: string
}

export interface ApplyToTaskInput {
  message?: string
}

export interface TaskListFilters {
  status?: string
  categoryId?: string
  /** Backend `CompensationType` enum name only (case-insensitive; numeric values are not accepted). */
  compensationType?: string
  /** ISO 8601 timestamp; tasks with `createdAt >= createdAfter` are returned. */
  createdAfter?: string
  latitude?: number
  longitude?: number
  radiusMeters?: number
  /** "relevant" orders by proximity to the caller's profile location; omit for newest-first. */
  sort?: "relevant" | "recency"
}

// ── Query keys ────────────────────────────────────────────────────────────────

type TaskListFiltersKey = Record<string, string | number | undefined>

export const taskKeys = {
  all: ["tasks"] as const,
  lists: () => [...taskKeys.all, "list"] as const,
  list: (filters: TaskListFiltersKey) =>
    [...taskKeys.lists(), filters] as const,
  infiniteList: (filters: TaskListFiltersKey, pageSize: number) =>
    [...taskKeys.list(filters), "infinite", pageSize] as const,
  detail: (id: string) => [...taskKeys.all, "detail", id] as const,
  applications: (id: string) =>
    [...taskKeys.detail(id), "applications"] as const,
  myApplications: () => [...taskKeys.all, "my-applications"] as const,
}

// ── Hooks ─────────────────────────────────────────────────────────────────────

export function useTaskList(filters: TaskListFilters = {}) {
  return useQuery({
    queryKey: taskKeys.list(filters as TaskListFiltersKey),
    queryFn: () => apiClient.get<ApiTask[]>(buildTaskListPath(filters)),
  })
}

export function useInfiniteTaskList(
  filters: TaskListFilters = {},
  pageSize = 20
) {
  return useInfiniteQuery({
    queryKey: taskKeys.infiniteList(filters as TaskListFiltersKey, pageSize),
    initialPageParam: 1,
    queryFn: ({ pageParam }) =>
      apiClient.get<ApiTask[]>(
        buildTaskListPath(filters, { page: pageParam, pageSize })
      ),
    // The API returns a plain array, so a full final page intentionally causes
    // one extra empty-page fetch before pagination stops.
    getNextPageParam: (lastPage, _pages, lastPageParam) =>
      lastPage.length < pageSize ? undefined : lastPageParam + 1,
  })
}

export function useTask(id: string) {
  return useQuery({
    queryKey: taskKeys.detail(id),
    queryFn: () => apiClient.get<ApiTask>(`/tasks/${id}`),
    enabled: Boolean(id),
    // Polling fallback so a task's status (Open → InProgress → PendingApproval
    // → Completed) converges for the other party on hosts where SignalR/cache
    // invalidation across users doesn't propagate immediately on deploy.
    refetchInterval: (query) => {
      const status = query.state.data?.status
      return status === "Completed" || status === "Cancelled" ? false : 10_000
    },
  })
}

export function useCreateTask() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateTaskInput) =>
      apiClient.post<ApiTask>("/tasks", data),
    onSuccess: () => {
      invalidateTaskData(qc)
    },
  })
}

export function useUpdateTask(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: UpdateTaskInput) =>
      apiClient.patch<ApiTask>(`/tasks/${id}`, data),
    onSuccess: (updated) => {
      qc.setQueryData(taskKeys.detail(id), updated)
      invalidateTaskData(qc)
    },
  })
}

export function useTaskApplications(taskId: string, enabled = true) {
  return useQuery({
    queryKey: taskKeys.applications(taskId),
    queryFn: () =>
      apiClient.get<ApiTaskApplication[]>(`/tasks/${taskId}/applications`),
    enabled: Boolean(taskId) && enabled,
    staleTime: 15_000,
    // New applications from helpers should surface for the seeker without a
    // hard refresh, so poll while the applications panel is mounted.
    refetchInterval: 15_000,
  })
}

export function useMyTaskApplications() {
  return useQuery({
    queryKey: taskKeys.myApplications(),
    queryFn: () =>
      apiClient.get<ApiMyTaskApplication[]>("/task-applications/me"),
    staleTime: 15_000,
    // Reflect accept/reject decisions made by the seeker on the helper's side
    // without requiring a manual refresh on deploy.
    refetchInterval: 15_000,
  })
}

export function useApplyToTask(taskId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: ApplyToTaskInput) =>
      apiClient.post<ApiTaskApplication>(`/tasks/${taskId}/applications`, data),
    onSuccess: () => {
      invalidateTaskData(qc)
      invalidateConversationData(qc)
    },
  })
}

export function usePatchTaskApplication(taskId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({
      applicationId,
      action,
    }: {
      applicationId: string
      action: "accept" | "reject"
    }) =>
      apiClient.patch<ApiTaskApplication>(
        `/tasks/${taskId}/applications/${applicationId}`,
        { action }
      ),
    onSuccess: () => {
      invalidateTaskData(qc)
      invalidateConversationData(qc)
    },
  })
}

export function useSubmitTaskCompletion(taskId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: () =>
      apiClient.post<ApiTask>(`/tasks/${taskId}/completion-submission`, {}),
    onSuccess: (updated) => {
      qc.setQueryData(taskKeys.detail(taskId), updated)
      invalidateTaskData(qc)
      invalidateConversationData(qc)
    },
  })
}

export function useApproveTaskCompletion(taskId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: () =>
      apiClient.post<ApiTask>(`/tasks/${taskId}/completion-approval`, {}),
    onSuccess: (updated) => {
      qc.setQueryData(taskKeys.detail(taskId), updated)
      invalidateTaskParticipantData(qc)
      invalidateConversationData(qc)
    },
  })
}

export function useWithdrawTaskApplication(taskId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (applicationId: string) =>
      apiClient.delete<void>(`/tasks/${taskId}/applications/${applicationId}`),
    onSuccess: () => {
      invalidateTaskData(qc)
      invalidateConversationData(qc)
    },
  })
}

function buildTaskListPath(
  filters: TaskListFilters,
  pagination?: { page: number; pageSize: number }
) {
  const params = new URLSearchParams()
  if (filters.status) params.set("status", filters.status)
  if (filters.categoryId) params.set("categoryId", filters.categoryId)
  if (filters.compensationType) {
    params.set("compensationType", filters.compensationType)
  }
  if (filters.createdAfter) params.set("createdAfter", filters.createdAfter)
  // Proximity is a triplet on the backend (latitude + longitude + radiusMeters).
  // Sending a partial triplet returns a 400, so only forward when all three are
  // present and finite.
  if (
    typeof filters.latitude === "number" &&
    Number.isFinite(filters.latitude) &&
    typeof filters.longitude === "number" &&
    Number.isFinite(filters.longitude) &&
    typeof filters.radiusMeters === "number" &&
    Number.isFinite(filters.radiusMeters) &&
    filters.radiusMeters > 0
  ) {
    params.set("latitude", String(filters.latitude))
    params.set("longitude", String(filters.longitude))
    params.set("radiusMeters", String(filters.radiusMeters))
  }
  if (filters.sort) params.set("sort", filters.sort)
  if (pagination) {
    params.set("page", String(pagination.page))
    params.set("pageSize", String(pagination.pageSize))
  }

  const qs = params.size > 0 ? `?${params.toString()}` : ""
  return `/tasks${qs}`
}
