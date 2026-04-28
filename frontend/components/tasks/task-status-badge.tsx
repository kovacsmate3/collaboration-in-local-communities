import { Badge } from "@/components/ui/badge"
import { TASK_STATUS_LABELS } from "@/lib/constants"
import type { TaskStatus } from "@/lib/types"

interface TaskStatusBadgeProps {
  status: TaskStatus
}

const VARIANT: Record<
  TaskStatus,
  React.ComponentProps<typeof Badge>["variant"]
> = {
  open: "outline",
  in_progress: "default",
  completed: "success",
  reviewed: "secondary",
  cancelled: "destructive",
}

export function TaskStatusBadge({ status }: TaskStatusBadgeProps) {
  return <Badge variant={VARIANT[status]}>{TASK_STATUS_LABELS[status]}</Badge>
}
