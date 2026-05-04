"use client"

import * as React from "react"
import { HugeiconsIcon } from "@hugeicons/react"
import {
  UserMultiple02Icon,
  Activity01Icon,
  TaskDaily01Icon,
  Tick02Icon,
  BarChartIcon,
  InformationCircleIcon,
} from "@hugeicons/core-free-icons"

import { useAdminKpi, type KpiCurrent } from "@/lib/api/admin/analytics"
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  CardDescription,
} from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import type { IconSvgElement } from "@hugeicons/react"

// ── KPI card ────────────────────────────────────────────────────────────────

interface KpiCardProps {
  title: string
  value: string | number
  description?: string
  icon: IconSvgElement
  isLoading?: boolean
}

function KpiCard({ title, value, description, icon, isLoading }: KpiCardProps) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">
          {title}
        </CardTitle>
        <HugeiconsIcon
          icon={icon}
          className="size-4 text-muted-foreground"
          strokeWidth={1.5}
        />
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <Skeleton className="h-8 w-24" />
        ) : (
          <div className="text-2xl font-bold tracking-tight">{value}</div>
        )}
        {description && (
          <p className="mt-1 text-xs text-muted-foreground">{description}</p>
        )}
      </CardContent>
    </Card>
  )
}

function KpiCards({
  data,
  isLoading,
}: {
  data?: KpiCurrent
  isLoading: boolean
}) {
  const fmt = (n?: number) => (n === undefined ? "—" : n.toLocaleString())
  const pct = (n?: number) =>
    n === undefined ? "—" : `${(n * 100).toFixed(1)}%`

  return (
    <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
      <KpiCard
        title="Registered Users"
        value={fmt(data?.registeredUsers)}
        description="All-time total"
        icon={UserMultiple02Icon}
        isLoading={isLoading}
      />
      <KpiCard
        title="Active Users (7d)"
        value={fmt(data?.activeUsers7d)}
        description="Unique users with activity"
        icon={Activity01Icon}
        isLoading={isLoading}
      />
      <KpiCard
        title="Tasks Posted (7d)"
        value={fmt(data?.tasksPosted7d)}
        description="New tasks this week"
        icon={TaskDaily01Icon}
        isLoading={isLoading}
      />
      <KpiCard
        title="Completed Tasks (7d)"
        value={fmt(data?.completedTasks7d)}
        description="Marked done this week"
        icon={Tick02Icon}
        isLoading={isLoading}
      />
      <KpiCard
        title="Completion Rate (7d)"
        value={pct(data?.completionRate7d)}
        description="Completed / posted"
        icon={BarChartIcon}
        isLoading={isLoading}
      />
    </div>
  )
}

// ── Placeholder charts ────────────────────────────────────────────────────────

/** Simple horizontal bar chart using plain CSS */
function BarChart({
  data,
  colorClass = "bg-primary",
}: {
  data: { label: string; value: number; pct: number }[]
  colorClass?: string
}) {
  return (
    <div className="space-y-3">
      {data.map((row) => (
        <div key={row.label} className="flex items-center gap-3">
          <span className="w-28 shrink-0 truncate text-sm text-muted-foreground">
            {row.label}
          </span>
          <div className="h-2 flex-1 overflow-hidden rounded-full bg-muted">
            <div
              className={`h-full rounded-full ${colorClass} transition-all`}
              style={{ width: `${row.pct}%` }}
            />
          </div>
          <span className="w-10 shrink-0 text-right text-sm tabular-nums">
            {row.value}
          </span>
        </div>
      ))}
    </div>
  )
}

// ── Stub chart data (isolated placeholders until real endpoints exist) ───────

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

function PlaceholderBadge() {
  return (
    <Badge variant="outline" className="gap-1 text-[10px]">
      <HugeiconsIcon
        icon={InformationCircleIcon}
        className="size-3"
        strokeWidth={1.5}
      />
      Placeholder data
    </Badge>
  )
}

// ── Dashboard component ───────────────────────────────────────────────────────

export function AdminDashboard() {
  const { data, isLoading, isError, error } = useAdminKpi()

  return (
    <div className="space-y-8">
      {/* Page heading */}
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Dashboard</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Marketplace health overview
        </p>
      </div>

      {/* KPI error state */}
      {isError && (
        <Alert variant="destructive">
          <AlertDescription>
            Could not load KPI data —{" "}
            {error instanceof Error ? error.message : "unknown error"}. The
            analytics endpoint may not be available yet (issue #72).
          </AlertDescription>
        </Alert>
      )}

      {/* KPI cards */}
      <KpiCards data={data} isLoading={isLoading && !isError} />

      {/* Charts row */}
      <div className="grid gap-6 lg:grid-cols-3">
        {/* Task lifecycle distribution */}
        <Card>
          <CardHeader className="pb-3">
            <div className="flex items-start justify-between gap-2">
              <div>
                <CardTitle className="text-base">Task Status</CardTitle>
                <CardDescription className="mt-1">
                  Distribution across lifecycle stages
                </CardDescription>
              </div>
              <PlaceholderBadge />
            </div>
          </CardHeader>
          <CardContent>
            <BarChart data={TASK_STATUS_DATA} colorClass="bg-primary" />
          </CardContent>
        </Card>

        {/* Category demand */}
        <Card>
          <CardHeader className="pb-3">
            <div className="flex items-start justify-between gap-2">
              <div>
                <CardTitle className="text-base">Category Demand</CardTitle>
                <CardDescription className="mt-1">
                  Tasks per category (last 30 days)
                </CardDescription>
              </div>
              <PlaceholderBadge />
            </div>
          </CardHeader>
          <CardContent>
            <BarChart data={CATEGORY_DEMAND_DATA} colorClass="bg-blue-500" />
          </CardContent>
        </Card>

        {/* Compensation mix */}
        <Card>
          <CardHeader className="pb-3">
            <div className="flex items-start justify-between gap-2">
              <div>
                <CardTitle className="text-base">Compensation Mix</CardTitle>
                <CardDescription className="mt-1">
                  Task compensation type breakdown
                </CardDescription>
              </div>
              <PlaceholderBadge />
            </div>
          </CardHeader>
          <CardContent>
            <BarChart data={COMPENSATION_DATA} colorClass="bg-emerald-500" />
          </CardContent>
        </Card>
      </div>

      {/* Roadmap notice */}
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
    </div>
  )
}
