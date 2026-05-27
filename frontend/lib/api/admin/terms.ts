/**
 * Admin terms API — types and TanStack Query hooks.
 *
 * Backend controller: AdminTermsController
 * Base route: GET|POST /api/admin/terms
 *             GET|PUT|DELETE /api/admin/terms/{id}
 *             POST           /api/admin/terms/{id}/publish
 */

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"

import { apiClient } from "@/lib/api/client"

// ── Types ─────────────────────────────────────────────────────────────────────

export interface AdminTermsVersionListItem {
  id: string
  version: string
  majorVersion: number
  minorVersion: number
  patchVersion: number
  title: string
  isActive: boolean
  publishedAt: string | null
  effectiveFrom: string
  createdAt: string
  acceptanceCount: number
}

export interface AdminTermsVersionDetail extends AdminTermsVersionListItem {
  content: string | null
  contentUrl: string | null
  updatedAt: string
}

export interface CreateTermsVersionRequest {
  version: string
  title: string
  content?: string
  contentUrl?: string
  effectiveFrom: string
}

export interface UpdateTermsVersionRequest {
  version: string
  title: string
  content?: string
  contentUrl?: string
  effectiveFrom: string
}

// ── Query keys ────────────────────────────────────────────────────────────────

export const termsKeys = {
  all: ["admin", "terms"] as const,
  lists: () => [...termsKeys.all, "list"] as const,
  detail: (id: string) => [...termsKeys.all, "detail", id] as const,
}

// ── Hooks ─────────────────────────────────────────────────────────────────────

export function useAdminTermsList() {
  return useQuery({
    queryKey: termsKeys.lists(),
    queryFn: () => apiClient.get<AdminTermsVersionListItem[]>("/admin/terms"),
  })
}

export function useAdminTermsById(id: string) {
  return useQuery({
    queryKey: termsKeys.detail(id),
    queryFn: () => apiClient.get<AdminTermsVersionDetail>(`/admin/terms/${id}`),
    enabled: Boolean(id),
  })
}

export function useCreateTermsVersion() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateTermsVersionRequest) =>
      apiClient.post<AdminTermsVersionDetail>("/admin/terms", data),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: termsKeys.lists() })
    },
  })
}

export function useUpdateTermsVersion(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: UpdateTermsVersionRequest) =>
      apiClient.put<AdminTermsVersionDetail>(`/admin/terms/${id}`, data),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: termsKeys.lists() })
      void qc.invalidateQueries({ queryKey: termsKeys.detail(id) })
    },
  })
}

export function usePublishTermsVersion() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) =>
      apiClient.post<AdminTermsVersionDetail>(`/admin/terms/${id}/publish`, {}),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: termsKeys.all })
    },
  })
}

export function useDeleteTermsVersion() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => apiClient.delete<void>(`/admin/terms/${id}`),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: termsKeys.lists() })
    },
  })
}
