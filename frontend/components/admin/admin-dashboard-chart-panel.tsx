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

export interface AdminDashboardChartPanelProps {
  title: string
  description: string
  data: AdminBarChartRow[]
  colorClass: string
}

export function AdminDashboardChartPanel({
  title,
  description,
  data,
  colorClass,
}: AdminDashboardChartPanelProps) {
  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between gap-2">
          <div>
            <CardTitle className="text-base">{title}</CardTitle>
            <CardDescription className="mt-1">{description}</CardDescription>
          </div>
          <AdminPlaceholderBadge />
        </div>
      </CardHeader>
      <CardContent>
        <AdminBarChart data={data} colorClass={colorClass} />
      </CardContent>
    </Card>
  )
}
