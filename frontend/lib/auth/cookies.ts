import { AUTH_COOKIES } from "@/lib/auth/constants"

export const ACCESS_TOKEN_COOKIE = AUTH_COOKIES.accessToken
export const REFRESH_TOKEN_COOKIE = AUTH_COOKIES.refreshToken

export function isSecureRequest(requestUrl: string): boolean {
  return new URL(requestUrl).protocol === "https:"
}

export function getAccessTokenCookieOptions(
  requestUrl: string,
  expiresAt: string
) {
  return {
    httpOnly: true,
    secure: isSecureRequest(requestUrl),
    sameSite: "lax" as const,
    path: "/",
    expires: new Date(expiresAt),
  }
}

export function getExpiredCookieOptions(requestUrl: string) {
  return {
    httpOnly: true,
    secure: isSecureRequest(requestUrl),
    sameSite: "lax" as const,
    path: "/",
    maxAge: 0,
  }
}
