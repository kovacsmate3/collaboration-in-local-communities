import type { ComponentProps, ReactNode } from "react"

import { cn } from "@/lib/utils"

interface PageHeaderProps extends ComponentProps<"div"> {
  title: string
  description?: string
  /** Right-side actions (buttons, links). */
  actions?: ReactNode
}

/**
 * Standard page header. Use it as the first child of every main page so
 * spacing, alignment and tone stay consistent.
 */
export function PageHeader({
  title,
  description,
  actions,
  className,
  ...props
}: PageHeaderProps) {
  return (
    <div
      className={cn(
        "flex flex-col gap-3 border-b border-border pb-5 sm:flex-row sm:items-end sm:justify-between",
        className
      )}
      {...props}
    >
      <div className="flex flex-col gap-1">
        <h1 className="font-heading text-page-title">{title}</h1>
        {description ? (
          <p className="text-sm text-muted-foreground">{description}</p>
        ) : null}
      </div>
      {actions ? (
        <div className="flex shrink-0 items-center gap-2">{actions}</div>
      ) : null}
    </div>
  )
}
