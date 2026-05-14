import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { apiClient } from "@/lib/api/client"

export interface AdminUserResponse {
  id: string
  email: string
  emailConfirmed: boolean
  displayName?: string
  photoUrl?: string
  isProfileCompleted: boolean
  roles: string[]
  joinedAt?: string
}

export interface AdminUserPagedResponse {
  items: AdminUserResponse[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface AdminUsersParams {
  page?: number
  pageSize?: number
  search?: string
  role?: string
}

export const userKeys = {
  all: ["admin", "users"] as const,
  lists: () => [...userKeys.all, "list"] as const,
  list: (params: AdminUsersParams) => [...userKeys.lists(), params] as const,
}

export function useAdminUsers(params: AdminUsersParams = {}) {
  return useQuery({
    queryKey: userKeys.list(params),
    queryFn: () => {
      const search = new URLSearchParams()
      if (params.page) search.set("page", String(params.page))
      if (params.pageSize) search.set("pageSize", String(params.pageSize))
      if (params.search) search.set("search", params.search)
      if (params.role) search.set("role", params.role)
      const qs = search.toString()
      return apiClient.get<AdminUserPagedResponse>(
        `/admin/users${qs ? `?${qs}` : ""}`
      )
    },
  })
}

export function useMakeAdmin() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (userId: string) =>
      apiClient.post<AdminUserResponse>(
        `/admin/users/${userId}/make-admin`,
        {}
      ),
    onSuccess: () => void qc.invalidateQueries({ queryKey: userKeys.lists() }),
  })
}

export function useRevokeAdmin() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (userId: string) =>
      apiClient.delete<AdminUserResponse>(`/admin/users/${userId}/make-admin`),
    onSuccess: () => void qc.invalidateQueries({ queryKey: userKeys.lists() }),
  })
}
