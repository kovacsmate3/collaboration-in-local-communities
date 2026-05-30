import { cookies } from "next/headers"
import { getRequestConfig } from "next-intl/server"

import {
  DEFAULT_LOCALE,
  LOCALE_COOKIE_NAME,
  isSupportedLocale,
  type Locale,
} from "@/i18n/config"

/**
 * Server-side per-request locale resolution for next-intl.
 *
 * We use a cookie (not URL prefixes) so:
 *   - existing routes keep their paths
 *   - a shared link doesn't lock the recipient into the sender's language
 *   - anonymous users on auth pages still get a working switcher
 *
 * Resolution order:
 *   1. `NEXT_LOCALE` cookie value (if it's one of the supported locales)
 *   2. DEFAULT_LOCALE
 *
 * Browser Accept-Language is intentionally not read here — once a user
 * makes a choice, that choice wins; first-visit users land in
 * DEFAULT_LOCALE which is the safest reproducible default for testing.
 */
export default getRequestConfig(async () => {
  const cookieStore = await cookies()
  const cookieLocale = cookieStore.get(LOCALE_COOKIE_NAME)?.value
  const locale: Locale = isSupportedLocale(cookieLocale)
    ? cookieLocale
    : DEFAULT_LOCALE

  return {
    locale,
    messages: (await import(`@/messages/${locale}.json`)).default,
  }
})
