/**
 * Admin analytics / KPI API types and hooks.
 *
 * Endpoint: GET /api/admin/analytics/kpi
 * Backed by the `analytics.kpi_current_v` database view (issue #72 / #73).
 *
 * TODO: wire up once the /api/admin/analytics/kpi endpoint is shipped.
 */

import { useQuery } from "@tanstack/react-query"
import { apiClient } from "@/lib/api/client"

export interface KpiCurrent {
  registeredUsers: number
  activeUsers7d: number
  tasksPosted7d: number
  completedTasks7d: number
  completionRate7d: number
}

export const adminAnalyticsKeys = {
  all: ["admin", "analytics"] as const,
  kpi: () => [...adminAnalyticsKeys.all, "kpi"] as const,
}

export function useAdminKpi() {
  return useQuery({
    queryKey: adminAnalyticsKeys.kpi(),
    queryFn: () => apiClient.get<KpiCurrent>("/admin/analytics/kpi"),
    // Refresh every 5 minutes
    staleTime: 5 * 60 * 1000,
  })
}
