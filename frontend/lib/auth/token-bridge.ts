// Bridge between non-React modules (apiClient, chat-hub) and the AuthProvider.
//
// AuthProvider registers its `performRefresh` handler on mount via
// `registerRefreshHandler`. Any module that needs to refresh the access token —
// typically after receiving a 401 — calls `refreshAccessToken()`, which routes
// through the AuthProvider's singleton in-flight promise so concurrent callers
// share a single refresh attempt.
//
// Before AuthProvider mounts, the default handler returns false (refresh failed),
// which is the safe behavior — callers will surface the 401 as a normal error.

let handler: () => Promise<boolean> = async () => false

export function registerRefreshHandler(fn: () => Promise<boolean>): void {
  handler = fn
}

export function refreshAccessToken(): Promise<boolean> {
  return handler()
}
