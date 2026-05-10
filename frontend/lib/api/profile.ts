import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"

import { apiClient } from "@/lib/api/client"
import type { Reputation, User } from "@/lib/types"

export interface ProfilePrivacySettingsResponse {
  showWorkplace: boolean
  showPosition: boolean
  showLocation: boolean
  showAvailability: boolean
}

export interface OwnProfileResponse {
  id: string
  userId: string
  displayName: string
  bio?: string | null
  workplace?: string | null
  position?: string | null
  availability?: string | null
  photoUrl?: string | null
  locationText?: string | null
  latitude?: number | null
  longitude?: number | null
  isProfileCompleted: boolean
  averageRating: number
  reviewCount: number
  completedTaskCount: number
  createdAt: string
  updatedAt: string
  skillIds: string[]
  privacySettings: ProfilePrivacySettingsResponse
}

export interface PublicProfileResponse {
  id: string
  displayName: string
  bio?: string | null
  workplace?: string | null
  position?: string | null
  availability?: string | null
  photoUrl?: string | null
  locationText?: string | null
  averageRating: number
  reviewCount: number
  completedTaskCount: number
}

export interface UpdateOwnProfileRequest {
  displayName: string
  bio?: string
  workplace?: string
  position?: string
  availability?: string
  photoUrl?: string
  locationText?: string
  latitude?: number
  longitude?: number
  skillIds?: string[]
}

export interface SkillResponse {
  id: string
  code: string
  name: string
  description?: string | null
  status: string
}

export const profileKeys = {
  all: ["profiles"] as const,
  me: () => [...profileKeys.all, "me"] as const,
  public: (id: string) => [...profileKeys.all, "public", id] as const,
  skill: (id: string) => ["skills", "detail", id] as const,
  skills: (prefix: string) => ["skills", "search", prefix] as const,
}

export function useOwnProfile(enabled = true) {
  return useQuery({
    queryKey: profileKeys.me(),
    queryFn: () => apiClient.get<OwnProfileResponse>("/profiles/me"),
    enabled,
  })
}

export function usePublicProfile(id: string) {
  return useQuery({
    queryKey: profileKeys.public(id),
    queryFn: () => apiClient.get<PublicProfileResponse>(`/profiles/${id}`),
    enabled: id.length > 0,
  })
}

export function useUpdateOwnProfile() {
  const qc = useQueryClient()

  return useMutation({
    mutationFn: (data: UpdateOwnProfileRequest) =>
      apiClient.put<OwnProfileResponse>("/profiles/me", data),
    onSuccess: (profile) => {
      qc.setQueryData(profileKeys.me(), profile)
      void qc.invalidateQueries({ queryKey: profileKeys.all })
    },
  })
}

export function useSkills(prefix: string) {
  return useQuery({
    queryKey: profileKeys.skills(prefix),
    queryFn: () =>
      apiClient.get<SkillResponse[]>(
        `/skills?prefix=${encodeURIComponent(prefix)}`
      ),
  })
}

export function useSkill(id: string) {
  return useQuery({
    queryKey: profileKeys.skill(id),
    queryFn: () => apiClient.get<SkillResponse>(`/skills/${id}`),
    enabled: id.length > 0,
  })
}

export function toProfileUser(
  profile: OwnProfileResponse | PublicProfileResponse,
  skills: string[] = []
): User {
  return {
    id: profile.id,
    name: profile.displayName,
    avatarUrl: profile.photoUrl ?? undefined,
    bio: profile.bio ?? undefined,
    workplace: profile.workplace ?? undefined,
    position: profile.position ?? undefined,
    availability: profile.availability ?? undefined,
    location: profile.locationText ?? undefined,
    skills,
    reputation: toReputation(profile),
    verified: false,
    joinedAt: "createdAt" in profile ? profile.createdAt : "",
  }
}

function toReputation(
  profile: Pick<
    OwnProfileResponse | PublicProfileResponse,
    "averageRating" | "reviewCount" | "completedTaskCount"
  >
): Reputation {
  return {
    points: profile.completedTaskCount,
    averageRating: Number(profile.averageRating),
    reviewCount: profile.reviewCount,
    completedTasks: profile.completedTaskCount,
  }
}
