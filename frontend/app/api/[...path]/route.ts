import type { NextRequest } from "next/server"
import { NextResponse } from "next/server"

import { ACCESS_TOKEN_COOKIE } from "@/lib/auth/cookies"
import { BACKEND_AUTH_PATHS } from "@/lib/auth/constants"
import {
  appendBackendSetCookie,
  clearAuthCookies,
  createForwardedResponse,
  getBackendUrl,
  getRefreshCookieHeader,
  isBackendAuthResponse,
  readJson,
  setAccessTokenCookie,
  toSafeAuthResponse,
} from "@/lib/auth/backend"
import { applyProxyAuth } from "@/lib/auth/frontend-proxy-jws"
import type { BackendAuthResponse } from "@/lib/auth/types"

type ApiRouteContext = {
  params: Promise<{
    path: string[]
  }>
}

const TOKEN_ISSUING_PATHS: ReadonlySet<string> = new Set(
  BACKEND_AUTH_PATHS.tokenIssuing
)

// Forwards requests to the backend with the current access token. No refresh
// logic here — if the backend returns 401, the client's apiClient handles it
// by triggering a refresh through the AuthProvider's singleton, then retries.
async function proxyRequest(
  request: NextRequest,
  context: ApiRouteContext
): Promise<Response> {
  const { path } = await context.params
  const pathKey = path.join("/")
  const requestBody = await getRequestBody(request)

  // login/refresh issue tokens, so we must NOT send one in.
  // logout requires [Authorize] on the backend, so we do send it.
  const accessToken = TOKEN_ISSUING_PATHS.has(pathKey)
    ? undefined
    : request.cookies.get(ACCESS_TOKEN_COOKIE)?.value

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

  const response = createForwardedResponse(backendResponse)
  appendBackendSetCookie(backendResponse, response)
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
      clearAuthCookies(response, request.url)
    }

    appendBackendSetCookie(backendResponse, response)
    return response
  }

  const response = NextResponse.json(toSafeAuthResponse(body), {
    status: backendResponse.status,
  })
  // ORDER MATTERS: response.cookies.set() wipes all Set-Cookie headers and
  // rewrites them from its internal Map (see @edge-runtime/cookies `replace`).
  // So we MUST call setAccessTokenCookie (which uses response.cookies.set)
  // BEFORE appendBackendSetCookie (which uses headers.append). Otherwise the
  // backend's refresh-token Set-Cookie is silently deleted before reaching
  // the browser.
  setAccessTokenCookie(response, request.url, body)
  appendBackendSetCookie(backendResponse, response)

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

  const backendUrl = getBackendUrl(path, request.url)
  await applyProxyAuth(request, headers, backendUrl, request.method)

  return fetch(backendUrl, {
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
