"use client"

import { useTranslations } from "next-intl"

import { Badge } from "@/components/ui/badge"
import { normalizeTaskStatus } from "@/lib/task-status"

interface TaskStatusBadgeProps {
  status: string
}

type StatusKey =
  | "open"
  | "in_progress"
  | "pending_approval"
  | "completed"
  | "reviewed"
  | "cancelled"

const VARIANT: Record<
  StatusKey,
  React.ComponentProps<typeof Badge>["variant"]
> = {
  open: "outline",
  in_progress: "default",
  pending_approval: "warning",
  completed: "success",
  reviewed: "secondary",
  cancelled: "destructive",
}

const TRANSLATION_KEY: Record<StatusKey, string> = {
  open: "open",
  in_progress: "inProgress",
  pending_approval: "pendingApproval",
  completed: "completed",
  reviewed: "reviewed",
  cancelled: "cancelled",
}

export function TaskStatusBadge({ status }: TaskStatusBadgeProps) {
  const t = useTranslations("tasks.status")
  const key = normalizeTaskStatus(status) as StatusKey
  const variant = VARIANT[key] ?? "secondary"
  const labelKey = TRANSLATION_KEY[key]
  const label = labelKey ? t(labelKey) : status

  return <Badge variant={variant}>{label}</Badge>
}
