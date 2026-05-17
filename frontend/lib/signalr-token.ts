import { refreshAccessToken } from "@/lib/auth/token-bridge"

export async function fetchSignalRToken(): Promise<string> {
  let res = await fetch("/api/auth/token")
  if (res.status === 401) {
    const refreshed = await refreshAccessToken()
    if (refreshed) {
      res = await fetch("/api/auth/token")
    }
  }
  if (!res.ok) return ""
  const data = (await res.json()) as { token: string }
  return data.token
}
