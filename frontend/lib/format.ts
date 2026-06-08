/**
 * Formatting helpers shared across the app.
 *
 * `Intl.NumberFormat`, `Intl.DateTimeFormat`, and `Intl.RelativeTimeFormat`
 * instances are relatively expensive to construct and trivially safe to
 * reuse, so we memoize per cache key (locale + currency for numbers,
 * locale for dates and relative time). In hot lists - feed cards, task
 * history, message previews - this keeps allocations down without
 * changing semantics.
 *
 * Pass the active i18n `locale` explicitly from the call site (via
 * `useLocale()` in client components or `getLocale()` in server
 * components). Omitting it falls back to the JS runtime's default,
 * which on the server is typically `en-US` and on the client is the
 * browser's navigator.language — neither of which match the i18n cookie.
 */

const dateFormatters = new Map<string, Intl.DateTimeFormat>()

function getDateFormatter(locale?: string): Intl.DateTimeFormat {
  const key = locale ?? "__default__"
  let fmt = dateFormatters.get(key)
  if (!fmt) {
    fmt = new Intl.DateTimeFormat(locale, {
      year: "numeric",
      month: "short",
      day: "numeric",
    })
    dateFormatters.set(key, fmt)
  }
  return fmt
}

/** Formats an ISO date string or Date as a short locale date (e.g. "May 14, 2026"). */
export function formatDate(input: string | Date, locale?: string): string {
  const date = typeof input === "string" ? new Date(input) : input
  return getDateFormatter(locale).format(date)
}

const datetimeFormatters = new Map<string, Intl.DateTimeFormat>()

function getDatetimeFormatter(locale?: string): Intl.DateTimeFormat {
  const key = locale ?? "__default__"
  let fmt = datetimeFormatters.get(key)
  if (!fmt) {
    fmt = new Intl.DateTimeFormat(locale, {
      year: "numeric",
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
    })
    datetimeFormatters.set(key, fmt)
  }
  return fmt
}

/** Formats an ISO date string or Date as a precise locale datetime (e.g. "May 14, 2026, 09:30:00 AM"). */
export function formatDatetime(input: string | Date, locale?: string): string {
  const date = typeof input === "string" ? new Date(input) : input
  return getDatetimeFormatter(locale).format(date)
}

const currencyFormatters = new Map<string, Intl.NumberFormat>()

function getCurrencyFormatter(
  currency: string,
  locale?: string
): Intl.NumberFormat {
  const key = `${locale ?? "__default__"}|${currency}`
  let fmt = currencyFormatters.get(key)
  if (!fmt) {
    fmt = new Intl.NumberFormat(locale, {
      style: "currency",
      currency,
      maximumFractionDigits: 0,
    })
    currencyFormatters.set(key, fmt)
  }
  return fmt
}

export function formatCurrency(
  amount: number,
  currency = "HUF",
  locale?: string
): string {
  return getCurrencyFormatter(currency, locale).format(amount)
}

const relativeTimeFormatters = new Map<string, Intl.RelativeTimeFormat>()

function getRelativeTimeFormatter(locale?: string): Intl.RelativeTimeFormat {
  const key = locale ?? "__default__"
  let fmt = relativeTimeFormatters.get(key)
  if (!fmt) {
    fmt = new Intl.RelativeTimeFormat(locale, { numeric: "auto" })
    relativeTimeFormatters.set(key, fmt)
  }
  return fmt
}

/**
 * Lightweight relative-time formatter.
 *
 * Intentionally dependency-free for the skeleton. For richer i18n
 * (locale-aware bucketing, "yesterday" vs "1 day ago", plural rules)
 * consider dayjs/date-fns later - the call sites won't need to change.
 */
export function formatRelativeTime(
  input: string | Date,
  locale?: string
): string {
  const date = typeof input === "string" ? new Date(input) : input
  const diffSeconds = Math.round((date.getTime() - Date.now()) / 1000)
  const abs = Math.abs(diffSeconds)

  const rtf = getRelativeTimeFormatter(locale)

  if (abs < 60) return rtf.format(Math.round(diffSeconds), "second")
  if (abs < 3600) return rtf.format(Math.round(diffSeconds / 60), "minute")
  if (abs < 86_400) return rtf.format(Math.round(diffSeconds / 3600), "hour")
  if (abs < 30 * 86_400) {
    return rtf.format(Math.round(diffSeconds / 86_400), "day")
  }
  return rtf.format(Math.round(diffSeconds / (30 * 86_400)), "month")
}
