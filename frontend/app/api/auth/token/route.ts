import type { NextRequest } from "next/server"
import { NextResponse } from "next/server"

import { ACCESS_TOKEN_COOKIE } from "@/lib/auth/cookies"
import { isJwtFresh } from "@/lib/auth/jwt"

// Returns the current access token for SignalR's accessTokenFactory.
// No refresh logic — the caller (fetchSignalRToken in chat-hub) is responsible for
// triggering a refresh on 401 and retrying.
export function GET(request: NextRequest) {
  const accessToken = request.cookies.get(ACCESS_TOKEN_COOKIE)?.value

  if (!accessToken || !isJwtFresh(accessToken, 0)) {
    return NextResponse.json({ error: "Unauthorized" }, { status: 401 })
  }

  return NextResponse.json({ token: accessToken })
}
