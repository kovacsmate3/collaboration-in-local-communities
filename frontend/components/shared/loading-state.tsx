"use client"

import { useTranslations } from "next-intl"

import { Skeleton } from "@/components/ui/skeleton"
import { cn } from "@/lib/utils"

interface LoadingStateProps {
  /**
   * Number of skeleton blocks to render. Defaults to 3 — about the right
   * density for "list of cards is loading" without filling the whole viewport.
   */
  rows?: number
  /** Extra Tailwind classes for the wrapper (e.g. layout tweaks). */
  className?: string
  /**
   * Override the screen-reader announcement. Defaults to `common.states.loading`
   * ("Loading…" / "Betöltés…").
   */
  ariaLabel?: string
}

/**
 * Generic skeleton-block loading placeholder for list-style pages.
 *
 * Used as the single shared loading UI across the app so feeds, message
 * lists, profile cards, etc. all share one visual language (see #74). For
 * one-off, layout-specific shimmer placeholders (e.g. a profile header that
 * needs avatar + multi-line text) compose the underlying `<Skeleton>` directly.
 *
 * Announces itself as `role="status"` so screen readers pick the state up
 * without further wiring on the call site.
 */
export function LoadingState({
  rows = 3,
  className,
  ariaLabel,
}: LoadingStateProps) {
  const t = useTranslations("common.states")
  const label = ariaLabel ?? t("loading")

  return (
    <div
      role="status"
      aria-label={label}
      aria-live="polite"
      aria-busy="true"
      className={cn("flex flex-col gap-3", className)}
    >
      {Array.from({ length: Math.max(1, rows) }, (_, index) => (
        <Skeleton key={index} className="h-20 w-full rounded-lg" />
      ))}
      <span className="sr-only">{label}</span>
    </div>
  )
}
