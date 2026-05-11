import type { NextRequest } from "next/server"

export const PROXY_AUTH_HEADER = "X-Frontend-Auth"

const FORWARDED_HEADER_BLOCKLIST = [
  "x-forwarded-for",
  "x-real-ip",
  "x-vercel-forwarded-for",
  "x-vercel-ip",
  PROXY_AUTH_HEADER.toLowerCase(),
] as const

const HEADER_B64 = base64UrlEncode(
  new TextEncoder().encode(JSON.stringify({ alg: "HS256", typ: "JWT" }))
)

const TOKEN_LIFETIME_SECONDS = 30

type SignProxyTokenInput = {
  ip: string
  method: string
  path: string
}

let cachedKey: { material: string; key: CryptoKey } | null = null

export async function signProxyToken(
  input: SignProxyTokenInput
): Promise<string | null> {
  const keyMaterial = process.env.BACKEND_PROXY_SIGNING_KEY
  if (!keyMaterial) {
    return null
  }

  const cryptoKey = await getCryptoKey(keyMaterial)
  const now = Math.floor(Date.now() / 1000)
  const payload = {
    iat: now,
    exp: now + TOKEN_LIFETIME_SECONDS,
    ip: input.ip,
    mth: input.method.toUpperCase(),
    pth: input.path,
  }

  const payloadB64 = base64UrlEncode(
    new TextEncoder().encode(JSON.stringify(payload))
  )
  const signingInput = `${HEADER_B64}.${payloadB64}`
  const signature = await crypto.subtle.sign(
    "HMAC",
    cryptoKey,
    new TextEncoder().encode(signingInput)
  )

  return `${signingInput}.${base64UrlEncode(new Uint8Array(signature))}`
}

async function getCryptoKey(material: string): Promise<CryptoKey> {
  if (cachedKey && cachedKey.material === material) {
    return cachedKey.key
  }

  const key = await crypto.subtle.importKey(
    "raw",
    new TextEncoder().encode(material),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"]
  )

  cachedKey = { material, key }
  return key
}

function base64UrlEncode(bytes: Uint8Array): string {
  let binary = ""
  for (let i = 0; i < bytes.byteLength; i += 1) {
    binary += String.fromCharCode(bytes[i])
  }
  return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "")
}

export function getClientIpFromRequest(request: NextRequest): string {
  const candidates = [
    request.headers.get("x-vercel-forwarded-for"),
    request.headers.get("x-forwarded-for"),
    request.headers.get("x-real-ip"),
  ]

  for (const candidate of candidates) {
    if (!candidate) {
      continue
    }
    const first = candidate.split(",")[0]?.trim()
    if (first) {
      return first
    }
  }

  return ""
}

export async function applyProxyAuth(
  request: NextRequest,
  outbound: Headers,
  backendUrl: URL,
  method: string
): Promise<void> {
  for (const header of FORWARDED_HEADER_BLOCKLIST) {
    outbound.delete(header)
  }

  const token = await signProxyToken({
    ip: getClientIpFromRequest(request),
    method,
    path: backendUrl.pathname,
  })

  if (token) {
    outbound.set(PROXY_AUTH_HEADER, token)
  }
}
