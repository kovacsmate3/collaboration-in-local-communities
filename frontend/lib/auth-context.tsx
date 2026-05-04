"use client"

/**
 * Placeholder authentication context.
 *
 * TODO (separate issue): replace with real JWT/session integration once the
 * auth endpoints (#15, #16) are implemented. The shape of AuthUser and the
 * useAuth hook must stay stable so the rest of the app can be wired up without
 * touching every consumer.
 *
 * During development, set NEXT_PUBLIC_DEV_ROLE to "Admin" or "User" in
 * .env.local to simulate different roles:
 *
 *   NEXT_PUBLIC_DEV_ROLE=Admin
 */

import * as React from "react"

export type UserRole = "Admin" | "User"

export interface AuthUser {
  id: string
  name: string
  email: string
  role: UserRole
  avatarUrl?: string
}

interface AuthContextValue {
  /** null while loading, undefined when logged out */
  user: AuthUser | null | undefined
  isLoading: boolean
  isAdmin: boolean
  /** TODO: wire to real login endpoint */
  login: (email: string, password: string) => Promise<void>
  /** TODO: wire to real logout endpoint */
  logout: () => Promise<void>
}

const AuthContext = React.createContext<AuthContextValue | null>(null)

// ---------------------------------------------------------------------------
// Dev stub — swapped out for a real provider once auth ships
// ---------------------------------------------------------------------------
const DEV_ROLE = (process.env.NEXT_PUBLIC_DEV_ROLE ?? "Admin") as UserRole

const STUB_USERS: Record<UserRole, AuthUser> = {
  Admin: {
    id: "dev-admin-1",
    name: "Dev Admin",
    email: "admin@dev.local",
    role: "Admin",
    avatarUrl: undefined,
  },
  User: {
    id: "dev-user-1",
    name: "Dev User",
    email: "user@dev.local",
    role: "User",
    avatarUrl: undefined,
  },
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = React.useState<AuthUser | null | undefined>(
    // Start as null (loading) then settle to the stub after a tick so RSC
    // hydration is clean.
    null
  )

  React.useEffect(() => {
    // Simulate an async session check
    const timer = setTimeout(() => {
      setUser(STUB_USERS[DEV_ROLE])
    }, 0)
    return () => clearTimeout(timer)
  }, [])

  async function login(_email: string, _password: string) {
    // TODO: POST /api/auth/login, store JWT, set user from response
    setUser(STUB_USERS[DEV_ROLE])
  }

  async function logout() {
    // TODO: POST /api/auth/logout, clear JWT
    setUser(undefined)
  }

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading: user === null,
        isAdmin: user?.role === "Admin",
        login,
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
