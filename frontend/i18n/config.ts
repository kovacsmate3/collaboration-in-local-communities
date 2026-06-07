/**
 * Single source of truth for the supported locales.
 *
 * Adding a new language is: add the code here, drop a matching JSON
 * catalog under `messages/`, optionally add a display label. The request
 * config and the switcher pick it up automatically.
 */
export const LOCALES = ["en", "hu"] as const

export type Locale = (typeof LOCALES)[number]

export const DEFAULT_LOCALE: Locale = "en"

/**
 * Cookie name used to persist the user's language choice. Matches the
 * next-intl convention so any future server-side helpers from the lib
 * read the same cookie without extra config.
 */
export const LOCALE_COOKIE_NAME = "NEXT_LOCALE"

/** Long-lived cookie (one year) — the choice is sticky across sessions. */
export const LOCALE_COOKIE_MAX_AGE_SECONDS = 60 * 60 * 24 * 365

/**
 * Cookie attributes used by the server action that persists the locale
 * choice. Kept here so request.ts and set-locale.ts share the same
 * single source of truth — changing them in one place is enough.
 */
export const LOCALE_COOKIE_OPTIONS = {
  path: "/",
  maxAge: LOCALE_COOKIE_MAX_AGE_SECONDS,
  sameSite: "lax" as const,
}

/**
 * Human-readable, native-spelling labels for the switcher. Hungarian
 * users see "Magyar", not "Hungarian", which is the expected affordance
 * for language pickers.
 */
export const LOCALE_LABELS: Record<Locale, string> = {
  en: "English",
  hu: "Magyar",
}

/** Short uppercase code used as the compact switcher trigger label. */
export const LOCALE_SHORT_LABELS: Record<Locale, string> = {
  en: "EN",
  hu: "HU",
}

/** Type-safe check that a string is a supported locale. */
export function isSupportedLocale(
  value: string | undefined | null
): value is Locale {
  return value != null && (LOCALES as readonly string[]).includes(value)
}
