"use client"

import Link from "next/link"
import type { ReactNode } from "react"
import { HugeiconsIcon } from "@hugeicons/react"
import type { IconSvgElement } from "@hugeicons/react"
import {
  Award01Icon,
  ChartLineData01Icon,
  InformationCircleIcon,
  StarIcon,
  TaskDone01Icon,
} from "@hugeicons/core-free-icons"
import { useTranslations } from "next-intl"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip"
import { RatingStars } from "@/components/shared/rating-stars"
import {
  COMPLETED_TASK_WEIGHT,
  REVIEW_QUALITY_WEIGHT,
  type ReputationTier,
  nextTierForScore,
  reputationScoreBreakdown,
  tierForScore,
} from "@/lib/reputation"
import { cn } from "@/lib/utils"

interface ReputationTrendPoint {
  date: string
  score: number
  averageRating: number
  reviewCount: number
  completedTaskCount: number
}

interface ReputationCardProps {
  /** Aggregate reputation score (server-computed, with a client fallback). */
  score: number
  averageRating: number
  reviewCount: number
  completedTaskCount: number
  /** Backend-computed cumulative reputation snapshots for the sparkline. */
  trend?: readonly ReputationTrendPoint[]
  /**
   * When `true`, the empty-state shows CTAs aimed at the profile owner
   * (post a task / browse helper requests). For public profiles we hide them.
   */
  showOwnerCtas?: boolean
  className?: string
}

/**
 * Reputation dashboard widget rendered on the profile.
 *
 * Composes {@link ReputationSummary}-style metrics (score, rating, completed)
 * with a tier badge, a tooltip explaining how the score is computed, a
 * progress bar toward the next tier, and a guided empty state for new users.
 */
export function ReputationCard({
  score,
  averageRating,
  reviewCount,
  completedTaskCount,
  trend = [],
  showOwnerCtas = false,
  className,
}: ReputationCardProps) {
  const t = useTranslations("profile.reputation")
  const breakdown = reputationScoreBreakdown({
    score,
    averageRating,
    reviewCount,
    completedTaskCount,
  })
  const tier = tierForScore(breakdown.total)
  const nextTier = nextTierForScore(breakdown.total)
  const isEmpty =
    breakdown.total === 0 &&
    breakdown.reviewCount === 0 &&
    breakdown.completedTaskCount === 0

  return (
    <Card className={className}>
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle className="text-sm tracking-wide text-muted-foreground uppercase">
          {t("title")}
        </CardTitle>
        <Badge variant={isEmpty ? "muted" : "warning"} className="gap-1">
          <HugeiconsIcon icon={Award01Icon} />
          {t(`tiers.${tier.id}` as const)}
        </Badge>
      </CardHeader>
      <CardContent className="flex flex-col gap-5">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <Stat
            icon={Award01Icon}
            label={t("score")}
            value={
              <span className="font-heading text-2xl font-semibold tracking-tight tabular-nums">
                {breakdown.total.toLocaleString()}
              </span>
            }
            hint={
              <ScoreBreakdownTooltip
                completedComponent={breakdown.completedComponent}
                reviewComponent={breakdown.reviewComponent}
                completedTaskCount={breakdown.completedTaskCount}
                averageRating={breakdown.averageRating}
                reviewCount={breakdown.reviewCount}
                total={breakdown.total}
              />
            }
            accent
          />
          <Stat
            icon={StarIcon}
            label={t("averageRating")}
            value={
              <RatingStars
                size="md"
                value={breakdown.averageRating}
                reviewCount={breakdown.reviewCount}
              />
            }
            hint={
              breakdown.reviewCount > 0
                ? t("averageRatingHint", {
                    rating: breakdown.averageRating.toFixed(1),
                    count: breakdown.reviewCount,
                  })
                : t("noReviewsYet")
            }
          />
          <Stat
            icon={TaskDone01Icon}
            label={t("completedTasks")}
            value={
              <span className="font-heading text-2xl font-semibold tracking-tight tabular-nums">
                {breakdown.completedTaskCount.toLocaleString()}
              </span>
            }
            hint={t("completedTasksHint", {
              count: breakdown.completedTaskCount,
            })}
          />
        </div>

        <TrendPanel trend={trend} />

        {nextTier ? (
          <NextTierProgress
            currentScore={breakdown.total}
            nextTierId={nextTier.id}
            nextTierMin={nextTier.minScore}
          />
        ) : null}

        {isEmpty ? <EmptyState showOwnerCtas={showOwnerCtas} /> : null}
      </CardContent>
    </Card>
  )
}

interface StatProps {
  icon: IconSvgElement
  label: string
  value: ReactNode
  hint?: ReactNode
  accent?: boolean
}

