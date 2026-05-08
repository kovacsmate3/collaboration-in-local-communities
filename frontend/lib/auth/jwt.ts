import { JWT_REFRESH_SKEW_SECONDS, JWT_ROLE_CLAIM } from "@/lib/auth/constants"

interface JwtPayload {
  exp?: number
  sub?: string
  email?: string
  nameid?: string
  role?: string | string[]
  roles?: string | string[]
  [JWT_ROLE_CLAIM]?: string | string[]
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"?: string
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"?: string
}

export interface JwtUserClaims {
  userId: string
  email: string
  roles: string[]
}

function decodeBase64Url(value: string): string {
  const base64 = value.replaceAll("-", "+").replaceAll("_", "/")
  const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, "=")
  return atob(padded)
}

export function decodeJwtPayload(token: string): JwtPayload | null {
  const [, payload] = token.split(".")
  if (!payload) {
    return null
  }

  try {
    return JSON.parse(decodeBase64Url(payload)) as JwtPayload
  } catch {
    return null
  }
}

export function getJwtRoles(token: string): string[] {
  const payload = decodeJwtPayload(token)
  if (!payload) {
    return []
  }

  return getRolesFromPayload(payload)
}

export function getJwtUserClaims(token: string): JwtUserClaims | null {
  const payload = decodeJwtPayload(token)
  if (!payload) {
    return null
  }

  const userId = firstStringClaim(
    payload.sub,
    payload.nameid,
    payload[
      "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
    ]
  )
  const email = firstStringClaim(
    payload.email,
    payload[
      "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
    ]
  )

  return userId && email
    ? {
        userId,
        email,
        roles: getRolesFromPayload(payload),
      }
    : null
}

export function isJwtFresh(
  token: string,
  skewSeconds = JWT_REFRESH_SKEW_SECONDS
): boolean {
  const payload = decodeJwtPayload(token)
  if (!payload?.exp) {
    return false
  }

  const nowSeconds = Math.floor(Date.now() / 1000)
  return payload.exp > nowSeconds + skewSeconds
}

function getRolesFromPayload(payload: JwtPayload): string[] {
  const rawRoles = payload[JWT_ROLE_CLAIM] ?? payload.roles ?? payload.role

  if (Array.isArray(rawRoles)) {
    return rawRoles.filter((role): role is string => typeof role === "string")
  }

  return typeof rawRoles === "string" ? [rawRoles] : []
}

function firstStringClaim(...claims: Array<string | undefined>): string | null {
  return claims.find((claim) => claim && claim.length > 0) ?? null
}
