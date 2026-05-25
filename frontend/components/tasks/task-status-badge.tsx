import { Badge } from "@/components/ui/badge"
import { normalizeTaskStatus } from "@/lib/task-status"

interface TaskStatusBadgeProps {
  status: string
}

interface StatusDisplay {
  label: string
  variant: React.ComponentProps<typeof Badge>["variant"]
}

const STATUS_DISPLAY: Record<string, StatusDisplay> = {
  open: { label: "Open", variant: "outline" },
  in_progress: { label: "In progress", variant: "default" },
  pending_approval: { label: "Awaiting approval", variant: "warning" },
  completed: { label: "Completed", variant: "success" },
  reviewed: { label: "Reviewed", variant: "secondary" },
  cancelled: { label: "Cancelled", variant: "destructive" },
}

export function TaskStatusBadge({ status }: TaskStatusBadgeProps) {
  const key = normalizeTaskStatus(status)
  const display = STATUS_DISPLAY[key] ?? { label: status, variant: "secondary" }

  return <Badge variant={display.variant}>{display.label}</Badge>
}
