/**
 * Base API client for all backend requests.
 * All paths are relative — Next.js rewrites them to the backend via /api/* → BACKEND_URL/api/*.
 */

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
  const res = await fetch(`/api${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...(options?.headers ?? {}),
    },
    ...options,
  })

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

  delete: <T = void>(path: string, init?: RequestInit) =>
    request<T>(path, { method: "DELETE", ...init }),
}
