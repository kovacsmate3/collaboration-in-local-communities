import {
  Activity01Icon,
  BarChartIcon,
  TaskDaily01Icon,
  Tick02Icon,
  UserMultiple02Icon,
} from "@hugeicons/core-free-icons"

import type { KpiCurrent } from "@/lib/api/admin/analytics"
import { AdminKpiCard } from "@/components/admin/admin-kpi-card"

function formatCount(value?: number) {
  return value === undefined ? "-" : value.toLocaleString()
}

function formatPercent(value?: number) {
  return value === undefined ? "-" : `${(value * 100).toFixed(1)}%`
}

interface AdminDashboardKpiCardsProps {
  data?: KpiCurrent
  isLoading: boolean
}

export function AdminDashboardKpiCards({
  data,
  isLoading,
}: AdminDashboardKpiCardsProps) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
      <AdminKpiCard
        title="Registered Users"
        value={formatCount(data?.registeredUsers)}
        description="All-time total"
        icon={UserMultiple02Icon}
        isLoading={isLoading}
      />
      <AdminKpiCard
        title="Active Users (7d)"
        value={formatCount(data?.activeUsers7d)}
        description="Unique users with activity"
        icon={Activity01Icon}
        isLoading={isLoading}
      />
      <AdminKpiCard
        title="Tasks Posted (7d)"
        value={formatCount(data?.tasksPosted7d)}
        description="New tasks this week"
        icon={TaskDaily01Icon}
        isLoading={isLoading}
      />
      <AdminKpiCard
        title="Completed Tasks (7d)"
        value={formatCount(data?.completedTasks7d)}
        description="Marked done this week"
        icon={Tick02Icon}
        isLoading={isLoading}
      />
      <AdminKpiCard
        title="Completion Rate (7d)"
        value={formatPercent(data?.completionRate7d)}
        description="Completed / posted"
        icon={BarChartIcon}
        isLoading={isLoading}
      />
    </div>
  )
}
