import { Badge } from "@/components/ui/badge"

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

function normalize(status: string): string {
  // "InProgress" → "in_progress", "Open" → "open", "in_progress" unchanged
  return status.replace(/([a-z])([A-Z])/g, "$1_$2").toLowerCase()
}

export function TaskStatusBadge({ status }: TaskStatusBadgeProps) {
  const key = normalize(status)
  const display = STATUS_DISPLAY[key] ?? { label: status, variant: "secondary" }

  return <Badge variant={display.variant}>{display.label}</Badge>
}
