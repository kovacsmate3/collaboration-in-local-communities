"use client"

import { useTransition } from "react"
import { useRouter } from "next/navigation"
import { HugeiconsIcon } from "@hugeicons/react"
import { GlobalIcon } from "@hugeicons/core-free-icons"
import { useLocale, useTranslations } from "next-intl"

import { setLocale } from "@/app/actions/set-locale"
import { Button } from "@/components/ui/button"
import {
  DEFAULT_LOCALE,
  LOCALES,
  LOCALE_LABELS,
  LOCALE_SHORT_LABELS,
  isSupportedLocale,
  type Locale,
} from "@/i18n/config"

/**
 * Single-button language toggle.
 *
 * For the two-locale case (en / hu) a dropdown is overkill — clicking the
 * button simply flips to the other locale. The current locale's short
 * label (e.g. "EN" or "HU") sits next to the globe icon so users can see
 * the active language at a glance. If a third locale is ever introduced,
 * switch this back to a dropdown.
 *
 * Persists the choice in the NEXT_LOCALE cookie via a server action, then
 * calls `router.refresh()` so server-rendered content re-fetches its
 * translations in the new locale.
 */
export function LanguageSwitcher() {
  const router = useRouter()
  const t = useTranslations("languageSwitcher")
  const rawLocale = useLocale()
  const currentLocale: Locale = isSupportedLocale(rawLocale)
    ? rawLocale
    : DEFAULT_LOCALE
  const [isPending, startTransition] = useTransition()

  const otherLocale =
    LOCALES.find((locale) => locale !== currentLocale) ?? DEFAULT_LOCALE

  function handleToggle() {
    startTransition(async () => {
      await setLocale(otherLocale)
      router.refresh()
    })
  }

  return (
    <Button
      type="button"
      size="sm"
      variant="ghost"
      disabled={isPending}
      onClick={handleToggle}
      aria-label={t("switchTo", { name: LOCALE_LABELS[otherLocale] })}
      className="gap-1.5 px-2"
    >
      <HugeiconsIcon icon={GlobalIcon} className="size-4" />
      <span className="text-xs font-medium">
        {LOCALE_SHORT_LABELS[currentLocale]}
      </span>
    </Button>
  )
}
