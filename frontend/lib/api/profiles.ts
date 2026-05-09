import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"

import { apiClient } from "@/lib/api/client"

// ── Types ─────────────────────────────────────────────────────────────────────

export interface ApiPrivacySettings {
  showWorkplace: boolean
  showPosition: boolean
  showLocation: boolean
  showAvailability: boolean
}

export interface ApiPublicProfile {
  id: string
  displayName: string
  bio: string | null
  workplace: string | null
  position: string | null
  availability: string | null
  photoUrl: string | null
  locationText: string | null
  averageRating: number
  reviewCount: number
  completedTaskCount: number
}

export interface ApiOwnProfile extends ApiPublicProfile {
  userId: string
  isProfileCompleted: boolean
  createdAt: string
  updatedAt: string
  skillIds: string[]
  privacySettings: ApiPrivacySettings
}

export interface UpdateProfileInput {
  displayName: string
  bio?: string | null
  workplace?: string | null
  position?: string | null
  availability?: string | null
  photoUrl?: string | null
  locationText?: string | null
}

// ── Query keys ────────────────────────────────────────────────────────────────

export const profileKeys = {
  own: ["profiles", "me"] as const,
  public: (id: string) => ["profiles", id] as const,
  privacy: ["profiles", "me", "privacy"] as const,
}

// ── Hooks ─────────────────────────────────────────────────────────────────────

export function useOwnProfile() {
  return useQuery({
    queryKey: profileKeys.own,
    queryFn: () => apiClient.get<ApiOwnProfile>("/profiles/me"),
  })
}

export function usePublicProfile(id: string) {
  return useQuery({
    queryKey: profileKeys.public(id),
    queryFn: () => apiClient.get<ApiPublicProfile>(`/profiles/${id}`),
    enabled: !!id,
  })
}

export function useUpdateProfile() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: UpdateProfileInput) =>
      apiClient.put<ApiOwnProfile>("/profiles/me", data),
    onSuccess: (updated) => {
      qc.setQueryData(profileKeys.own, updated)
    },
  })
}

export function useUpdatePrivacySettings() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: ApiPrivacySettings) =>
      apiClient.put<ApiPrivacySettings>("/profiles/me/privacy", data),
    onSuccess: (updated) => {
      qc.setQueryData<ApiOwnProfile | undefined>(profileKeys.own, (prev) =>
        prev ? { ...prev, privacySettings: updated } : prev
      )
    },
  })
}
