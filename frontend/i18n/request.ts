import { cookies, headers } from "next/headers"
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
 *   1. NEXT_LOCALE cookie — the user's explicit choice; once set, it wins.
 *   2. Accept-Language request header — best-fit match against the locales
 *      we support, using q-value ordering and primary-subtag fallback.
 *      So a Hungarian browser lands in Hungarian on first visit.
 *   3. DEFAULT_LOCALE — last-resort baseline for clients that don't send
 *      an Accept-Language header at all.
 */
export default getRequestConfig(async () => {
  const cookieStore = await cookies()
  const cookieLocale = cookieStore.get(LOCALE_COOKIE_NAME)?.value
  if (isSupportedLocale(cookieLocale)) {
    return loadMessagesFor(cookieLocale)
  }

  const headerStore = await headers()
  const acceptLanguage = headerStore.get("accept-language")
  const negotiated = pickFromAcceptLanguage(acceptLanguage)
  if (negotiated) {
    return loadMessagesFor(negotiated)
  }

  return loadMessagesFor(DEFAULT_LOCALE)
})

async function loadMessagesFor(locale: Locale) {
  return {
    locale,
    messages: (await import(`@/messages/${locale}.json`)).default,
  }
}

/**
 * Picks the best supported locale from an Accept-Language header.
 *
 * Implements a minimal subset of RFC 4647: sort entries by their q-value
 * (default 1.0), then for each tag try an exact match first, then fall
 * back to the primary subtag (e.g. `hu-HU` → `hu`). Returns null if none
 * of the requested tags map to a supported locale.
 *
 * Kept inline (no new dependency) because we only support two locales —
 * adding @formatjs/intl-localematcher would be over-engineering here.
 */
function pickFromAcceptLanguage(header: string | null): Locale | null {
  if (!header) return null

  const entries = header
    .split(",")
    .map((part) => {
      const [rawTag, ...params] = part
        .trim()
        .split(";")
        .map((segment) => segment.trim())
      const qParam = params.find((p) => p.startsWith("q="))
      const parsedQ = qParam ? Number.parseFloat(qParam.slice(2)) : 1.0
      return {
        tag: rawTag.toLowerCase(),
        q: Number.isFinite(parsedQ) ? parsedQ : 1.0,
      }
    })
    .filter((entry) => entry.tag.length > 0 && entry.q > 0)
    .sort((a, b) => b.q - a.q)

  for (const { tag } of entries) {
    if (isSupportedLocale(tag)) {
      return tag
    }
    const primary = tag.split("-")[0]
    if (isSupportedLocale(primary)) {
      return primary
    }
  }

  return null
}
