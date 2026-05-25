export type UserRole = "Admin" | "User"

export interface AuthUser {
  id: string
  name: string
  email: string
  emailVerified: boolean
  role: UserRole
  roles: UserRole[]
  avatarUrl?: string
  profileId?: string
  isProfileCompleted: boolean
  terms: AuthTermsState
}

export interface AuthTermsState {
  hasAccepted: boolean
  activeVersionId: string | null
  activeVersion: string | null
  activeTitle: string | null
  acceptedAt: string | null
}

export interface LoginInput {
  email: string
  password: string
}

export interface RegisterInput extends LoginInput {
  displayName: string
  workplace?: string
  position?: string
  locationText?: string
  latitude?: number
  longitude?: number
  bio?: string
  acceptTerms: boolean
}

export interface BackendAuthResponse {
  userId: string
  email: string
  tokenType: string
  accessToken: string
  accessTokenExpiresAt: string
  refreshTokenExpiresAt: string
}

export interface SafeAuthResponse {
  userId: string
  email: string
  tokenType: string
  accessTokenExpiresAt: string
  refreshTokenExpiresAt: string
}

export interface OwnProfileResponse {
  id: string
  userId: string
  displayName: string
  photoUrl?: string | null
  isProfileCompleted: boolean
}

export interface SessionResponse {
  user: AuthUser | null
  expiresAt: string | null
}

export interface RefreshResponse {
  expiresAt: string | null
}
