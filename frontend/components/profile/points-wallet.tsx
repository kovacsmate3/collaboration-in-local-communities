"use client"

import { HugeiconsIcon } from "@hugeicons/react"
import { InformationCircleIcon } from "@hugeicons/core-free-icons"
import { useLocale, useTranslations } from "next-intl"

import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader } from "@/components/ui/card"
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip"
import { EmptyState } from "@/components/shared/empty-state"
import { formatRelativeTime } from "@/lib/format"
import {
  POINTS_BASE_REWARD,
  POINTS_REVIEW_BONUS_MAX,
  POINTS_REVIEW_BONUS_MIN,
  POINTS_VOLUNTARY_BONUS,
} from "@/lib/points"
import { cn } from "@/lib/utils"
import type { PointsLedgerEntryResponse } from "@/lib/api/profile"

interface PointsWalletProps {
  /** Live signed balance (sum of all ledger entries). */
  balance: number
  entries: PointsLedgerEntryResponse[]
  page: number
  totalPages: number
  onPageChange: (page: number) => void
  isFetching?: boolean
}

/**
 * Owner-only points wallet: the current balance plus a paginated, newest-first
 * history of ledger entries. Always rendered for the viewer's own profile, so
 * all copy is written in the second person.
 */
export function PointsWallet({
  balance,
  entries,
  page,
  totalPages,
  onPageChange,
  isFetching,
}: PointsWalletProps) {
  const t = useTranslations("profile.points")
  const locale = useLocale()
  const numberFormatter = new Intl.NumberFormat(locale)

  const entryTypeLabels: Record<string, string> = {
    TaskCompletedReward: t("entryType.taskCompletedReward"),
    ReviewQualityBonus: t("entryType.reviewQualityBonus"),
    ManualAdjustment: t("entryType.manualAdjustment"),
    Redemption: t("entryType.redemption"),
  }

  const showPager = totalPages > 1

  return (
    <div className="flex flex-col gap-4">
      <Card>
        <CardHeader className="flex flex-row items-center justify-between gap-2">
          <span className="text-sm font-medium tracking-wide text-muted-foreground uppercase">
            {t("balanceLabel")}
          </span>
          <EarnPointsHint />
        </CardHeader>
        <CardContent>
          <div className="flex items-baseline gap-2">
            <span className="text-3xl font-semibold tabular-nums">
              {numberFormatter.format(balance)}
            </span>
            <span className="text-sm text-muted-foreground">
              {t("unit", { count: balance })}
            </span>
          </div>
        </CardContent>
      </Card>

      {entries.length === 0 ? (
        <EmptyState
          title={t("historyEmptyTitle")}
          description={t("historyEmptyBody")}
        />
      ) : (
        <ul className="flex flex-col gap-3">
          {entries.map((entry) => (
            <li key={entry.id}>
              <Card>
                <CardContent className="flex items-center justify-between gap-3 py-4">
                  <div className="flex min-w-0 flex-col gap-0.5">
                    <span className="truncate text-sm font-medium">
                      {entryTypeLabels[entry.entryType] ?? entry.entryType}
                    </span>
                    {entry.description ? (
                      <span className="truncate text-xs text-muted-foreground">
                        {entry.description}
                      </span>
                    ) : null}
                    <span className="text-xs text-muted-foreground">
                      {formatRelativeTime(entry.createdAt, locale)}
                    </span>
                  </div>
                  <span
                    className={cn(
                      "shrink-0 text-sm font-semibold tabular-nums",
                      entry.amount >= 0
                        ? "text-emerald-600 dark:text-emerald-400"
                        : "text-destructive"
                    )}
                  >
                    {entry.amount >= 0
                      ? `+${numberFormatter.format(entry.amount)}`
                      : numberFormatter.format(entry.amount)}
                  </span>
                </CardContent>
              </Card>
            </li>
          ))}
        </ul>
      )}

      {showPager ? (
        <div className="flex items-center justify-between gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={page <= 1 || isFetching}
            onClick={() => onPageChange(page - 1)}
          >
            {t("previous")}
          </Button>
          <span className="text-xs text-muted-foreground">
            {t("pageOf", { page, total: totalPages })}
          </span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={page >= totalPages || isFetching}
            onClick={() => onPageChange(page + 1)}
          >
            {t("next")}
          </Button>
        </div>
      ) : null}
    </div>
  )
}

/**
 * Small info affordance next to the balance that explains, in the second
 * person, how points are earned. The amounts mirror the backend reward policy
 * via the constants in `@/lib/points`; the server remains the source of truth.
 */
function EarnPointsHint() {
  const t = useTranslations("profile.points.earn")

  return (
    <TooltipProvider delayDuration={150}>
      <Tooltip>
        <TooltipTrigger asChild>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="h-auto gap-1.5 px-2 py-1 text-xs text-muted-foreground"
          >
            <HugeiconsIcon icon={InformationCircleIcon} className="size-3.5" />
            {t("label")}
          </Button>
        </TooltipTrigger>
        <TooltipContent side="bottom" align="end" className="max-w-xs">
          <p className="font-medium">{t("title")}</p>
          <p className="mt-1 text-muted-foreground">{t("intro")}</p>
          <ul className="mt-2 flex list-disc flex-col gap-1 pl-4">
            <li>{t("base", { base: POINTS_BASE_REWARD })}</li>
            <li>{t("voluntary", { voluntary: POINTS_VOLUNTARY_BONUS })}</li>
            <li>
              {t("review", {
                min: POINTS_REVIEW_BONUS_MIN,
                max: POINTS_REVIEW_BONUS_MAX,
              })}
            </li>
          </ul>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  )
}
