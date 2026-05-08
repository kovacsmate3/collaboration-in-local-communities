"use client"

import { useAdminKpi } from "@/lib/api/admin/analytics"
import { Alert, AlertDescription } from "@/components/ui/alert"
import {
  AdminAnalyticsRoadmapNotice,
  AdminDashboardCharts,
} from "@/components/admin/admin-dashboard-charts"
import { AdminDashboardKpiCards } from "@/components/admin/admin-dashboard-kpi-cards"

export function AdminDashboard() {
  const { data, isLoading, isError, error } = useAdminKpi()

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Dashboard</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Marketplace health overview
        </p>
      </div>

      {isError ? (
        <Alert variant="destructive">
          <AlertDescription>
            Could not load KPI data -{" "}
            {error instanceof Error ? error.message : "unknown error"}. The
            analytics endpoint may not be available yet (issue #72).
          </AlertDescription>
        </Alert>
      ) : null}

      <AdminDashboardKpiCards data={data} isLoading={isLoading && !isError} />
      <AdminDashboardCharts />
      <AdminAnalyticsRoadmapNotice />
    </div>
  )
}
