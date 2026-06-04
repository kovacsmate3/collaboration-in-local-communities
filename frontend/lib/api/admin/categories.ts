/**
 * Admin categories API — types and TanStack Query hooks.
 *
 * Backend controller: AdminCategoriesController
 * Base route: GET|POST /api/admin/categories
 *             GET|PUT|DELETE /api/admin/categories/{id}
 *             POST          /api/admin/categories/{id}/deactivate
 *             POST          /api/admin/categories/{id}/activate
 */

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { apiClient } from "@/lib/api/client"
import { invalidateCategoryData } from "@/lib/api/query-invalidation"

// ── Types (mirror AdminCategoriesController DTOs) ────────────────────────────

/** Shape returned by every admin category endpoint */
export interface AdminCategoryResponse {
  id: string
  /** Immutable slug, set at creation — max 64 chars */
  code: string
  /** Display name — max 120 chars */
  name: string
  /** HugeIcons identifier from AllowedCategoryIcons — max 64 chars */
  icon: string
  /** Optional description — max 500 chars */
  description?: string
  /** Controls render order */
  sortOrder: number
  /** False when deactivated; can be flipped via the activate/deactivate endpoints */
  isActive: boolean
}

/** POST /api/admin/categories */
export interface CreateCategoryRequest {
  code: string
  name: string
  icon: string
  description?: string
  sortOrder: number
}

/** PUT /api/admin/categories/{id} — code is immutable, excluded */
export interface UpdateCategoryRequest {
  name: string
  icon: string
  description?: string
  sortOrder: number
}

// ── Query keys ───────────────────────────────────────────────────────────────

export const categoryKeys = {
  all: ["admin", "categories"] as const,
  lists: () => [...categoryKeys.all, "list"] as const,
  detail: (id: string) => [...categoryKeys.all, "detail", id] as const,
}

// ── Hooks ────────────────────────────────────────────────────────────────────

/**
 * Fetches the full list of admin categories (active + inactive).
 * Endpoint: GET /api/admin/categories
 */
export function useAdminCategories() {
  return useQuery({
    queryKey: categoryKeys.lists(),
    queryFn: () => apiClient.get<AdminCategoryResponse[]>("/admin/categories"),
  })
}

/**
 * Creates a new category.
 * Endpoint: POST /api/admin/categories
 * Returns 201 with the created category. 409 if code already exists.
 */
export function useCreateCategory() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateCategoryRequest) =>
      apiClient.post<AdminCategoryResponse>("/admin/categories", data),
    onSuccess: () => {
      invalidateCategoryData(qc)
    },
  })
}

/**
 * Updates an existing category. `code` is never sent — it is immutable.
 * Endpoint: PUT /api/admin/categories/{id}
 */
export function useUpdateCategory() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateCategoryRequest }) =>
      apiClient.put<AdminCategoryResponse>(`/admin/categories/${id}`, data),
    onSuccess: () => {
      invalidateCategoryData(qc)
    },
  })
}

/**
 * Permanently deletes a category.
 * Endpoint: DELETE /api/admin/categories/{id}
 *
 * Returns 204 on success. Returns 409 if any task still references the
 * category (FK is configured with OnDelete(Restrict)) — in that case,
 * callers should deactivate instead.
 */
export function useDeleteCategory() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/admin/categories/${id}`),
    onSuccess: () => {
      invalidateCategoryData(qc)
    },
  })
}

/**
 * Deactivates (soft-deletes) a category. Idempotent.
 * Endpoint: POST /api/admin/categories/{id}/deactivate
 * Backend sets IsActive = false; returns 204 No Content.
 */
export function useDeactivateCategory() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) =>
      apiClient.post<void>(`/admin/categories/${id}/deactivate`, {}),
    onSuccess: () => {
      invalidateCategoryData(qc)
    },
  })
}

/**
 * Reactivates a previously deactivated category. Idempotent.
 * Endpoint: POST /api/admin/categories/{id}/activate
 * Backend sets IsActive = true; returns 204 No Content.
 */
export function useActivateCategory() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) =>
      apiClient.post<void>(`/admin/categories/${id}/activate`, {}),
    onSuccess: () => {
      invalidateCategoryData(qc)
    },
  })
}
