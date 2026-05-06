export interface AdminBarChartRow {
  label: string
  value: number
  pct: number
}

interface AdminBarChartProps {
  data: AdminBarChartRow[]
  colorClass: string
}

export function AdminBarChart({ data, colorClass }: AdminBarChartProps) {
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
