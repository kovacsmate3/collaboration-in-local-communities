/**
 * Admin analytics / KPI API types and hooks.
 *
 * Endpoint: GET /api/admin/analytics/kpi
 * Backed by the `analytics.kpi_current_v` database view (issue #72 / #73).
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

export interface ChartEntry {
  label: string
  count: number
  pct: number
}

export interface ChartDataResponse {
  entries: ChartEntry[]
}

export const adminAnalyticsKeys = {
  all: ["admin", "analytics"] as const,
  kpi: () => [...adminAnalyticsKeys.all, "kpi"] as const,
  charts: {
    all: () => [...adminAnalyticsKeys.all, "charts"] as const,
    taskStatus: () =>
      [...adminAnalyticsKeys.charts.all(), "task-status"] as const,
    categoryDemand: () =>
      [...adminAnalyticsKeys.charts.all(), "category-demand"] as const,
    compensationMix: () =>
      [...adminAnalyticsKeys.charts.all(), "compensation-mix"] as const,
    taskApplicationStatus: () =>
      [...adminAnalyticsKeys.charts.all(), "task-application-status"] as const,
  },
}

export function useAdminKpi() {
  return useQuery({
    queryKey: adminAnalyticsKeys.kpi(),
    queryFn: () => apiClient.get<KpiCurrent>("/admin/analytics/kpi"),
    staleTime: 5 * 60 * 1000,
  })
}

export function useTaskStatusChart() {
  return useQuery({
    queryKey: adminAnalyticsKeys.charts.taskStatus(),
    queryFn: () =>
      apiClient.get<ChartDataResponse>("/admin/analytics/charts/task-status"),
    staleTime: 5 * 60 * 1000,
  })
}

export function useCategoryDemandChart() {
  return useQuery({
    queryKey: adminAnalyticsKeys.charts.categoryDemand(),
    queryFn: () =>
      apiClient.get<ChartDataResponse>(
        "/admin/analytics/charts/category-demand"
      ),
    staleTime: 5 * 60 * 1000,
  })
}

export function useCompensationMixChart() {
  return useQuery({
    queryKey: adminAnalyticsKeys.charts.compensationMix(),
    queryFn: () =>
      apiClient.get<ChartDataResponse>(
        "/admin/analytics/charts/compensation-mix"
      ),
    staleTime: 5 * 60 * 1000,
  })
}

export function useTaskApplicationStatusChart() {
  return useQuery({
    queryKey: adminAnalyticsKeys.charts.taskApplicationStatus(),
    queryFn: () =>
      apiClient.get<ChartDataResponse>(
        "/admin/analytics/charts/task-application-status"
      ),
    staleTime: 5 * 60 * 1000,
  })
}
