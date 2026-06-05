"use server"

import { cookies } from "next/headers"

import {
  LOCALE_COOKIE_NAME,
  LOCALE_COOKIE_OPTIONS,
  isSupportedLocale,
} from "@/i18n/config"

/**
 * Persists the user's language choice in the `NEXT_LOCALE` cookie.
 *
 * Reasons for a server action instead of a client-set document.cookie:
 *   - the server reads this cookie during the very next render to pick
 *     the right messages, so it must be set before the refresh fires
 *   - centralizes locale persistence on the server (cookie name + shape
 *     are shared via i18n/config so request.ts and this action stay in
 *     sync)
 *
 * Throws on an unsupported locale rather than silently accepting it,
 * so a bad client value surfaces in the network panel during development
 * instead of writing junk into the cookie.
 */
export async function setLocale(locale: string): Promise<void> {
  if (!isSupportedLocale(locale)) {
    throw new Error(`Unsupported locale: ${locale}`)
  }

  const cookieStore = await cookies()
  cookieStore.set(LOCALE_COOKIE_NAME, locale, LOCALE_COOKIE_OPTIONS)
}
