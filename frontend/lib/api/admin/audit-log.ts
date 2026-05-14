import { useQuery } from "@tanstack/react-query"
import { apiClient } from "@/lib/api/client"

export interface AuditLogEntry {
  id: string
  actorUserId?: string
  actorEmail?: string
  actorDisplayName?: string
  eventType: string
  entityType?: string
  entityId?: string
  payload?: string
  createdAt: string
}

export interface AuditLogPagedResponse {
  items: AuditLogEntry[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface AuditLogParams {
  page?: number
  pageSize?: number
  eventType?: string
  actorUserId?: string
  entityType?: string
  search?: string
  from?: string
  to?: string
}

export const auditLogKeys = {
  all: ["admin", "audit-log"] as const,
  lists: () => [...auditLogKeys.all, "list"] as const,
  list: (params: AuditLogParams) => [...auditLogKeys.lists(), params] as const,
}

export function useAuditLog(params: AuditLogParams = {}) {
  return useQuery({
    queryKey: auditLogKeys.list(params),
    queryFn: () => {
      const qs = new URLSearchParams()
      Object.entries(params).forEach(([k, v]) => {
        if (v) qs.set(k, String(v))
      })
      const q = qs.toString()
      return apiClient.get<AuditLogPagedResponse>(
        `/admin/audit-log${q ? `?${q}` : ""}`
      )
    },
  })
}
