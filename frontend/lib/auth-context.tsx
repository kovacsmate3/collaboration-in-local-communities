"use client"

import * as React from "react"

import type {
  AuthUser,
  LoginInput,
  RegisterInput,
  SessionResponse,
} from "@/lib/auth/types"
import { AUTH_API_PATHS } from "@/lib/auth/constants"
import { authMutation } from "@/lib/auth/functions"

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
  register: (input: RegisterInput) => Promise<AuthUser>
  logout: () => Promise<void>
}

const AuthContext = React.createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = React.useState<AuthUser | null | undefined>(null)

  const refreshSession = React.useCallback(async (signal?: AbortSignal) => {
    const response = await fetch(AUTH_API_PATHS.session, {
      method: "GET",
      cache: "no-store",
      signal,
    })

    if (!response.ok) {
      setUser(undefined)
      return undefined
    }

    const session = (await response.json()) as SessionResponse
    const nextUser = session.user ?? undefined
    setUser(nextUser)
    return nextUser
  }, [])

  React.useEffect(() => {
    const controller = new AbortController()

    void Promise.resolve()
      .then(() => refreshSession(controller.signal))
      .catch((error: unknown) => {
        if (!controller.signal.aborted) {
          console.warn("Unable to refresh auth session", error)
          setUser(undefined)
        }
      })

    return () => {
      controller.abort()
    }
  }, [refreshSession])

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

  const register = React.useCallback(
    async (input: RegisterInput) => {
      await authMutation(AUTH_API_PATHS.register, input)
      const nextUser = await refreshSession()

      if (!nextUser) {
        throw new Error("Account created, but the session could not be loaded.")
      }

      return nextUser
    },
    [refreshSession]
  )

  const logout = React.useCallback(async () => {
    try {
      await fetch(AUTH_API_PATHS.logout, {
        method: "POST",
        cache: "no-store",
      })
    } finally {
      setUser(undefined)
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
