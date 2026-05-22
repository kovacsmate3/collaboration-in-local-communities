import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"

import { apiClient } from "@/lib/api/client"

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
  status: "Open" | "InProgress" | "Completed" | "Cancelled"
  createdAt: string
  updatedAt: string
}

export interface ApiTaskApplication {
  id: string
  taskId: string
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
  status?: string
  cancellationReason?: string
}

export interface ApplyToTaskInput {
  message?: string
}

// ── Query keys ────────────────────────────────────────────────────────────────

export const taskKeys = {
  all: ["tasks"] as const,
  lists: () => [...taskKeys.all, "list"] as const,
  list: (filters: Record<string, string | undefined>) =>
    [...taskKeys.lists(), filters] as const,
  detail: (id: string) => [...taskKeys.all, "detail", id] as const,
  applications: (id: string) =>
    [...taskKeys.detail(id), "applications"] as const,
  myApplications: () => [...taskKeys.all, "my-applications"] as const,
}

// ── Hooks ─────────────────────────────────────────────────────────────────────

export function useTaskList(
  filters: { status?: string; categoryId?: string } = {}
) {
  const params = new URLSearchParams()
  if (filters.status) params.set("status", filters.status)
  if (filters.categoryId) params.set("categoryId", filters.categoryId)
  const qs = params.size > 0 ? `?${params.toString()}` : ""

  return useQuery({
    queryKey: taskKeys.list(filters as Record<string, string | undefined>),
    queryFn: () => apiClient.get<ApiTask[]>(`/tasks${qs}`),
  })
}

export function useTask(id: string) {
  return useQuery({
    queryKey: taskKeys.detail(id),
    queryFn: () => apiClient.get<ApiTask>(`/tasks/${id}`),
    enabled: Boolean(id),
  })
}

export function useCreateTask() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateTaskInput) =>
      apiClient.post<ApiTask>("/tasks", data),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: taskKeys.lists() })
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
      void qc.invalidateQueries({ queryKey: taskKeys.lists() })
    },
  })
}

export function useTaskApplications(taskId: string, enabled = true) {
  return useQuery({
    queryKey: taskKeys.applications(taskId),
    queryFn: () =>
      apiClient.get<ApiTaskApplication[]>(`/tasks/${taskId}/applications`),
    enabled: Boolean(taskId) && enabled,
    staleTime: 5 * 60 * 1000,
  })
}

export function useMyTaskApplications() {
  return useQuery({
    queryKey: taskKeys.myApplications(),
    queryFn: () =>
      apiClient.get<ApiMyTaskApplication[]>("/task-applications/me"),
    staleTime: 5 * 60 * 1000,
  })
}

export function useApplyToTask(taskId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: ApplyToTaskInput) =>
      apiClient.post<ApiTaskApplication>(`/tasks/${taskId}/applications`, data),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: taskKeys.myApplications() })
      void qc.invalidateQueries({ queryKey: taskKeys.applications(taskId) })
      void qc.invalidateQueries({ queryKey: taskKeys.detail(taskId) })
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
      void qc.invalidateQueries({ queryKey: taskKeys.applications(taskId) })
      void qc.invalidateQueries({ queryKey: taskKeys.myApplications() })
      void qc.invalidateQueries({ queryKey: taskKeys.detail(taskId) })
      void qc.invalidateQueries({ queryKey: taskKeys.lists() })
    },
  })
}

export function useWithdrawTaskApplication(taskId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (applicationId: string) =>
      apiClient.delete<void>(`/tasks/${taskId}/applications/${applicationId}`),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: taskKeys.myApplications() })
      void qc.invalidateQueries({ queryKey: taskKeys.applications(taskId) })
    },
  })
}
