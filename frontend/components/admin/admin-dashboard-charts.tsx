import { InformationCircleIcon } from "@hugeicons/core-free-icons"
import { HugeiconsIcon } from "@hugeicons/react"

import {
  AdminDashboardChartPanel,
  type AdminDashboardChartPanelProps,
} from "@/components/admin/admin-dashboard-chart-panel"
import { Card, CardContent } from "@/components/ui/card"

const TASK_STATUS_DATA = [
  { label: "Open", value: 124, pct: 62 },
  { label: "In Progress", value: 47, pct: 24 },
  { label: "Completed", value: 186, pct: 93 },
  { label: "Cancelled", value: 18, pct: 9 },
]

const CATEGORY_DEMAND_DATA = [
  { label: "Household", value: 89, pct: 89 },
  { label: "Moving", value: 67, pct: 67 },
  { label: "Tutoring", value: 54, pct: 54 },
  { label: "Tech Help", value: 43, pct: 43 },
  { label: "Errands", value: 38, pct: 38 },
  { label: "Pet Care", value: 22, pct: 22 },
]

const COMPENSATION_DATA = [
  { label: "Voluntary", value: 142, pct: 71 },
  { label: "Points/Credit", value: 78, pct: 39 },
  { label: "Paid", value: 55, pct: 28 },
  { label: "Barter", value: 25, pct: 13 },
]

const BAR_CHART_PANELS: AdminDashboardChartPanelProps[] = [
  {
    title: "Task Status",
    description: "Distribution across lifecycle stages",
    data: TASK_STATUS_DATA,
    colorClass: "bg-primary",
  },
  {
    title: "Category Demand",
    description: "Tasks per category (last 30 days)",
    data: CATEGORY_DEMAND_DATA,
    colorClass: "bg-blue-500",
  },
  {
    title: "Compensation Mix",
    description: "Task compensation type breakdown",
    data: COMPENSATION_DATA,
    colorClass: "bg-emerald-500",
  },
]

export function AdminDashboardCharts() {
  return (
    <div className="grid gap-6 lg:grid-cols-3">
      {BAR_CHART_PANELS.map((panel) => (
        <AdminDashboardChartPanel key={panel.title} {...panel} />
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
