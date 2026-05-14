import type { NextRequest } from "next/server"
import { NextResponse } from "next/server"

import { ACCESS_TOKEN_COOKIE } from "@/lib/auth/cookies"

export async function GET(request: NextRequest) {
  const token = request.cookies.get(ACCESS_TOKEN_COOKIE)?.value
  if (!token) {
    return NextResponse.json({ error: "Unauthorized" }, { status: 401 })
  }
  return NextResponse.json({ token })
}
