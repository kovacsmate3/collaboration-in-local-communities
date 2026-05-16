import type { NextRequest } from "next/server"
import { NextResponse } from "next/server"

import {
  appendRefreshSetCookie,
  clearAuthCookies,
  refreshBackendToken,
  setAccessTokenCookie,
} from "@/lib/auth/backend"
import type { RefreshResponse } from "@/lib/auth/types"

// The single endpoint that talks to the backend's /api/auth/refresh.
// Called only by AuthProvider (through a singleton in-flight promise) so we
// never hit the backend's atomic-rotation race with concurrent refresh attempts.
export async function POST(request: NextRequest): Promise<Response> {
  const result = await refreshBackendToken(request)

  if (!result) {
    const response = NextResponse.json<RefreshResponse>(
      { expiresAt: null },
      { status: 401 }
    )
    clearAuthCookies(response, request.url)
    return response
  }

  const response = NextResponse.json<RefreshResponse>({
    expiresAt: result.auth.accessTokenExpiresAt,
  })
  // ORDER MATTERS: response.cookies.set() wipes all Set-Cookie headers and
  // rewrites them from its internal Map. Call setAccessTokenCookie BEFORE
  // appendRefreshSetCookie or the refresh-token cookie is silently dropped.
  setAccessTokenCookie(response, request.url, result.auth)
  appendRefreshSetCookie(result, response)
  return response
}
