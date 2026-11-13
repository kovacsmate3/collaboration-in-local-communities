import {
  Activity01Icon,
  BarChartIcon,
  TaskDaily01Icon,
  Tick02Icon,
  UserMultiple02Icon,
} from "@hugeicons/core-free-icons"
import { useLocale } from "next-intl"

import type { KpiCurrent } from "@/lib/api/admin/analytics"
import { AdminKpiCard } from "@/components/admin/admin-kpi-card"

function formatCount(locale: string, value?: number) {
  return value === undefined ? "-" : value.toLocaleString(locale)
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
  const locale = useLocale()

  return (
    <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
      <AdminKpiCard
        title="Registered Users"
        value={formatCount(locale, data?.registeredUsers)}
        description="All-time total"
        icon={UserMultiple02Icon}
        isLoading={isLoading}
      />
      <AdminKpiCard
        title="Active Users (7d)"
        value={formatCount(locale, data?.activeUsers7d)}
        description="Unique users with activity"
        icon={Activity01Icon}
        isLoading={isLoading}
      />
      <AdminKpiCard
        title="Tasks Posted (7d)"
        value={formatCount(locale, data?.tasksPosted7d)}
        description="New tasks this week"
        icon={TaskDaily01Icon}
        isLoading={isLoading}
      />
      <AdminKpiCard
        title="Completed Tasks (7d)"
        value={formatCount(locale, data?.completedTasks7d)}
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
