import type { NextRequest } from "next/server"
import { NextResponse } from "next/server"

import {
  ACCESS_TOKEN_COOKIE,
  getExpiredCookieOptions,
} from "@/lib/auth/cookies"
import { BACKEND_AUTH_PATHS } from "@/lib/auth/constants"
import {
  appendBackendSetCookie,
  clearAuthCookies,
  createForwardedResponse,
  getBackendUrl,
  getRefreshCookieHeader,
  isBackendAuthResponse,
  readJson,
  refreshBackendToken,
  setAccessTokenCookie,
  toSafeAuthResponse,
} from "@/lib/auth/backend"
import type { BackendAuthResponse } from "@/lib/auth/types"

type ApiRouteContext = {
  params: Promise<{
    path: string[]
  }>
}

const TOKEN_ISSUING_PATHS: ReadonlySet<string> = new Set(
  BACKEND_AUTH_PATHS.tokenIssuing
)

async function proxyRequest(
  request: NextRequest,
  context: ApiRouteContext
): Promise<Response> {
  const { path } = await context.params
  const pathKey = path.join("/")
  const requestBody = await getRequestBody(request)
  const accessToken = request.cookies.get(ACCESS_TOKEN_COOKIE)?.value

  const backendResponse = await fetchBackend(
    request,
    path,
    requestBody,
    accessToken
  )

  if (TOKEN_ISSUING_PATHS.has(pathKey)) {
    return handleTokenIssuingResponse(request, backendResponse)
  }

  if (pathKey === "auth/logout") {
    return handleLogoutResponse(request, backendResponse)
  }

  if (backendResponse.status !== 401) {
    const response = createForwardedResponse(backendResponse)
    appendBackendSetCookie(backendResponse, response)
    return response
  }

  const refreshResult = await refreshBackendToken(request)
  if (!refreshResult) {
    const response = createForwardedResponse(backendResponse)
    response.cookies.set(
      ACCESS_TOKEN_COOKIE,
      "",
      getExpiredCookieOptions(request.url)
    )
    return response
  }

  const retryResponse = await fetchBackend(
    request,
    path,
    requestBody,
    refreshResult.auth.accessToken
  )
  const response = createForwardedResponse(retryResponse)
  appendBackendSetCookie(refreshResult.response, response)
  appendBackendSetCookie(retryResponse, response)
  setAccessTokenCookie(response, request.url, refreshResult.auth)

  return response
}

async function handleTokenIssuingResponse(
  request: NextRequest,
  backendResponse: Response
): Promise<Response> {
  const body = await readJson<BackendAuthResponse>(backendResponse.clone())

  if (!backendResponse.ok || !isBackendAuthResponse(body)) {
    const response = body
      ? NextResponse.json(body, { status: backendResponse.status })
      : new NextResponse(null, { status: backendResponse.status })

    if (backendResponse.status === 401) {
      response.cookies.set(
        ACCESS_TOKEN_COOKIE,
        "",
        getExpiredCookieOptions(request.url)
      )
    }

    appendBackendSetCookie(backendResponse, response)
    return response
  }

  const response = NextResponse.json(toSafeAuthResponse(body), {
    status: backendResponse.status,
  })
  appendBackendSetCookie(backendResponse, response)
  setAccessTokenCookie(response, request.url, body)

  return response
}

function handleLogoutResponse(
  request: NextRequest,
  backendResponse: Response
): Response {
  const response = createForwardedResponse(backendResponse)
  appendBackendSetCookie(backendResponse, response)
  clearAuthCookies(response, request.url)
  return response
}

async function fetchBackend(
  request: NextRequest,
  path: string[],
  requestBody: ArrayBuffer | undefined,
  accessToken: string | undefined
): Promise<Response> {
  const headers = new Headers(request.headers)
  const refreshCookie = getRefreshCookieHeader(request)

  headers.delete("accept-encoding")
  headers.delete("connection")
  headers.delete("content-length")
  headers.delete("cookie")
  headers.delete("host")

  if (refreshCookie) {
    headers.set("cookie", refreshCookie)
  }

  if (accessToken) {
    headers.set("authorization", `Bearer ${accessToken}`)
  }

  return fetch(getBackendUrl(path, request.url), {
    method: request.method,
    headers,
    body: requestBody ? requestBody.slice(0) : undefined,
    cache: "no-store",
  })
}

async function getRequestBody(
  request: NextRequest
): Promise<ArrayBuffer | undefined> {
  if (request.method === "GET" || request.method === "HEAD") {
    return undefined
  }

  return request.arrayBuffer()
}

export const GET = proxyRequest
export const POST = proxyRequest
export const PUT = proxyRequest
export const PATCH = proxyRequest
export const DELETE = proxyRequest
export const HEAD = proxyRequest
export const OPTIONS = proxyRequest
