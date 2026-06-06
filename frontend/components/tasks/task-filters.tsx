"use client"

import * as React from "react"
import { HugeiconsIcon } from "@hugeicons/react"
import { Search01Icon } from "@hugeicons/core-free-icons"
import { useTranslations } from "next-intl"

import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { COMPENSATION_OPTIONS, RECENCY_OPTIONS } from "@/lib/constants"
import type { ApiCategory } from "@/lib/api/categories"
import type { CompensationType } from "@/lib/types"

/**
 * Radius options offered in the feed filter. Stored as meters so the value
 * can be forwarded straight to the backend's `radiusMeters` query parameter.
 *
 * The label is intentionally NOT in this list — the consumer resolves it
 * through next-intl (`tasks.filters.anyDistance` or `tasks.filters.within`
 * with `{km}`) so the dropdown renders in either language.
 */
export const RADIUS_OPTIONS = [
  { value: "any", meters: null, km: null },
  { value: "1000", meters: 1000, km: 1 },
  { value: "5000", meters: 5000, km: 5 },
  { value: "10000", meters: 10_000, km: 10 },
  { value: "25000", meters: 25_000, km: 25 },
  { value: "50000", meters: 50_000, km: 50 },
] as const

export type RadiusOptionValue = (typeof RADIUS_OPTIONS)[number]["value"]

export interface TaskFiltersState {
  query: string
  /** Backend category id (GUID) or `"all"` for no category filter. */
  category: string
  compensation: CompensationType | "all"
  recency: (typeof RECENCY_OPTIONS)[number]
  /** Selected radius bucket. `"any"` disables the proximity filter. */
  radius: RadiusOptionValue
}

interface TaskFiltersProps {
  value: TaskFiltersState
  onChange: (value: TaskFiltersState) => void
  /** Categories loaded from `useCategories()` and rendered in the Category Select. */
  categories: ApiCategory[]
  /**
   * Whether the viewer has a known geographic origin (typically the profile
   * location). When false, the radius Select is disabled and a hint explains
   * why — the backend rejects partial proximity triplets.
   */
  hasOrigin: boolean
}

/**
 * Combined search + filter bar for the canonical feed.
 *
 * Controlled component; the parent page owns the filter state and persists
 * it to the URL so refreshes and shares preserve the view.
 *
 * Accessibility notes:
 *  - The search input has only a decorative icon and a placeholder, so it
 *    needs an explicit `aria-label` to get an accessible name.
 *  - The icon is `aria-hidden` so screen readers don't announce it.
 *  - Each Select trigger shows its current value (e.g. "All categories");
 *    without context a screen-reader user can't tell which dimension it
 *    filters, so `aria-label` on the trigger fills the gap.
 */
export function TaskFilters({
  value,
  onChange,
  categories,
  hasOrigin,
}: TaskFiltersProps) {
  const t = useTranslations("tasks.filters")
  const tCompensation = useTranslations("tasks.compensation")
  const tCategories = useTranslations("tasks.categories")
  const update = (patch: Partial<TaskFiltersState>) =>
    onChange({ ...value, ...patch })

  return (
    <div className="flex flex-col gap-2">
      <div className="flex flex-col gap-3 sm:grid sm:grid-cols-2 lg:grid-cols-[1fr_auto_auto_auto_auto]">
        <div className="relative">
          <HugeiconsIcon
            icon={Search01Icon}
            aria-hidden="true"
            className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground"
          />
          <Input
            type="search"
            aria-label={t("searchAria")}
            className="pl-9"
            placeholder={t("searchPlaceholder")}
            value={value.query}
            onChange={(e) => update({ query: e.target.value })}
          />
        </div>

        <Select
          value={value.category}
          onValueChange={(v) => update({ category: v })}
        >
          <SelectTrigger aria-label={t("categoryAria")} className="lg:w-44">
            <SelectValue placeholder={t("categoryPlaceholder")} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t("allCategories")}</SelectItem>
            {categories.map((c) => (
              <SelectItem key={c.id} value={c.id}>
                {tCategories.has(c.code) ? tCategories(c.code) : c.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Select
          value={value.compensation}
          onValueChange={(v) =>
            update({ compensation: v as TaskFiltersState["compensation"] })
          }
        >
          <SelectTrigger aria-label={t("rewardAria")} className="lg:w-40">
            <SelectValue placeholder={t("rewardPlaceholder")} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t("anyReward")}</SelectItem>
            {COMPENSATION_OPTIONS.map((value) => (
              <SelectItem key={value} value={value}>
                {tCompensation(value)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Select
          value={value.radius}
          onValueChange={(v) =>
            update({ radius: v as TaskFiltersState["radius"] })
          }
          disabled={!hasOrigin}
        >
          <SelectTrigger
            aria-label={t("distanceAria")}
            aria-describedby={!hasOrigin ? "radius-helper" : undefined}
            className="lg:w-40"
          >
            <SelectValue placeholder={t("distancePlaceholder")} />
          </SelectTrigger>
          <SelectContent>
            {RADIUS_OPTIONS.map((opt) => (
              <SelectItem key={opt.value} value={opt.value}>
                {opt.km == null
                  ? t("anyDistance")
                  : t("within", { km: opt.km })}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Select
          value={value.recency}
          onValueChange={(v) =>
            update({ recency: v as TaskFiltersState["recency"] })
          }
        >
          <SelectTrigger aria-label={t("recencyAria")} className="lg:w-40">
            <SelectValue placeholder={t("recencyPlaceholder")} />
          </SelectTrigger>
          <SelectContent>
            {RECENCY_OPTIONS.map((value) => (
              <SelectItem key={value} value={value}>
                {t(`recencyOptions.${value}`)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      {!hasOrigin ? (
        <p id="radius-helper" className="text-xs text-muted-foreground">
          {t("addLocationHint")}
        </p>
      ) : null}
    </div>
  )
}

export const DEFAULT_FILTERS: TaskFiltersState = {
  query: "",
  category: "all",
  compensation: "all",
  recency: "any",
  radius: "any",
}