function Stat({ icon, label, value, hint, accent = false }: StatProps) {
  return (
    <div className="flex items-start gap-3">
      <div
        className={cn(
          "flex size-9 shrink-0 items-center justify-center rounded-lg ring-1 ring-foreground/10",
          accent
            ? "bg-reputation/15 text-reputation"
            : "bg-muted text-muted-foreground"
        )}
        aria-hidden
      >
        <HugeiconsIcon icon={icon} className="size-4" strokeWidth={1.5} />
      </div>
      <div className="flex min-w-0 flex-col gap-1">
        <span className="text-xs text-muted-foreground">{label}</span>
        <span className="text-base leading-none font-semibold">{value}</span>
        {hint ? (
          <span className="text-xs text-muted-foreground">{hint}</span>
        ) : null}
      </div>
    </div>
  )
}

interface ScoreBreakdownTooltipProps {
  completedComponent: number
  reviewComponent: number
  completedTaskCount: number
  averageRating: number
  reviewCount: number
  total: number
}

function ScoreBreakdownTooltip({
  completedComponent,
  reviewComponent,
  completedTaskCount,
  averageRating,
  reviewCount,
  total,
}: ScoreBreakdownTooltipProps) {
  const t = useTranslations("profile.reputation")
  return (
    <TooltipProvider delayDuration={150}>
      <Tooltip>
        <TooltipTrigger asChild>
          <button
            type="button"
            className="inline-flex items-center gap-1 rounded-sm text-xs text-muted-foreground underline-offset-2 hover:underline focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
            aria-label={t("calculatedAria")}
          >
            <HugeiconsIcon
              icon={InformationCircleIcon}
              className="size-3.5"
              strokeWidth={1.5}
            />
            {t("calculatedLabel")}
          </button>
        </TooltipTrigger>
        <TooltipContent
          side="bottom"
          align="start"
          className="max-w-xs px-3 py-2 text-left"
        >
          <p className="font-semibold">{t("tooltipTitle")}</p>
          <p className="mt-1 text-muted-foreground">
            {t("tooltipBody", {
              taskWeight: COMPLETED_TASK_WEIGHT,
              reviewWeight: REVIEW_QUALITY_WEIGHT,
            })}
          </p>
          <dl className="mt-2 grid grid-cols-[1fr_auto] gap-x-3 gap-y-0.5 tabular-nums">
            <dt>
              {completedTaskCount} x {COMPLETED_TASK_WEIGHT}
            </dt>
            <dd className="text-right">{completedComponent}</dd>
            <dt>
              {averageRating.toFixed(1)} x {reviewCount} x{" "}
              {REVIEW_QUALITY_WEIGHT}
            </dt>
            <dd className="text-right">{reviewComponent}</dd>
            <dt className="border-t border-border/50 pt-1 font-semibold">
              {t("tooltipTotal")}
            </dt>
            <dd className="border-t border-border/50 pt-1 text-right font-semibold">
              {total}
            </dd>
          </dl>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  )
}

interface NextTierProgressProps {
  currentScore: number
  nextTierId: ReputationTier
  nextTierMin: number
}

function NextTierProgress({
  currentScore,
  nextTierId,
  nextTierMin,
}: NextTierProgressProps) {
  const t = useTranslations("profile.reputation")
  const nextTierLabel = t(`tiers.${nextTierId}` as const)
  const ratio =
    nextTierMin > 0 ? Math.min(1, Math.max(0, currentScore / nextTierMin)) : 1
  const pct = Math.round(ratio * 100)
  const remaining = Math.max(0, nextTierMin - currentScore)

  return (
    <div className="flex flex-col gap-2">
      <div className="flex items-center justify-between text-xs text-muted-foreground">
        <span>
          {remaining > 0
            ? t("nextTierProgress", {
                remaining: remaining.toLocaleString(),
                tier: nextTierLabel,
              })
            : t("nextTierReady", { tier: nextTierLabel })}
        </span>
        <span className="tabular-nums">{pct}%</span>
      </div>
      <div
        className="h-1.5 w-full overflow-hidden rounded-full bg-muted"
        role="progressbar"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={pct}
        aria-label={t("nextTierAria", { tier: nextTierLabel })}
      >
        <div
          className="h-full rounded-full bg-reputation transition-[width]"
          style={{ width: `${pct}%` }}
        />
      </div>
    </div>
  )
}

function TrendPanel({ trend }: { trend: readonly ReputationTrendPoint[] }) {
  const t = useTranslations("profile.reputation")
  const points = normalizeTrend(trend)

  if (points.length === 0) {
    return null
  }

  const first = points[0]
  const last = points[points.length - 1]
  const comparisonScore = points.length > 1 ? first.score : first.score
  const delta = last.score - comparisonScore
  const deltaLabel =
    delta > 0
      ? `+${delta.toLocaleString()}`
      : delta < 0
        ? `-${Math.abs(delta).toLocaleString()}`
        : t("trendNoChange")

  return (
    <div className="grid gap-3 rounded-lg border border-border bg-muted/20 p-3 sm:grid-cols-[minmax(0,1fr)_11rem] sm:items-center">
      <div className="flex items-start gap-3">
        <div
          className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-reputation/15 text-reputation ring-1 ring-foreground/10"
          aria-hidden
        >
          <HugeiconsIcon
            icon={ChartLineData01Icon}
            className="size-4"
            strokeWidth={1.5}
          />
        </div>
        <div className="min-w-0">
          <p className="text-xs text-muted-foreground">{t("trendLabel")}</p>
          <p className="font-heading text-lg font-semibold tracking-tight tabular-nums">
            {deltaLabel}
          </p>
          <p className="text-xs text-muted-foreground">
            {first.date === last.date
              ? t("trendSinceDate", { date: formatShortDate(first.date) })
              : t("trendDateRange", {
                  start: formatShortDate(first.date),
                  end: formatShortDate(last.date),
                })}
          </p>
        </div>
      </div>
      <Sparkline points={points} />
    </div>
  )
}

