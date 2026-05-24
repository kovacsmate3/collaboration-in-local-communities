import type { NextRequest } from "next/server"
import { NextResponse } from "next/server"

import {
  fetchOwnProfile,
  fetchTermsState,
  toAuthUser,
} from "@/lib/auth/backend"
import { ACCESS_TOKEN_COOKIE } from "@/lib/auth/cookies"
import { decodeJwtPayload, getJwtUserClaims, isJwtFresh } from "@/lib/auth/jwt"
import type { SessionResponse } from "@/lib/auth/types"

// Reads the current session without touching the backend's refresh endpoint.
// If the access token is missing or expired, returns 401. The caller
// (AuthProvider) is responsible for triggering a refresh and re-fetching.
export async function GET(request: NextRequest): Promise<Response> {
  const accessToken = request.cookies.get(ACCESS_TOKEN_COOKIE)?.value

  if (!accessToken || !isJwtFresh(accessToken, 0)) {
    return unauthorizedSession()
  }

  const claims = getJwtUserClaims(accessToken)
  if (!claims) {
    return unauthorizedSession()
  }

  const [profileResult, termsResult] = await Promise.all([
    fetchOwnProfile(request, accessToken),
    fetchTermsState(request, accessToken),
  ])

  if (
    profileResult.status === "unauthorized" ||
    termsResult.status === "unauthorized"
  ) {
    return unauthorizedSession()
  }

  const user = toAuthUser(claims, profileResult.profile, termsResult.terms)
  const exp = decodeJwtPayload(accessToken)?.exp
  const expiresAt = exp ? new Date(exp * 1000).toISOString() : null

  return NextResponse.json<SessionResponse>({ user, expiresAt })
}

function unauthorizedSession(): Response {
  return NextResponse.json<SessionResponse>(
    { user: null, expiresAt: null },
    { status: 401 }
  )
}
