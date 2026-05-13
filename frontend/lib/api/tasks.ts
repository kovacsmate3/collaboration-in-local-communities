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
  status: string
  createdAt: string
  updatedAt: string
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

// ── Query keys ────────────────────────────────────────────────────────────────

export const taskKeys = {
  all: ["tasks"] as const,
  lists: () => [...taskKeys.all, "list"] as const,
  list: (filters: Record<string, string | undefined>) =>
    [...taskKeys.lists(), filters] as const,
  detail: (id: string) => [...taskKeys.all, "detail", id] as const,
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
