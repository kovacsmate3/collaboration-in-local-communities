"use client"

import { useTransition } from "react"
import { useRouter } from "next/navigation"
import { HugeiconsIcon } from "@hugeicons/react"
import { GlobalIcon } from "@hugeicons/core-free-icons"
import { useLocale } from "next-intl"

import { setLocale } from "@/app/actions/set-locale"
import {
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
} from "@/components/ui/dropdown-menu"
import {
  DEFAULT_LOCALE,
  LOCALES,
  LOCALE_LABELS,
  isSupportedLocale,
  type Locale,
} from "@/i18n/config"

/**
 * Language picker rendered inline inside the authenticated user menu.
 *
 * For anonymous routes (login/register) the standalone `<LanguageSwitcher />`
 * is still shown in the top-right of the auth layout — this variant exists
 * so signed-in users find the picker where they look for account settings
 * rather than scanning a separate header button.
 *
 * Uses a radio group so the active locale is communicated to assistive tech
 * out of the box, no custom aria wiring needed.
 */
export function LanguageMenuItems({ groupLabel }: { groupLabel: string }) {
  const router = useRouter()
  const rawLocale = useLocale()
  const currentLocale: Locale = isSupportedLocale(rawLocale)
    ? rawLocale
    : DEFAULT_LOCALE
  const [isPending, startTransition] = useTransition()

  function handleSelect(next: string) {
    if (!isSupportedLocale(next) || next === currentLocale) return
    startTransition(async () => {
      await setLocale(next)
      router.refresh()
    })
  }

  return (
    <>
      <DropdownMenuLabel className="flex items-center gap-2 text-xs font-medium text-muted-foreground">
        <HugeiconsIcon icon={GlobalIcon} className="size-3.5" />
        {groupLabel}
      </DropdownMenuLabel>
      <DropdownMenuRadioGroup
        value={currentLocale}
        onValueChange={handleSelect}
      >
        {LOCALES.map((locale) => (
          <DropdownMenuRadioItem
            key={locale}
            value={locale}
            disabled={isPending}
          >
            {LOCALE_LABELS[locale]}
          </DropdownMenuRadioItem>
        ))}
      </DropdownMenuRadioGroup>
    </>
  )
}
