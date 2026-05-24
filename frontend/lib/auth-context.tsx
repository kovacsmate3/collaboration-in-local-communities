"use client"

import * as React from "react"
import type { ReactNode } from "react"

import type {
  AuthUser,
  LoginInput,
  RefreshResponse,
  RegisterInput,
  SessionResponse,
} from "@/lib/auth/types"
import { AUTH_API_PATHS } from "@/lib/auth/constants"
import { authMutation } from "@/lib/auth/functions"
import { registerRefreshHandler } from "@/lib/auth/token-bridge"

export type {
  AuthUser,
  LoginInput,
  RegisterInput,
  UserRole,
} from "./auth/types"

interface AuthContextValue {
  /** null while loading, undefined when logged out */
  user: AuthUser | null | undefined
  isLoading: boolean
  isAdmin: boolean
  refreshSession: (signal?: AbortSignal) => Promise<AuthUser | undefined>
  login: (input: LoginInput) => Promise<AuthUser>
  register: (input: RegisterInput) => Promise<string>
  logout: () => Promise<void>
}

const AuthContext = React.createContext<AuthContextValue | null>(null)

// How early before expiry to fire the proactive refresh.
const REFRESH_BEFORE_EXPIRY_MS = 15_000
// Floor so we never schedule a setTimeout for 0/negative if something is odd.
const MIN_REFRESH_DELAY_MS = 1_000

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = React.useState<AuthUser | null | undefined>(null)
  const [expiresAt, setExpiresAt] = React.useState<string | null>(null)

  // Single in-flight refresh promise — every caller (proactive timer, 401
  // retry, visibilitychange, login) shares the same Promise, so the backend
  // sees exactly one refresh request per logical refresh moment. This is the
  // single point that prevents the atomic-rotation race.
  const inFlightRefresh = React.useRef<Promise<boolean> | null>(null)

  // Refs so imperative handlers (visibilitychange, bridge) can read the latest
  // values without re-registering listeners on every state change.
  const userRef = React.useRef(user)
  const expiresAtRef = React.useRef(expiresAt)
  React.useEffect(() => {
    userRef.current = user
  }, [user])
  React.useEffect(() => {
    expiresAtRef.current = expiresAt
  }, [expiresAt])

  // GET /api/auth/session — never refreshes. Returns user info if the access
  // token cookie is still valid, otherwise null.
  const fetchSession = React.useCallback(async (signal?: AbortSignal) => {
    const response = await fetch(AUTH_API_PATHS.session, {
      method: "GET",
      cache: "no-store",
      signal,
    })

    if (!response.ok) {
      return { user: null as AuthUser | null, expiresAt: null }
    }

    const session = (await response.json()) as SessionResponse
    return {
      user: session.user,
      expiresAt: session.expiresAt,
    }
  }, [])

  // POST /api/auth/refresh — rotates tokens. Singleton; concurrent callers
  // share the same in-flight Promise.
  const performRefresh = React.useCallback(async (): Promise<boolean> => {
    if (inFlightRefresh.current) {
      return inFlightRefresh.current
    }

    inFlightRefresh.current = (async () => {
      try {
        const response = await fetch(AUTH_API_PATHS.refresh, {
          method: "POST",
          cache: "no-store",
        })

        if (response.status === 401) {
          // Refresh token is invalid/revoked/expired — user is logged out.
          setUser(undefined)
          setExpiresAt(null)
          return false
        }

        if (!response.ok) {
          // Transient error (network/5xx) — don't log the user out, just fail
          // this attempt. The next proactive tick will try again.
          return false
        }

        const data = (await response.json()) as RefreshResponse
        setExpiresAt(data.expiresAt ?? null)
        return true
      } catch {
        // Network error — same as transient: don't log out.
        return false
      } finally {
        inFlightRefresh.current = null
      }
    })()

    return inFlightRefresh.current
  }, [])

  // Public refreshSession: ensures we have a valid session. Tries the session
  // endpoint first; if that 401s, attempts a refresh and re-fetches.
  const refreshSession = React.useCallback(
    async (signal?: AbortSignal): Promise<AuthUser | undefined> => {
      let result = await fetchSession(signal)

      if (!result.user) {
        const refreshed = await performRefresh()
        if (signal?.aborted) return undefined
        if (refreshed) {
          result = await fetchSession(signal)
        }
      }

      if (signal?.aborted) return undefined

      const nextUser = result.user ?? undefined
      setUser(nextUser)
      setExpiresAt(result.expiresAt)
      return nextUser
    },
    [fetchSession, performRefresh]
  )

  // Expose performRefresh to non-React modules (apiClient, chat-hub) so they
  // can request a refresh on 401 and retry. Routes through the same singleton.
  React.useEffect(() => {
    registerRefreshHandler(performRefresh)
  }, [performRefresh])

  // Initial session load on mount.
  React.useEffect(() => {
    const controller = new AbortController()

    void refreshSession(controller.signal).catch((error: unknown) => {
      if (!controller.signal.aborted) {
        console.warn("Unable to load auth session", error)
        setUser(undefined)
        setExpiresAt(null)
      }
    })

    return () => controller.abort()
  }, [refreshSession])

  // Proactive refresh: schedule a refresh shortly before the access token
  // expires. Each successful refresh updates expiresAt, which restarts this
  // effect and schedules the next tick.
  React.useEffect(() => {
    if (!expiresAt || !user) return

    const msUntilExpiry = new Date(expiresAt).getTime() - Date.now()
    const delay = Math.max(
      msUntilExpiry - REFRESH_BEFORE_EXPIRY_MS,
      MIN_REFRESH_DELAY_MS
    )

    const timer = setTimeout(() => void performRefresh(), delay)
    return () => clearTimeout(timer)
  }, [expiresAt, user, performRefresh])

  // When the tab regains focus, refresh if the token is close to expiry —
  // handles browsers throttling setTimeout in backgrounded tabs.
  React.useEffect(() => {
    function handleVisibility() {
      if (document.visibilityState !== "visible") return
      if (!userRef.current) return

      const expiry = expiresAtRef.current
      const msLeft = expiry ? new Date(expiry).getTime() - Date.now() : 0
      if (msLeft < REFRESH_BEFORE_EXPIRY_MS) {
        void performRefresh()
      }
    }

    document.addEventListener("visibilitychange", handleVisibility)
    return () =>
      document.removeEventListener("visibilitychange", handleVisibility)
  }, [performRefresh])

  const login = React.useCallback(
    async (input: LoginInput) => {
      await authMutation(AUTH_API_PATHS.login, input)
      const nextUser = await refreshSession()

      if (!nextUser) {
        throw new Error("Signed in, but the session could not be loaded.")
      }

      return nextUser
    },
    [refreshSession]
  )

  const register = React.useCallback(async (input: RegisterInput) => {
    const body = await authMutation(AUTH_API_PATHS.register, input)
    if (
      typeof body === "object" &&
      body !== null &&
      "message" in body &&
      typeof (body as Record<string, unknown>).message === "string"
    ) {
      return (body as Record<string, unknown>).message as string
    }
    return "Registration successful. Please check your email to verify your account."
  }, [])

  const logout = React.useCallback(async () => {
    try {
      await fetch(AUTH_API_PATHS.logout, {
        method: "POST",
        cache: "no-store",
      })
    } finally {
      setUser(undefined)
      setExpiresAt(null)
    }
  }, [])

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading: user === null,
        isAdmin: user?.roles.includes("Admin") ?? false,
        refreshSession,
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth(): AuthContextValue {
  const ctx = React.useContext(AuthContext)
  if (!ctx) {
    throw new Error("useAuth must be used within <AuthProvider>")
  }
  return ctx
}
