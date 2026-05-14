/**
 * Formatting helpers shared across the app.
 *
 * `Intl.NumberFormat` and `Intl.RelativeTimeFormat` instances are
 * relatively expensive to construct and trivially safe to reuse, so we
 * memoize per-key (currency for numbers, locale for relative time).
 * In hot lists - feed cards, task history, message previews - this
 * keeps allocations down without changing semantics.
 */

const currencyFormatters = new Map<string, Intl.NumberFormat>()

function getCurrencyFormatter(currency: string): Intl.NumberFormat {
  let fmt = currencyFormatters.get(currency)
  if (!fmt) {
    fmt = new Intl.NumberFormat(undefined, {
      style: "currency",
      currency,
      maximumFractionDigits: 0,
    })
    currencyFormatters.set(currency, fmt)
  }
  return fmt
}

export function formatCurrency(amount: number, currency = "HUF"): string {
  return getCurrencyFormatter(currency).format(amount)
}

const relativeTimeFormatters = new Map<string, Intl.RelativeTimeFormat>()

function getRelativeTimeFormatter(locale?: string): Intl.RelativeTimeFormat {
  // `undefined` resolves to the runtime default; key it explicitly so
  // the cache hit is stable across calls.
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
export function formatRelativeTime(input: string | Date): string {
  const date = typeof input === "string" ? new Date(input) : input
  const diffSeconds = Math.round((date.getTime() - Date.now()) / 1000)
  const abs = Math.abs(diffSeconds)

  const rtf = getRelativeTimeFormatter()

  if (abs < 60) return rtf.format(Math.round(diffSeconds), "second")
  if (abs < 3600) return rtf.format(Math.round(diffSeconds / 60), "minute")
  if (abs < 86_400) return rtf.format(Math.round(diffSeconds / 3600), "hour")
  if (abs < 30 * 86_400) {
    return rtf.format(Math.round(diffSeconds / 86_400), "day")
  }
  return rtf.format(Math.round(diffSeconds / (30 * 86_400)), "month")
}
