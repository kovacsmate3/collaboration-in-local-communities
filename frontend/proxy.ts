import { NextResponse, type NextRequest } from "next/server"

import { ACCESS_TOKEN_COOKIE, REFRESH_TOKEN_COOKIE } from "@/lib/auth/cookies"
import { clearAuthCookies } from "@/lib/auth/backend"
import {
  getHomePathForToken,
  getLoginRedirectUrl,
  isAdminPath,
  isAuthPath,
  isProtectedPath,
} from "@/lib/auth/functions"
import { getJwtRoles, isJwtFresh } from "@/lib/auth/jwt"
import { APP_HOME_ROUTES } from "@/lib/auth/constants"

export function proxy(request: NextRequest): NextResponse {
  const pathname = request.nextUrl.pathname
  const isAuthRoute = isAuthPath(pathname)
  const isProtectedRoute = isProtectedPath(pathname)
  const isRootPath = pathname === "/"

  if (!isAuthRoute && !isProtectedRoute && !isRootPath) {
    return NextResponse.next()
  }

  const accessToken = request.cookies.get(ACCESS_TOKEN_COOKIE)?.value
  const hasRefreshToken = Boolean(
    request.cookies.get(REFRESH_TOKEN_COOKIE)?.value
  )
  const tokenIsFresh = accessToken ? isJwtFresh(accessToken, 0) : false

  // Treated as logged in when the access token is still valid, OR when the refresh
  // token cookie is present (meaning the API proxy will obtain a new access token
  // on the first API call without needing the middleware to do it here).
  const isLoggedIn = tokenIsFresh || hasRefreshToken

  if ((isAuthRoute || isRootPath) && isLoggedIn) {
    const homePath =
      tokenIsFresh && accessToken
        ? getHomePathForToken(accessToken)
        : APP_HOME_ROUTES.user
    return NextResponse.redirect(new URL(homePath, request.url))
  }

  if (!isProtectedRoute) {
    return NextResponse.next()
  }

  if (!isLoggedIn) {
    const response = NextResponse.redirect(
      getLoginRedirectUrl(request.url, pathname, request.nextUrl.search)
    )
    clearAuthCookies(response, request.url)
    return response
  }

  // Only enforce admin-role redirect when we have a fresh token to decode.
  // If the token is stale but the refresh cookie is present, let the page
  // load — the API proxy will refresh the token and the admin UI handles 403s.
  if (
    isAdminPath(pathname) &&
    tokenIsFresh &&
    accessToken &&
    !getJwtRoles(accessToken).includes("Admin")
  ) {
    return NextResponse.redirect(
      new URL(getHomePathForToken(accessToken), request.url)
    )
  }

  return NextResponse.next()
}

export const config = {
  matcher: [
    "/((?!api|_next/static|_next/image|favicon.ico|sitemap.xml|robots.txt).*)",
  ],
}
