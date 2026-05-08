import type { NextRequest } from "next/server"
import { NextResponse } from "next/server"

import { ACCESS_TOKEN_COOKIE } from "@/lib/auth/cookies"
import {
  appendBackendSetCookie,
  clearAuthCookies,
  fetchOwnProfile,
  refreshBackendToken,
  setAccessTokenCookie,
  toAuthUser,
} from "@/lib/auth/backend"
import { getJwtUserClaims, isJwtFresh } from "@/lib/auth/jwt"
import type { AuthUser, SessionResponse } from "@/lib/auth/types"

export async function GET(request: NextRequest): Promise<Response> {
  const accessToken = request.cookies.get(ACCESS_TOKEN_COOKIE)?.value

  if (accessToken) {
    const user = await getSessionUser(accessToken)
    if (user) {
      return NextResponse.json<SessionResponse>({
        user,
      })
    }
  }

  const refreshResult = await refreshBackendToken(request)
  if (!refreshResult) {
    const response = NextResponse.json<SessionResponse>({ user: null })
    clearAuthCookies(response, request.url)
    return response
  }

  const user = await getSessionUser(refreshResult.auth.accessToken)
  if (!user) {
    const response = NextResponse.json<SessionResponse>({ user: null })
    appendBackendSetCookie(refreshResult.response, response)
    clearAuthCookies(response, request.url)
    return response
  }

  const response = NextResponse.json<SessionResponse>({
    user,
  })
  appendBackendSetCookie(refreshResult.response, response)
  setAccessTokenCookie(response, request.url, refreshResult.auth)

  return response
}

async function getSessionUser(accessToken: string): Promise<AuthUser | null> {
  if (!isJwtFresh(accessToken)) {
    return null
  }

  const claims = getJwtUserClaims(accessToken)
  if (!claims) {
    return null
  }

  const profileResult = await fetchOwnProfile(accessToken)
  if (profileResult.status === "unauthorized") {
    return null
  }

  return toAuthUser(claims, profileResult.profile)
}
