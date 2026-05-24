import { NextResponse, type NextRequest } from "next/server"

import { clearAuthCookies, fetchTermsState } from "@/lib/auth/backend"
import { ACCESS_TOKEN_COOKIE, REFRESH_TOKEN_COOKIE } from "@/lib/auth/cookies"
import { APP_HOME_ROUTES } from "@/lib/auth/constants"
import {
  getHomePathForToken,
  getLoginRedirectUrl,
  getTermsRedirectUrl,
  isAdminPath,
  isAuthPath,
  isProtectedPath,
} from "@/lib/auth/functions"
import { getJwtRoles, isJwtFresh } from "@/lib/auth/jwt"

export async function proxy(request: NextRequest): Promise<NextResponse> {
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

  // Treat a refresh cookie as an optimistic session marker. The client-side
  // AuthProvider owns refresh rotation so concurrent page/API requests do not
  // race the backend's single-use refresh token.
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
    return redirectToLogin(request, pathname)
  }

  // Only enforce admin-role redirects when we have a fresh token to decode.
  // If the token is stale but the refresh cookie is present, let the page load;
  // the AuthProvider refreshes the access token and API calls still enforce 403s.
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

  if (!tokenIsFresh || !accessToken) {
    return NextResponse.next()
  }

  const termsResult = await fetchTermsState(request, accessToken)
  if (termsResult.status === "unauthorized") {
    return redirectToLogin(request, pathname)
  }

  if (!termsResult.terms.hasAccepted) {
    return NextResponse.redirect(
      getTermsRedirectUrl(request.url, pathname, request.nextUrl.search)
    )
  }

  return NextResponse.next()
}

export const config = {
  matcher: [
    "/((?!api|_next/static|_next/image|favicon.ico|sitemap.xml|robots.txt).*)",
  ],
}

function redirectToLogin(request: NextRequest, pathname: string): NextResponse {
  const response = NextResponse.redirect(
    getLoginRedirectUrl(request.url, pathname, request.nextUrl.search)
  )
  clearAuthCookies(response, request.url)
  return response
}
