/**
 * Admin skills API — types and TanStack Query hooks.
 *
 * Backend controller: AdminSkillsController
 * Base route: GET    /api/admin/skills
 *             PATCH  /api/admin/skills/{id}  body: { action: "Approve" | "Deactivate" | "Activate" }
 *             DELETE /api/admin/skills/{id}
 */

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { apiClient } from "@/lib/api/client"

// ── Types (mirror AdminSkillsController DTOs) ─────────────────────────────────

/** Shape returned by every admin skill endpoint */
export interface AdminSkillResponse {
  id: string
  code: string
  name: string
  description?: string
  isActive: boolean
  status: "Pending" | "Approved"
  approvedAt?: string
  createdAt: string
  updatedAt: string
}

/** Shape returned by GET /api/admin/skills */
export interface AdminSkillPagedResponse {
  items: AdminSkillResponse[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface AdminSkillsParams {
  page?: number
  pageSize?: number
  /** "Pending" | "Approved" | undefined (all) */
  status?: string
}

// ── Query keys ────────────────────────────────────────────────────────────────

export const skillKeys = {
  all: ["admin", "skills"] as const,
  lists: () => [...skillKeys.all, "list"] as const,
  list: (params: AdminSkillsParams) => [...skillKeys.lists(), params] as const,
}

// ── Hooks ─────────────────────────────────────────────────────────────────────

/**
 * Fetches a paginated list of admin skills.
 * Endpoint: GET /api/admin/skills
 */
export function useAdminSkills(params: AdminSkillsParams = {}) {
  return useQuery({
    queryKey: skillKeys.list(params),
    queryFn: () => {
      const qs = new URLSearchParams()
      if (params.page) qs.set("page", String(params.page))
      if (params.pageSize) qs.set("pageSize", String(params.pageSize))
      if (params.status) qs.set("status", params.status)
      const q = qs.toString()
      return apiClient.get<AdminSkillPagedResponse>(
        `/admin/skills${q ? `?${q}` : ""}`
      )
    },
  })
}

function usePatchSkill(action: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) =>
      apiClient.patch<AdminSkillResponse>(`/admin/skills/${id}`, { action }),
    onSuccess: () => void qc.invalidateQueries({ queryKey: skillKeys.lists() }),
  })
}

/** Approves a pending skill. Idempotent. */
export function useApproveSkill() {
  return usePatchSkill("Approve")
}

/** Deactivates a skill (sets IsActive = false). Idempotent. */
export function useDeactivateSkill() {
  return usePatchSkill("Deactivate")
}

/** Reactivates a deactivated skill (sets IsActive = true). Idempotent. */
export function useActivateSkill() {
  return usePatchSkill("Activate")
}

/**
 * Permanently deletes a skill.
 * Returns 204 on success. Returns 409 if any profile still references
 * the skill — deactivate it instead.
 */
export function useDeleteSkill() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/admin/skills/${id}`),
    onSuccess: () => void qc.invalidateQueries({ queryKey: skillKeys.lists() }),
  })
}
