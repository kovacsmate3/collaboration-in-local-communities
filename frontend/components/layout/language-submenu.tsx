"use client"

import { useTransition } from "react"
import { useRouter } from "next/navigation"
import { HugeiconsIcon } from "@hugeicons/react"
import { GlobalIcon } from "@hugeicons/core-free-icons"
import { useLocale, useTranslations } from "next-intl"

import { setLocale } from "@/app/actions/set-locale"
import {
  DropdownMenuItem,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
} from "@/components/ui/dropdown-menu"
import {
  DEFAULT_LOCALE,
  LOCALES,
  LOCALE_LABELS,
  isSupportedLocale,
  type Locale,
} from "@/i18n/config"

/**
 * Language picker nested inside the user dropdown.
 *
 * Lives next to Profile / Settings / Logout — keeps the app header
 * uncluttered while keeping the choice one click deep for authenticated
 * users. Anonymous users on the auth pages still get a stand-alone
 * `LanguageSwitcher` (see the auth layout) because they have no user menu
 * to nest inside.
 */
export function LanguageSubmenu() {
  const router = useRouter()
  const t = useTranslations("languageSwitcher")
  const rawLocale = useLocale()
  const currentLocale: Locale = isSupportedLocale(rawLocale)
    ? rawLocale
    : DEFAULT_LOCALE
  const [isPending, startTransition] = useTransition()

  function handleSelect(next: Locale) {
    if (next === currentLocale) return
    startTransition(async () => {
      await setLocale(next)
      router.refresh()
    })
  }

  return (
    <DropdownMenuSub>
      <DropdownMenuSubTrigger disabled={isPending}>
        <HugeiconsIcon icon={GlobalIcon} className="size-4" />
        <span>{t("label")}</span>
        <span className="ml-auto text-xs text-muted-foreground">
          {LOCALE_LABELS[currentLocale]}
        </span>
      </DropdownMenuSubTrigger>
      <DropdownMenuSubContent>
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
      </DropdownMenuSubContent>
    </DropdownMenuSub>
  )
}