function Sparkline({ points }: { points: readonly ReputationTrendPoint[] }) {
  const t = useTranslations("profile.reputation")
  const width = 176
  const height = 56
  const padding = 4
  const plotWidth = width - padding * 2
  const plotHeight = height - padding * 2
  const scores = points.map((point) => point.score)
  const minScore = Math.min(...scores)
  const maxScore = Math.max(...scores)
  const scoreRange = Math.max(1, maxScore - minScore)
  const xStep = points.length > 1 ? plotWidth / (points.length - 1) : 0
  const plotted = points.map((point, index) => {
    const x = points.length > 1 ? padding + index * xStep : width / 2
    const y =
      padding +
      plotHeight -
      ((point.score - minScore) / scoreRange) * plotHeight

    return { x, y }
  })
  const polyline = plotted
    .map((point) => `${point.x.toFixed(2)},${point.y.toFixed(2)}`)
    .join(" ")
  const linePath = plotted
    .map((point) => `${point.x.toFixed(2)} ${point.y.toFixed(2)}`)
    .join(" L ")
  const areaPath =
    plotted.length > 1
      ? `M ${plotted[0].x.toFixed(2)} ${height - padding} L ${linePath} L ${plotted[
          plotted.length - 1
        ].x.toFixed(2)} ${height - padding} Z`
      : ""
  const last = plotted[plotted.length - 1]

  return (
    <svg
      role="img"
      aria-label={t("trendAria", {
        start: formatShortDate(points[0].date),
        end: formatShortDate(points[points.length - 1].date),
      })}
      viewBox={`0 0 ${width} ${height}`}
      className="h-14 w-full text-reputation"
    >
      {areaPath ? (
        <path d={areaPath} className="fill-current opacity-15" />
      ) : null}
      {plotted.length > 1 ? (
        <polyline
          points={polyline}
          fill="none"
          stroke="currentColor"
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth={2}
          vectorEffect="non-scaling-stroke"
        />
      ) : null}
      <circle
        cx={last.x}
        cy={last.y}
        r={2.75}
        className="fill-background stroke-current"
        strokeWidth={2}
      />
    </svg>
  )
}

function EmptyState({ showOwnerCtas }: { showOwnerCtas: boolean }) {
  const t = useTranslations("profile.reputation")
  return (
    <div className="rounded-lg border border-dashed border-border bg-muted/30 p-3 text-sm">
      <p className="font-medium">
        {t(showOwnerCtas ? "emptyTitleOwn" : "emptyTitle")}
      </p>
      <p className="mt-1 text-xs text-muted-foreground">
        {t(showOwnerCtas ? "emptyBodyOwn" : "emptyBody")}
      </p>
      {showOwnerCtas ? (
        <div className="mt-3 flex flex-wrap gap-2">
          <Button asChild size="sm">
            <Link href="/feed">{t("findTask")}</Link>
          </Button>
          <Button asChild size="sm" variant="outline">
            <Link href="/post-task">{t("postFirstTask")}</Link>
          </Button>
        </div>
      ) : null}
    </div>
  )
}

function normalizeTrend(
  trend: readonly ReputationTrendPoint[]
): ReputationTrendPoint[] {
  return trend
    .map((point) => ({
      ...point,
      score: Number.isFinite(point.score)
        ? Math.max(0, Math.round(point.score))
        : 0,
      averageRating: Number.isFinite(point.averageRating)
        ? Math.max(0, Math.min(5, point.averageRating))
        : 0,
      reviewCount: Number.isFinite(point.reviewCount)
        ? Math.max(0, Math.round(point.reviewCount))
        : 0,
      completedTaskCount: Number.isFinite(point.completedTaskCount)
        ? Math.max(0, Math.round(point.completedTaskCount))
        : 0,
    }))
    .filter((point) => point.date.length > 0)
    .sort((a, b) => a.date.localeCompare(b.date))
}

function formatShortDate(date: string) {
  const parsed = new Date(`${date}T00:00:00Z`)

  if (Number.isNaN(parsed.getTime())) {
    return date
  }

  return new Intl.DateTimeFormat(undefined, {
    day: "numeric",
    month: "short",
  }).format(parsed)
}
