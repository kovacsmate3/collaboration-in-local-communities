import type { NextRequest } from "next/server"
import { NextResponse } from "next/server"

import {
  appendRefreshSetCookie,
  clearAuthCookies,
  fetchOwnProfile,
  getFreshAccessToken,
  refreshBackendToken,
  setAccessTokenCookie,
  toAuthUser,
} from "@/lib/auth/backend"
import { getJwtUserClaims } from "@/lib/auth/jwt"
import type { AuthUser, SessionResponse } from "@/lib/auth/types"

export async function GET(request: NextRequest): Promise<Response> {
  let tokenResult = await getFreshAccessToken(request)
  let accessToken = tokenResult.accessToken

  if (!accessToken) {
    const response = NextResponse.json<SessionResponse>({ user: null })
    clearAuthCookies(response, request.url)
    return response
  }

  let user = await getSessionUser(request, accessToken)
  if (!user && !tokenResult.refreshed) {
    const refreshResult = await refreshBackendToken(request)
    tokenResult = {
      accessToken: refreshResult?.auth.accessToken ?? null,
      refreshed: refreshResult,
    }
    accessToken = tokenResult.accessToken
    user = accessToken ? await getSessionUser(request, accessToken) : null
  }

  if (!user) {
    const response = NextResponse.json<SessionResponse>({ user: null })
    clearAuthCookies(response, request.url)
    return response
  }

  const response = NextResponse.json<SessionResponse>({
    user,
  })
  if (tokenResult.refreshed) {
    appendRefreshSetCookie(tokenResult.refreshed, response)
    setAccessTokenCookie(response, request.url, tokenResult.refreshed.auth)
  }

  return response
}

async function getSessionUser(
  request: NextRequest,
  accessToken: string
): Promise<AuthUser | null> {
  const claims = getJwtUserClaims(accessToken)
  if (!claims) {
    return null
  }

  const profileResult = await fetchOwnProfile(request, accessToken)
  if (profileResult.status === "unauthorized") {
    return null
  }

  return toAuthUser(claims, profileResult.profile)
}
