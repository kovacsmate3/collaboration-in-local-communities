export const DEFAULT_BACKEND_API_URL = "http://localhost:5073"

export const AUTH_COOKIES = {
  accessToken: "cilc.access-token",
  refreshToken: "refreshToken",
} as const

export const AUTH_API_PATHS = {
  session: "/api/auth/session",
  login: "/api/auth/login",
  register: "/api/auth/register",
  logout: "/api/auth/logout",
} as const

export const BACKEND_AUTH_PATHS = {
  refresh: ["auth", "refresh"],
  tokenIssuing: ["auth/login", "auth/register", "auth/refresh"],
} as const

export const BACKEND_PROFILE_PATHS = {
  me: ["profiles", "me"],
} as const

export const APP_AUTH_ROUTES = {
  login: "/login",
  forgotPassword: "/login/forgot",
  register: "/register",
} as const

export const APP_HOME_ROUTES = {
  admin: "/admin",
  user: "/feed",
} as const

export const APP_LEGAL_ROUTES = {
  terms: "/terms",
} as const

export const PROTECTED_ROUTE_PREFIXES = [
  "/admin",
  "/feed",
  "/helper",
  "/messages",
  "/post-task",
  "/profile",
  "/tasks",
] as const

export const JWT_ROLE_CLAIM =
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"

export const JWT_REFRESH_SKEW_SECONDS = 30
