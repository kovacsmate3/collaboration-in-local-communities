import { NextResponse, type NextRequest } from "next/server"

import {
  ACCESS_TOKEN_COOKIE,
  REFRESH_TOKEN_COOKIE,
  getAccessTokenCookieOptions,
  getExpiredCookieOptions,
} from "@/lib/auth/cookies"
import {
  BACKEND_AUTH_PATHS,
  BACKEND_PROFILE_PATHS,
  DEFAULT_BACKEND_API_URL,
} from "@/lib/auth/constants"
import type { JwtUserClaims } from "@/lib/auth/jwt"
import type {
  AuthUser,
  BackendAuthResponse,
  OwnProfileResponse,
  SafeAuthResponse,
  UserRole,
} from "@/lib/auth/types"

type HeadersWithSetCookie = Headers & {
  getSetCookie?: () => string[]
}

type OwnProfileFetchResult =
  | {
      status: "ok"
      profile: OwnProfileResponse | null
    }
  | {
      status: "unauthorized"
    }

export function getBackendUrl(path: string[], requestUrl?: string): URL {
  const backendBaseUrl = process.env.API_URL ?? DEFAULT_BACKEND_API_URL
  const backendUrl = new URL(`/api/${path.join("/")}`, backendBaseUrl)

  if (requestUrl) {
    backendUrl.search = new URL(requestUrl).search
  }

  return backendUrl
}

export function toAuthUser(
  claims: JwtUserClaims,
  profile: OwnProfileResponse | null
): AuthUser {
  const roles = normalizeRoles(claims.roles)
  const isAdmin = roles.includes("Admin")

  return {
    id: claims.userId,
    name: profile?.displayName ?? claims.email,
    email: claims.email,
    role: isAdmin ? "Admin" : "User",
    roles,
    avatarUrl: profile?.photoUrl ?? undefined,
    profileId: profile?.id,
    isProfileCompleted: profile?.isProfileCompleted ?? false,
  }
}

export function toSafeAuthResponse(
  response: BackendAuthResponse
): SafeAuthResponse {
  return {
    userId: response.userId,
    email: response.email,
    tokenType: response.tokenType,
    accessTokenExpiresAt: response.accessTokenExpiresAt,
    refreshTokenExpiresAt: response.refreshTokenExpiresAt,
  }
}

export function setAccessTokenCookie(
  response: NextResponse,
  requestUrl: string,
  auth: BackendAuthResponse
) {
  response.cookies.set(
    ACCESS_TOKEN_COOKIE,
    auth.accessToken,
    getAccessTokenCookieOptions(requestUrl, auth.accessTokenExpiresAt)
  )
}

export function clearAuthCookies(response: NextResponse, requestUrl: string) {
  const options = getExpiredCookieOptions(requestUrl)
  response.cookies.set(ACCESS_TOKEN_COOKIE, "", options)
  response.cookies.set(REFRESH_TOKEN_COOKIE, "", options)
}

export function appendBackendSetCookie(
  backendResponse: Response,
  response: NextResponse
) {
  for (const cookie of getSetCookieHeaders(backendResponse.headers)) {
    response.headers.append("set-cookie", cookie)
  }
}

export function getRefreshCookieHeader(request: NextRequest): string | null {
  const refreshToken = request.cookies.get(REFRESH_TOKEN_COOKIE)?.value
  return refreshToken ? `${REFRESH_TOKEN_COOKIE}=${refreshToken}` : null
}

export async function readJson<T>(response: Response): Promise<T | null> {
  try {
    return (await response.json()) as T
  } catch {
    return null
  }
}

export async function refreshBackendToken(
  request: NextRequest
): Promise<{ auth: BackendAuthResponse; response: Response } | null> {
  const cookie = getRefreshCookieHeader(request)
  if (!cookie) {
    return null
  }

  const response = await fetch(getBackendUrl([...BACKEND_AUTH_PATHS.refresh]), {
    method: "POST",
    headers: {
      cookie,
    },
    cache: "no-store",
  })

  if (!response.ok) {
    return null
  }

  const auth = await readJson<BackendAuthResponse>(response.clone())
  if (!isBackendAuthResponse(auth)) {
    return null
  }

  return { auth, response }
}

export async function fetchOwnProfile(
  accessToken: string
): Promise<OwnProfileFetchResult> {
  const response = await fetch(getBackendUrl([...BACKEND_PROFILE_PATHS.me]), {
    method: "GET",
    headers: {
      authorization: `Bearer ${accessToken}`,
    },
    cache: "no-store",
  })

  if (response.status === 401 || response.status === 403) {
    return { status: "unauthorized" }
  }

  if (response.status === 404) {
    return { status: "ok", profile: null }
  }

  if (!response.ok) {
    return { status: "ok", profile: null }
  }

  return {
    status: "ok",
    profile: await readJson<OwnProfileResponse>(response),
  }
}

export function isBackendAuthResponse(
  response: BackendAuthResponse | null
): response is BackendAuthResponse {
  return (
    typeof response?.accessToken === "string" &&
    typeof response.accessTokenExpiresAt === "string" &&
    typeof response.refreshTokenExpiresAt === "string"
  )
}

export function createForwardedResponse(
  backendResponse: Response
): NextResponse {
  const headers = new Headers(backendResponse.headers)
  headers.delete("connection")
  headers.delete("content-encoding")
  headers.delete("content-length")
  headers.delete("set-cookie")
  headers.delete("transfer-encoding")

  return new NextResponse(backendResponse.body, {
    status: backendResponse.status,
    statusText: backendResponse.statusText,
    headers,
  })
}

function getSetCookieHeaders(headers: Headers): string[] {
  const getSetCookie = (headers as HeadersWithSetCookie).getSetCookie
  if (typeof getSetCookie === "function") {
    return getSetCookie.call(headers)
  }

  const cookie = headers.get("set-cookie")
  return cookie ? [cookie] : []
}

function normalizeRoles(roles: string[]): UserRole[] {
  const normalized = roles.filter(
    (role): role is UserRole => role === "Admin" || role === "User"
  )

  return normalized.length > 0 ? normalized : ["User"]
}
