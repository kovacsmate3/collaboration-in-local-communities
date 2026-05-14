import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import {
  AdminBarChart,
  type AdminBarChartRow,
} from "@/components/admin/admin-bar-chart"
import { AdminPlaceholderBadge } from "@/components/admin/admin-placeholder-badge"
import { Skeleton } from "@/components/ui/skeleton"

export interface AdminDashboardChartPanelProps {
  title: string
  description: string
  data: AdminBarChartRow[]
  colorClass: string
  isLoading?: boolean
  isLive?: boolean
}

export function AdminDashboardChartPanel({
  title,
  description,
  data,
  colorClass,
  isLoading,
  isLive,
}: AdminDashboardChartPanelProps) {
  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between gap-2">
          <div>
            <CardTitle className="text-base">{title}</CardTitle>
            <CardDescription className="mt-1">{description}</CardDescription>
          </div>
          {!isLive && <AdminPlaceholderBadge />}
        </div>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <Skeleton className="h-32 w-full" />
        ) : (
          <AdminBarChart data={data} colorClass={colorClass} />
        )}
      </CardContent>
    </Card>
  )
}
