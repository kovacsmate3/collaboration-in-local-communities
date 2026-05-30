"use client"

import { useTransition } from "react"
import { useRouter } from "next/navigation"
import { HugeiconsIcon } from "@hugeicons/react"
import { GlobalIcon } from "@hugeicons/core-free-icons"
import { useLocale, useTranslations } from "next-intl"

import { setLocale } from "@/app/actions/set-locale"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  LOCALES,
  LOCALE_LABELS,
  LOCALE_SHORT_LABELS,
  isSupportedLocale,
  type Locale,
} from "@/i18n/config"

/**
 * Dropdown that switches the UI language. Persists the choice in the
 * NEXT_LOCALE cookie via a server action so the next server render
 * picks up the new locale immediately, then refreshes the route tree
 * so already-rendered content re-fetches its translations.
 *
 * Visible on every page so anonymous users (auth flow) can switch too.
 */
export function LanguageSwitcher() {
  const router = useRouter()
  const t = useTranslations("languageSwitcher")
  const rawLocale = useLocale()
  const currentLocale: Locale = isSupportedLocale(rawLocale) ? rawLocale : "en"
  const [isPending, startTransition] = useTransition()

  function handleSelect(next: Locale) {
    if (next === currentLocale) return
    startTransition(async () => {
      await setLocale(next)
      router.refresh()
    })
  }

  return (
    <DropdownMenu modal={false}>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          size="sm"
          variant="ghost"
          disabled={isPending}
          aria-label={t("label")}
          className="gap-1.5 px-2"
        >
          <HugeiconsIcon icon={GlobalIcon} className="size-4" />
          <span className="text-xs font-medium">
            {LOCALE_SHORT_LABELS[currentLocale]}
          </span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {LOCALES.map((locale) => {
          const isCurrent = locale === currentLocale
          return (
            <DropdownMenuItem
              key={locale}
              aria-current={isCurrent ? "true" : undefined}
              onSelect={(event) => {
                event.preventDefault()
                handleSelect(locale)
              }}
            >
              <span>{LOCALE_LABELS[locale]}</span>
              {isCurrent ? (
                <span className="ml-auto text-xs text-muted-foreground">
                  {t("selectedSuffix")}
                </span>
              ) : null}
            </DropdownMenuItem>
          )
        })}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
