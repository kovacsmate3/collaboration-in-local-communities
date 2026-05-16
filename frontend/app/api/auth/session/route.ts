import type { NextRequest } from "next/server"
import { NextResponse } from "next/server"

import { ACCESS_TOKEN_COOKIE } from "@/lib/auth/cookies"
import { fetchOwnProfile, toAuthUser } from "@/lib/auth/backend"
import { decodeJwtPayload, getJwtUserClaims, isJwtFresh } from "@/lib/auth/jwt"
import type { SessionResponse } from "@/lib/auth/types"

// Reads the current session without touching the backend's refresh endpoint.
// If the access token is missing or expired, returns 401 — the caller
// (AuthProvider) is responsible for triggering a refresh and re-fetching.
export async function GET(request: NextRequest): Promise<Response> {
  const accessToken = request.cookies.get(ACCESS_TOKEN_COOKIE)?.value

  if (!accessToken || !isJwtFresh(accessToken, 0)) {
    return NextResponse.json<SessionResponse>(
      { user: null, expiresAt: null },
      { status: 401 }
    )
  }

  const claims = getJwtUserClaims(accessToken)
  if (!claims) {
    return NextResponse.json<SessionResponse>(
      { user: null, expiresAt: null },
      { status: 401 }
    )
  }

  const profileResult = await fetchOwnProfile(request, accessToken)
  if (profileResult.status === "unauthorized") {
    return NextResponse.json<SessionResponse>(
      { user: null, expiresAt: null },
      { status: 401 }
    )
  }

  const user = toAuthUser(claims, profileResult.profile)
  const exp = decodeJwtPayload(accessToken)?.exp
  const expiresAt = exp ? new Date(exp * 1000).toISOString() : null

  return NextResponse.json<SessionResponse>({ user, expiresAt })
}
