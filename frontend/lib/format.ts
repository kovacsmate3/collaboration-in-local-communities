/**
 * Lightweight relative-time formatter.
 *
 * Intentionally dependency-free for the skeleton.
 * For production-grade i18n consider `Intl.RelativeTimeFormat` with
 * the user's locale and a small bucket helper, or a library like dayjs.
 */
export function formatRelativeTime(input: string | Date): string {
  const date = typeof input === "string" ? new Date(input) : input
  const diffSeconds = Math.round((date.getTime() - Date.now()) / 1000)
  const abs = Math.abs(diffSeconds)

  const rtf = new Intl.RelativeTimeFormat(undefined, { numeric: "auto" })

  if (abs < 60) return rtf.format(Math.round(diffSeconds), "second")
  if (abs < 3600) return rtf.format(Math.round(diffSeconds / 60), "minute")
  if (abs < 86_400) return rtf.format(Math.round(diffSeconds / 3600), "hour")
  if (abs < 30 * 86_400) {
    return rtf.format(Math.round(diffSeconds / 86_400), "day")
  }
  return rtf.format(Math.round(diffSeconds / (30 * 86_400)), "month")
}

export function formatCurrency(amount: number, currency = "HUF"): string {
  return new Intl.NumberFormat(undefined, {
    style: "currency",
    currency,
    maximumFractionDigits: 0,
  }).format(amount)
}
