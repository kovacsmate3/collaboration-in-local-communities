/**
 * Admin categories API — types and TanStack Query hooks.
 *
 * Backend controller: AdminCategoriesController
 * Base route: GET|POST /api/admin/categories
 *             GET|PUT|DELETE /api/admin/categories/{id}
 */

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { apiClient } from "@/lib/api/client"

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
  /** False when soft-deleted */
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
      void qc.invalidateQueries({ queryKey: categoryKeys.lists() })
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
      void qc.invalidateQueries({ queryKey: categoryKeys.lists() })
    },
  })
}

/**
 * Soft-deletes (deactivates) a category.
 * Endpoint: DELETE /api/admin/categories/{id}
 * Backend sets IsActive = false; returns 204 No Content.
 */
export function useDeleteCategory() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/admin/categories/${id}`),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: categoryKeys.lists() })
    },
  })
}
