"use client"

import { HugeiconsIcon } from "@hugeicons/react"
import { useTranslations } from "next-intl"

import { Badge } from "@/components/ui/badge"
import { getIconForKey } from "@/lib/icon-registry"

interface CategoryBadgeProps {
  /**
   * Display label for the category. Accepts either an already-translated
   * string (e.g. `t("category.moving")`) or the raw English name returned
   * by the backend (e.g. `"Pet Care"`). For backend-supplied names we
   * derive a translation key by slugifying (lowercase + spaces →
   * underscores) and look it up against `tasks.categories`; if no
   * translation exists the original `label` is rendered unchanged so
   * admin-added custom categories still display.
   */
  label: string
  icon?: string
}

function slugify(name: string): string {
  return name.toLowerCase().replace(/ /g, "_").replace(/[^\w]/g, "")
}

export function CategoryBadge({ label, icon }: CategoryBadgeProps) {
  const t = useTranslations("tasks.categories")
  const Icon = icon ? getIconForKey(icon) : null
  const key = slugify(label)
  const translated = key && t.has(key) ? t(key) : label

  return (
    <Badge variant="muted">
      {Icon ? (
        <HugeiconsIcon
          icon={Icon}
          data-icon="inline-start"
          aria-hidden="true"
        />
      ) : null}
      {translated}
    </Badge>
  )
}
