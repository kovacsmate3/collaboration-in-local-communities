import { NextResponse, type NextRequest } from "next/server"

import {
  ACCESS_TOKEN_COOKIE,
  getExpiredCookieOptions,
} from "@/lib/auth/cookies"
import {
  appendBackendSetCookie,
  refreshBackendToken,
  setAccessTokenCookie,
} from "@/lib/auth/backend"
import {
  getHomePathForToken,
  getLoginRedirectUrl,
  isAdminPath,
  isAuthPath,
  isProtectedPath,
} from "@/lib/auth/functions"
import { getJwtRoles, isJwtFresh } from "@/lib/auth/jwt"

export async function middleware(request: NextRequest): Promise<NextResponse> {
  const pathname = request.nextUrl.pathname
  const isAuthRoute = isAuthPath(pathname)
  const isProtectedRoute = isProtectedPath(pathname)

  if (!isAuthRoute && !isProtectedRoute) {
    return NextResponse.next()
  }

  let accessToken = request.cookies.get(ACCESS_TOKEN_COOKIE)?.value
  let refreshed:
    | Awaited<ReturnType<typeof refreshBackendToken>>
    | null
    | undefined

  if (!accessToken || !isJwtFresh(accessToken)) {
    refreshed = await refreshBackendToken(request)
    accessToken = refreshed?.auth.accessToken
  }

  if (isAuthRoute && accessToken && isJwtFresh(accessToken, 0)) {
    const response = NextResponse.redirect(
      new URL(getHomePathForToken(accessToken), request.url)
    )
    applyRefreshResult(response, request, refreshed)
    return response
  }

  if (!isProtectedRoute) {
    const response = NextResponse.next()
    applyRefreshResult(response, request, refreshed)
    return response
  }

  if (!accessToken || !isJwtFresh(accessToken, 0)) {
    const response = NextResponse.redirect(
      getLoginRedirectUrl(request.url, pathname, request.nextUrl.search)
    )
    response.cookies.set(
      ACCESS_TOKEN_COOKIE,
      "",
      getExpiredCookieOptions(request.url)
    )
    return response
  }

  if (isAdminPath(pathname) && !getJwtRoles(accessToken).includes("Admin")) {
    const response = NextResponse.redirect(
      new URL(getHomePathForToken(accessToken), request.url)
    )
    applyRefreshResult(response, request, refreshed)
    return response
  }

  const response = NextResponse.next()
  applyRefreshResult(response, request, refreshed)
  return response
}

export const config = {
  matcher: [
    "/((?!api|_next/static|_next/image|favicon.ico|sitemap.xml|robots.txt).*)",
  ],
}

function applyRefreshResult(
  response: NextResponse,
  request: NextRequest,
  refreshed: Awaited<ReturnType<typeof refreshBackendToken>> | null | undefined
) {
  if (!refreshed) {
    return
  }

  appendBackendSetCookie(refreshed.response, response)
  setAccessTokenCookie(response, request.url, refreshed.auth)
}
