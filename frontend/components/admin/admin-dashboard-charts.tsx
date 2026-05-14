"use client"

import { InformationCircleIcon } from "@hugeicons/core-free-icons"
import { HugeiconsIcon } from "@hugeicons/react"

import { AdminDashboardChartPanel } from "@/components/admin/admin-dashboard-chart-panel"
import { Card, CardContent } from "@/components/ui/card"
import {
  useTaskStatusChart,
  useCategoryDemandChart,
  useCompensationMixChart,
  type ChartEntry,
} from "@/lib/api/admin/analytics"
import type { AdminBarChartRow } from "@/components/admin/admin-bar-chart"

const toRows = (entries?: ChartEntry[]): AdminBarChartRow[] =>
  entries?.map((e) => ({ label: e.label, value: e.count, pct: e.pct })) ?? []

export function AdminDashboardCharts() {
  const taskStatus = useTaskStatusChart()
  const categoryDemand = useCategoryDemandChart()
  const compensationMix = useCompensationMixChart()

  const panels = [
    {
      title: "Task Status",
      description: "Distribution across lifecycle stages",
      data: toRows(taskStatus.data?.entries),
      colorClass: "bg-primary",
      isLoading: taskStatus.isLoading,
    },
    {
      title: "Category Demand",
      description: "Tasks per category (last 30 days)",
      data: toRows(categoryDemand.data?.entries),
      colorClass: "bg-info",
      isLoading: categoryDemand.isLoading,
    },
    {
      title: "Compensation Mix",
      description: "Task compensation type breakdown",
      data: toRows(compensationMix.data?.entries),
      colorClass: "bg-success",
      isLoading: compensationMix.isLoading,
    },
  ]

  return (
    <div className="grid gap-6 lg:grid-cols-3">
      {panels.map((panel) => (
        <AdminDashboardChartPanel key={panel.title} {...panel} isLive />
      ))}
    </div>
  )
}

export function AdminAnalyticsRoadmapNotice() {
  return (
    <Card className="border-dashed">
      <CardContent className="py-5">
        <div className="flex items-start gap-3">
          <HugeiconsIcon
            icon={InformationCircleIcon}
            className="mt-0.5 size-4 shrink-0 text-muted-foreground"
            strokeWidth={1.5}
          />
          <div className="space-y-1 text-sm text-muted-foreground">
            <p className="font-medium text-foreground">
              More analytics coming soon
            </p>
            <p>
              User growth trend, reputation distribution, cancellation reasons
              (#55), and message volume will appear here once their backend
              endpoints are ready (#72 / #73).
            </p>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
