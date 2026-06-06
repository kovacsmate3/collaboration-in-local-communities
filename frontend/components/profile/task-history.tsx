"use client"

import Link from "next/link"
import { useLocale, useTranslations } from "next-intl"

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { TaskStatusBadge } from "@/components/tasks/task-status-badge"
import { CategoryBadge } from "@/components/tasks/category-badge"
import { EmptyState } from "@/components/shared/empty-state"
import { formatRelativeTime } from "@/lib/format"
import type { ProfileTaskHistoryItem } from "@/lib/api/profile"

interface TaskHistoryProps {
  tasks: ProfileTaskHistoryItem[]
}

/**
 * Compact history list shown on a user's profile.
 *
 * Differs from `TaskList` in that this is read-only context for a
 * profile view, not an actionable feed - so the styling is tighter and
 * cards are not navigable.
 */
export function TaskHistory({ tasks }: TaskHistoryProps) {
  const t = useTranslations("profile.taskHistory")
  const tCategories = useTranslations("tasks.categories")
  const locale = useLocale()

  if (tasks.length === 0) {
    return <EmptyState title={t("emptyTitle")} description={t("emptyBody")} />
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-sm tracking-wide text-muted-foreground uppercase">
          {t("title")}
        </CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-2">
        {tasks.map((task) => (
          <Link
            key={task.id}
            href={`/tasks/${task.id}`}
            className="-mx-2 flex items-center justify-between gap-3 rounded-md border-t border-border px-2 pt-2 transition-colors first:border-t-0 first:pt-0 hover:bg-muted/50"
          >
            <div className="flex min-w-0 flex-col gap-1">
              <span className="truncate text-sm font-medium">{task.title}</span>
              <div className="flex items-center gap-2">
                <CategoryBadge
                  label={tCategories(task.category)}
                  icon={task.icon}
                />
                <span className="text-xs text-muted-foreground">
                  {formatRelativeTime(task.createdAt, locale)}
                </span>
              </div>
            </div>
            <TaskStatusBadge status={task.status} />
          </Link>
        ))}
      </CardContent>
    </Card>
  )
}
