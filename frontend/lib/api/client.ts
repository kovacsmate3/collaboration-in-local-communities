/**
 * Base API client for all backend requests.
 * All paths are relative - the App Router API proxy forwards /api/* to the backend.
 *
 * On 401, this client asks the AuthProvider (via the token-bridge) to refresh
 * the access token, then retries the original request once. The bridge routes
 * through AuthProvider's singleton in-flight promise, so concurrent 401s share
 * a single refresh.
 */

import { refreshAccessToken } from "@/lib/auth/token-bridge"

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    public readonly body?: unknown
  ) {
    super(message)
    this.name = "ApiError"
  }
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const init: RequestInit = {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(options?.headers ?? {}),
    },
  }

  let res = await fetch(`/api${path}`, init)

  if (res.status === 401) {
    const refreshed = await refreshAccessToken()
    if (refreshed) {
      res = await fetch(`/api${path}`, init)
    }
  }

  if (res.status === 204) return undefined as T

  let body: unknown
  try {
    body = await res.json()
  } catch {
    body = undefined
  }

  if (!res.ok) {
    const message =
      (body as Record<string, string> | undefined)?.message ??
      (body as Record<string, string> | undefined)?.title ??
      res.statusText
    throw new ApiError(res.status, message, body)
  }

  return body as T
}

export const apiClient = {
  get: <T>(path: string, init?: RequestInit) =>
    request<T>(path, { method: "GET", ...init }),

  post: <T>(path: string, data: unknown, init?: RequestInit) =>
    request<T>(path, {
      method: "POST",
      body: JSON.stringify(data),
      ...init,
    }),

  put: <T>(path: string, data: unknown, init?: RequestInit) =>
    request<T>(path, {
      method: "PUT",
      body: JSON.stringify(data),
      ...init,
    }),

  patch: <T>(path: string, data: unknown, init?: RequestInit) =>
    request<T>(path, {
      method: "PATCH",
      body: JSON.stringify(data),
      ...init,
    }),

  delete: <T = void>(path: string, init?: RequestInit) =>
    request<T>(path, { method: "DELETE", ...init }),
}
