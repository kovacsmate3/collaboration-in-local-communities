import type { NextRequest } from "next/server"
import { NextResponse } from "next/server"

import {
  appendRefreshSetCookie,
  clearAuthCookies,
  getFreshAccessToken,
  setAccessTokenCookie,
} from "@/lib/auth/backend"

export async function GET(request: NextRequest) {
  const tokenResult = await getFreshAccessToken(request)
  if (!tokenResult.accessToken) {
    const response = NextResponse.json(
      { error: "Unauthorized" },
      { status: 401 }
    )
    clearAuthCookies(response, request.url)
    return response
  }

  const response = NextResponse.json({ token: tokenResult.accessToken })
  if (tokenResult.refreshed) {
    appendRefreshSetCookie(tokenResult.refreshed, response)
    setAccessTokenCookie(response, request.url, tokenResult.refreshed.auth)
  }

  return response
}
