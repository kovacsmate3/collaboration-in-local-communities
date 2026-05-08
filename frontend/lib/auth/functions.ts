import {
  APP_AUTH_ROUTES,
  APP_HOME_ROUTES,
  PROTECTED_ROUTE_PREFIXES,
} from "@/lib/auth/constants"
import { getJwtRoles } from "@/lib/auth/jwt"
import type { LoginInput, RegisterInput } from "@/lib/auth/types"

export type RegistrationStep = "account" | "profile"

export function getPostAuthRedirectPath(
  nextPath: string | null,
  role: string
): string {
  if (isSafeRelativePath(nextPath)) {
    return nextPath
  }

  return getHomePathForRole(role)
}

export function getHomePathForRole(role: string): string {
  return role === "Admin" ? APP_HOME_ROUTES.admin : APP_HOME_ROUTES.user
}

export function getHomePathForToken(accessToken: string): string {
  return getJwtRoles(accessToken).includes("Admin")
    ? APP_HOME_ROUTES.admin
    : APP_HOME_ROUTES.user
}

export function getAuthErrorMessage(
  error: unknown,
  fallbackMessage: string
): string {
  return error instanceof Error ? error.message : fallbackMessage
}

export function getRegisterSubmitLabel(
  step: RegistrationStep,
  isSubmitting: boolean
): string {
  if (isSubmitting) {
    return "Creating account..."
  }

  return step === "account" ? "Continue" : "Create account"
}

export function toOptionalString(value: string): string | undefined {
  const trimmed = value.trim()
  return trimmed.length > 0 ? trimmed : undefined
}

export function isAuthPath(pathname: string): boolean {
  return Object.values(APP_AUTH_ROUTES).some((route) => pathname === route)
}

export function isProtectedPath(pathname: string): boolean {
  return PROTECTED_ROUTE_PREFIXES.some((route) =>
    isPathWithinRoute(pathname, route)
  )
}

export function isAdminPath(pathname: string): boolean {
  return isPathWithinRoute(pathname, APP_HOME_ROUTES.admin)
}

export function getLoginRedirectUrl(
  requestUrl: string,
  pathname: string,
  search: string
): URL {
  const loginUrl = new URL(APP_AUTH_ROUTES.login, requestUrl)
  loginUrl.searchParams.set("next", `${pathname}${search}`)
  return loginUrl
}

export async function authMutation(
  path: string,
  input: LoginInput | RegisterInput
) {
  const response = await fetch(path, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(input),
    cache: "no-store",
  })

  if (!response.ok) {
    throw new Error(await readAuthError(response))
  }
}

function isSafeRelativePath(path: string | null): path is string {
  return path?.startsWith("/") === true && !path.startsWith("//")
}

function isPathWithinRoute(pathname: string, route: string): boolean {
  return pathname === route || pathname.startsWith(`${route}/`)
}

async function readAuthError(response: Response): Promise<string> {
  let body: unknown

  try {
    body = await response.json()
  } catch {
    body = undefined
  }

  if (isProblemDetails(body)) {
    const validationError = firstValidationError(body.errors)
    return validationError ?? body.title ?? response.statusText
  }

  return response.status === 401
    ? "The email or password is incorrect."
    : response.statusText
}

function isProblemDetails(
  value: unknown
): value is { title?: string; errors?: Record<string, string[]> } {
  return typeof value === "object" && value !== null
}

function firstValidationError(
  errors: Record<string, string[]> | undefined
): string | undefined {
  if (!errors) {
    return undefined
  }

  return Object.values(errors).find((messages) => messages.length > 0)?.[0]
}
