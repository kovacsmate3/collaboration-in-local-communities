"use client"

import { HugeiconsIcon } from "@hugeicons/react"
import { Alert02Icon } from "@hugeicons/core-free-icons"
import { useTranslations } from "next-intl"

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

interface ErrorStateProps {
  /**
   * Headline shown in the alert. Defaults to `common.states.loadErrorTitle`
   * ("Something went wrong"). Override when a more specific message helps
   * the user understand what failed.
   */
  title?: string
  /**
   * Supporting body copy under the headline. Defaults to
   * `common.states.loadErrorBody` ("Please try again in a moment."). Pass
   * `null` to hide the description entirely.
   */
  description?: string | null
  /**
   * If provided, a "Try again" button is rendered that calls this callback.
   * Wire it to your react-query `refetch()` (or equivalent) to give users a
   * way to recover without reloading the page.
   */
  onRetry?: () => void
  /** Override the retry button label. Defaults to `common.states.tryAgain`. */
  retryLabel?: string
  /** Extra Tailwind classes for the wrapper. */
  className?: string
}

/**
 * Shared error UI for "data fetch failed" surfaces across the app.
 *
 * Wraps the destructive `Alert` variant so the visual treatment is
 * consistent everywhere (feed, messages, profile, task lists). When a
 * retry callback is provided the alert grows a "Try again" button at the
 * bottom, which is the conventional shape for a recoverable fetch error
 * (see #74).
 *
 * For unrecoverable errors that should redirect to a 404 page, prefer
 * Next.js's `notFound()` over rendering this component.
 */
export function ErrorState({
  title,
  description,
  onRetry,
  retryLabel,
  className,
}: ErrorStateProps) {
  const t = useTranslations("common.states")
  const resolvedTitle = title ?? t("loadErrorTitle")
  const resolvedDescription =
    description === undefined ? t("loadErrorBody") : description
  const resolvedRetryLabel = retryLabel ?? t("tryAgain")

  return (
    <Alert
      variant="destructive"
      className={cn("flex flex-col gap-3", className)}
    >
      <HugeiconsIcon icon={Alert02Icon} />
      <AlertTitle>{resolvedTitle}</AlertTitle>
      {resolvedDescription ? (
        <AlertDescription>{resolvedDescription}</AlertDescription>
      ) : null}
      {onRetry ? (
        <div className="flex">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={onRetry}
            className="mt-1"
          >
            {resolvedRetryLabel}
          </Button>
        </div>
      ) : null}
    </Alert>
  )
}
